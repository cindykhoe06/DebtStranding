using UnityEngine;
using UnityEngine.AI;

// Candleman: Patrol -> Chase -> Search -> (Cooldown after a catch) -> Patrol.
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Search, Cooldown }

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float waypointWaitTime = 1.5f;

    [Header("Chase")]
    public float chaseSpeed = 4.2f;
    [Range(0f, 360f)] public float viewAngle = 100f;
    public float viewDistance = 15f;
    public float hearDistance = 8f;
    public float hearPlayerSpeedThreshold = 5f; // player CharacterController speed considered "running"
    public LayerMask sightObstructionMask = ~0;

    [Header("Search")]
    public float searchDuration = 5f;

    [Header("Catch")]
    public float catchDistance = 1.2f;   // horizontal (XZ) fallback check - the trigger collider is the primary detector
    public float catchCooldown = 8f;

    [Header("References")]
    public Transform player;      // auto-found (tag "Player", else GameObject named "hero") if left empty
    public Transform eyePoint;    // raycast origin for FOV check; defaults to transform + 1.6m up

    [Header("Audio")]
    public AudioSource footstepSource;
    public AudioSource breathSource;
    public AudioSource stingerSource;
    public AudioClip chaseStinger;
    public float breathMaxDistance = 15f;

    [Header("Animation")]
    public Animator animator;              // Candleman.controller: Trigger params Idle/Walk/Run/Nunduk/Bacok
    public string walkTrigger = "Walk";
    public string runTrigger = "Run";
    public string catchTrigger = "Bacok";

    [Header("Rotation")]
    // NavMeshAgent.updateRotation is turned off on this agent (see scene setup) so turning
    // is fully manual here - smoother and less "snappy" than the agent's own angularSpeed-based turn.
    public float rotationSlerpSpeed = 10f;

    [Header("Animation Speed (reduces foot sliding)")]
    // This rig has no root motion, so the walk/run clip's leg cadence never naturally
    // matches actual NavMeshAgent travel speed. Scaling animator.speed by (actual / nominal)
    // keeps the legs roughly in sync with real movement instead of sliding across the floor.
    public float minAnimSpeed = 0.6f;
    public float maxAnimSpeed = 2.2f;

    NavMeshAgent agent;
    CharacterController playerCC;
    State state = State.Patrol;
    int currentWaypoint = 0;
    float waitTimer;
    float searchTimer;
    float cooldownTimer;
    Vector3 lastKnownPlayerPos;

    // Hunting mode: triggered externally (e.g. GameManager after a failed payment) - the
    // Candleman immediately knows the player's position and moves faster for a set duration,
    // ignoring the normal FOV/hearing checks until it expires.
    bool hunting;
    float huntTimer;
    float huntSpeedMultiplier = 1f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            var tagged = GameObject.FindWithTag("Player");
            var go = tagged != null ? tagged : GameObject.Find("hero");
            if (go != null) player = go.transform;
        }
        if (player != null) playerCC = player.GetComponent<CharacterController>();
    }

    void Start()
    {
        agent.speed = patrolSpeed;
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentWaypoint].position);
        }
        PlayAnimTrigger(walkTrigger);
    }

    void Update()
    {
        if (DebugManager.EnemyDisabled)
        {
            agent.isStopped = true;
            return;
        }

        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.HasWon))
        {
            agent.isStopped = true;
            return;
        }
        agent.isStopped = false;

        if (hunting)
        {
            huntTimer -= Time.deltaTime;
            if (huntTimer <= 0f) hunting = false;
        }

        switch (state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.Search: UpdateSearch(); break;
            case State.Cooldown: UpdateCooldown(); break;
        }

        UpdateFacing();
        UpdateAudio();
        UpdateAnimationSpeed();
    }

    // Called by GameManager when a payment is missed: the Candleman immediately knows
    // the player's position and chases faster for `duration` seconds.
    public void StartHunting(float duration, float speedMultiplier = 1.5f)
    {
        hunting = true;
        huntTimer = duration;
        huntSpeedMultiplier = speedMultiplier;
        if (player != null) lastKnownPlayerPos = player.position;
        EnterChase();
    }

    void UpdateAnimationSpeed()
    {
        if (animator == null) return;
        float speed = agent.velocity.magnitude;
        if (speed < 0.05f) { animator.speed = 1f; return; }
        float reference = (state == State.Chase || state == State.Search) ? chaseSpeed : patrolSpeed;
        animator.speed = Mathf.Clamp(speed / Mathf.Max(reference, 0.01f), minAnimSpeed, maxAnimSpeed);
    }

    // Smoothly turns the whole rig toward its current movement direction. Replaces
    // NavMeshAgent's built-in rotation (disabled via updateRotation=false) to avoid the
    // abrupt, "patah-patah" turning the agent's own angularSpeed-based rotation produced.
    void UpdateFacing()
    {
        Vector3 flatVel = agent.velocity;
        flatVel.y = 0f;
        if (flatVel.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(flatVel.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSlerpSpeed);
    }

    // The enemy's root sits at floor height (NavMeshAgent keeps it on the navmesh surface)
    // while the player's root sits at the CharacterController's CENTER height. A plain
    // Vector3.Distance between the two roots therefore carries a built-in ~1.1m vertical
    // offset that is negligible at 15m/8m ranges but was previously large enough, relative
    // to the 1.2m catch radius, to make the catch check unreliable. Every gameplay distance
    // check here uses the horizontal (XZ) distance instead.
    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        Vector3 flatA = new Vector3(a.x, 0f, a.z);
        Vector3 flatB = new Vector3(b.x, 0f, b.z);
        return Vector3.Distance(flatA, flatB);
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.6f;
        Vector3 targetPoint = player.position + Vector3.up * 0.9f;
        Vector3 toPlayer = targetPoint - origin;
        float dist = HorizontalDistance(origin, targetPoint);
        if (dist > viewDistance) return false;

        Vector3 flatForward = transform.forward; flatForward.y = 0f;
        Vector3 flatToPlayer = toPlayer; flatToPlayer.y = 0f;
        if (Vector3.Angle(flatForward, flatToPlayer) > viewAngle * 0.5f) return false;

        // Blocked only if the ray hits something that ISN'T the player before reaching them.
        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, toPlayer.magnitude, sightObstructionMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(player) && hit.transform != player) return false;
        }
        return true;
    }

    bool CanHearPlayer()
    {
        if (player == null || playerCC == null) return false;
        float dist = HorizontalDistance(transform.position, player.position);
        if (dist > hearDistance) return false;

        Vector3 flatVel = playerCC.velocity; flatVel.y = 0f;
        return flatVel.magnitude > hearPlayerSpeedThreshold;
    }

    void UpdatePatrol()
    {
        agent.speed = patrolSpeed;

        if (CanSeePlayer() || CanHearPlayer())
        {
            EnterChase();
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waypointWaitTime)
            {
                waitTimer = 0f;
                currentWaypoint = (currentWaypoint + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentWaypoint].position);
            }
        }
    }

    void UpdateChase()
    {
        agent.speed = hunting ? chaseSpeed * huntSpeedMultiplier : chaseSpeed;
        if (player == null) { EnterPatrol(); return; }

        agent.SetDestination(player.position);

        // Fallback catch check (the SphereCollider trigger below is the primary detector).
        float dist = HorizontalDistance(transform.position, player.position);
        if (dist <= catchDistance)
        {
            CatchPlayer();
            return;
        }

        // While hunting, the Candleman always knows where the player is - it never loses track.
        if (!hunting && !CanSeePlayer() && !CanHearPlayer())
        {
            lastKnownPlayerPos = player.position;
            EnterSearch();
        }
    }

    void UpdateSearch()
    {
        agent.speed = chaseSpeed;

        if (CanSeePlayer() || CanHearPlayer())
        {
            EnterChase();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchDuration)
            {
                EnterPatrol();
            }
        }
    }

    void UpdateCooldown()
    {
        agent.isStopped = true;
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f) EnterPatrol();
    }

    void EnterPatrol()
    {
        bool wasPatrol = state == State.Patrol;
        state = State.Patrol;
        waitTimer = 0f;
        if (patrolPoints != null && patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[currentWaypoint].position);
        if (!wasPatrol) PlayAnimTrigger(walkTrigger);
    }

    void EnterChase()
    {
        if (state != State.Chase)
        {
            if (stingerSource != null && chaseStinger != null) stingerSource.PlayOneShot(chaseStinger);
            PlayAnimTrigger(runTrigger);
        }
        state = State.Chase;
    }

    void EnterSearch()
    {
        state = State.Search;
        searchTimer = 0f;
        agent.SetDestination(lastKnownPlayerPos);
    }

    void CatchPlayer()
    {
        if (state == State.Cooldown) return; // already caught this cycle - ignore repeat triggers

        PlayAnimTrigger(catchTrigger);
        GameManager.Instance?.OnPlayerCaught(playerCC);
        state = State.Cooldown;
        cooldownTimer = catchCooldown;
        agent.ResetPath();
    }

    void PlayAnimTrigger(string trigger)
    {
        if (animator != null && !string.IsNullOrEmpty(trigger)) animator.SetTrigger(trigger);
    }

    // Primary catch detector: a SphereCollider (isTrigger) placed on this same GameObject.
    // Using a real trigger avoids the center-to-center distance math entirely, so it isn't
    // thrown off by the enemy/player root height mismatch described above.
    void OnTriggerEnter(Collider other) => TryCatchFromTrigger(other);
    void OnTriggerStay(Collider other) => TryCatchFromTrigger(other);

    void TryCatchFromTrigger(Collider other)
    {
        if (state == State.Cooldown) return;
        if (player == null) return;
        if (other.transform != player && !other.transform.IsChildOf(player)) return;
        CatchPlayer();
    }

    void UpdateAudio()
    {
        bool moving = agent.velocity.sqrMagnitude > 0.05f;

        if (footstepSource != null)
        {
            if (moving && !footstepSource.isPlaying) footstepSource.Play();
            if (!moving && footstepSource.isPlaying) footstepSource.Stop();
            footstepSource.pitch = (state == State.Chase || state == State.Search) ? 1.3f : 1f;
        }

        if (breathSource != null && player != null)
        {
            float dist = HorizontalDistance(transform.position, player.position);
            float t = Mathf.Clamp01(1f - (dist / breathMaxDistance));
            breathSource.volume = t;
            if (t > 0.02f && !breathSource.isPlaying) breathSource.Play();
            if (t <= 0.02f && breathSource.isPlaying) breathSource.Stop();
        }
    }

}
