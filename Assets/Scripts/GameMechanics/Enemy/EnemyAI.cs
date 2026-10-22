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
    public float attackAnimDuration = 2.5f; // length of the "Bacok" (bacok.001) clip

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

    // Final design: the Candleman is only a threat while debt > 0. At debt == 0 it patrols
    // but never engages, even if it sees/hears the player.
    static bool IsDangerous => GameManager.Instance != null && GameManager.Instance.debt > 0;

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

        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.HasWon || GameManager.Instance.IsInputLocked))
        {
            agent.isStopped = true;
            return;
        }
        agent.isStopped = false;

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

        // Hunting mode: the moment there's debt, Candleman beelines the player's live
        // position from anywhere on the map - no line-of-sight or hearing-range check
        // needed. (Previously this required CanSeePlayer()/CanHearPlayer(), which meant a
        // player who wasn't sprinting and wasn't in the ~100 degree FOV cone was simply never
        // detected, even with an active debt - that was the "Candleman doesn't chase" bug.)
        if (IsDangerous)
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
        // Debt got settled (e.g. caught mid-chase and auto-paid) - stand down immediately.
        if (!IsDangerous) { EnterPatrol(); return; }

        agent.speed = chaseSpeed;
        if (player == null) { EnterPatrol(); return; }

        agent.SetDestination(player.position);

        // Fallback catch check (the SphereCollider trigger below is the primary detector).
        float dist = HorizontalDistance(transform.position, player.position);
        if (dist <= catchDistance)
        {
            CatchPlayer();
        }

        // No more dropping to Search while dangerous: hunting mode means Candleman keeps
        // retargeting the player's live position every frame, relentlessly, for as long as
        // debt > 0 (see UpdatePatrol). Search is still reachable if a future design wants a
        // "lost the scent" moment, but nothing currently triggers it.
    }

    void UpdateSearch()
    {
        if (!IsDangerous) { EnterPatrol(); return; }

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
        if (!IsDangerous) return; // debt == 0: touching Candleman has no effect whatsoever

        var gm = GameManager.Instance;
        bool canPay = gm != null && gm.coins >= gm.debt;

        state = State.Cooldown;
        cooldownTimer = catchCooldown;
        agent.ResetPath();
        agent.isStopped = true;

        if (canPay)
        {
            // Paid on the spot: no attack animation, no teleport, Candleman just stands
            // down and the cooldown timer (already started above) carries it back to patrol.
            gm.ResolvePaidCapture();
        }
        else
        {
            StartCoroutine(FailedCaptureSequence(gm));
        }
    }

    // coins < debt: freeze both player and Candleman, play the "Bacok" attack animation in
    // full view of the camera, then resolve the consequences (life lost, streak wiped,
    // teleport) only once the animation has actually finished.
    System.Collections.IEnumerator FailedCaptureSequence(GameManager gm)
    {
        gm?.SetInputLocked(true, showCursor: false);

        FacePlayerInstant();
        if (animator != null) animator.speed = 1f; // guarantee full-speed playback regardless of the chase-scaled speed set before the freeze
        PlayAnimTrigger(catchTrigger);

        yield return new WaitForSeconds(attackAnimDuration);

        gm?.ResolveFailedCapture(playerCC);

        // Don't unlock input if that resolution just triggered LOSE - TriggerGameOver already
        // re-locked it (with the cursor shown) for the end screen, and we'd otherwise stomp it.
        if (gm != null && !gm.IsGameOver) gm.SetInputLocked(false);
    }

    // Snaps Candleman's yaw to face the player instantly, so the attack animation that's
    // about to play is clearly facing the camera instead of whatever direction it was last
    // moving in (UpdateFacing() stops updating once the agent is stationary).
    void FacePlayerInstant()
    {
        if (player == null) return;
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
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
