using UnityEngine;
using UnityEngine.AI;

// Candleman: Patrol -> Chase -> Search -> (Cooldown after a catch) -> Patrol.
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Search, Cooldown, Investigate }

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float waypointWaitTime = 1.5f;

    [Header("Chase")]
    public float chaseSpeed = 5.8f;
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
    public float attackAnimDuration = 2.5f; // length of the "Attack" (bacok.001) clip

    [Header("Capture Camera Effect")]
    // During a failed-payment capture, the player camera is reparented onto this hand bone
    // (auto-found as "hand.L" under the model if left empty) so it swings with the attack,
    // while continuously looking back at Candleman.
    public Transform cameraHandAttachPoint;
    public float captureCameraLookSpeed = 10f;

    [Header("Investigate (lured by music)")]
    // Office Room's Gramophone (Gramophone.cs, unchanged) exposes IsPlaying - while debt == 0,
    // a playing gramophone pulls Candleman off its patrol route to go check the sound out.
    public float investigateWaitDuration = 8f;
    Gramophone[] gramophones;
    float investigateTimer;

    [Header("Crouch Through Low Doorways")]
    public float crouchLowerAmount = 0.9f;
    public float crouchLerpSpeed = 4f;
    public string crouchTrigger = "Nunduk";
    LayerMask crouchCheckMask;
    Transform modelTransform;
    Vector3 modelBasePos;
    bool isCrouching;
    float crouchBlend;

    [Header("References")]
    public Transform player;      // auto-found (tag "Player", else GameObject named "hero") if left empty
    public Transform eyePoint;    // raycast origin for FOV check; defaults to transform + 1.6m up

    [Header("Audio")]
    public AudioSource footstepSource;
    public AudioSource stingerSource;
    public AudioClip chaseStinger;

    [Header("Animation")]
    public Animator animator;              // Candleman.controller: Trigger params Idle/Walk/Run/Nunduk/Attack
    public string walkTrigger = "Walk";
    public string runTrigger = "Run";
    public string catchTrigger = "Attack";

    [Header("Rotation")]
    public float rotationSlerpSpeed = 10f;

    [Header("Animation Speed (reduces foot sliding)")]
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

        gramophones = FindObjectsByType<Gramophone>(FindObjectsInactive.Exclude);

        modelTransform = transform.Find("Model");
        if (modelTransform != null) modelBasePos = modelTransform.localPosition;

        crouchCheckMask = ~0;

        if (cameraHandAttachPoint == null) cameraHandAttachPoint = FindChildByName(transform, "hand.L");
    }

    static Transform FindChildByName(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var found = FindChildByName(child, name);
            if (found != null) return found;
        }
        return null;
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
            case State.Investigate: UpdateInvestigate(); break;
        }

        UpdateFacing();
        UpdateAudio();
        UpdateAnimationSpeed();
        UpdateCrouch();
    }

    void UpdateAnimationSpeed()
    {
        if (animator == null) return;
        float speed = agent.velocity.magnitude;
        if (speed < 0.05f) { animator.speed = 1f; return; }
        float reference = (state == State.Chase || state == State.Search) ? chaseSpeed : patrolSpeed;
        animator.speed = Mathf.Clamp(speed / Mathf.Max(reference, 0.01f), minAnimSpeed, maxAnimSpeed);
    }

    void UpdateFacing()
    {
        Vector3 flatVel = agent.velocity;
        flatVel.y = 0f;
        if (flatVel.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(flatVel.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSlerpSpeed);
    }

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

        if (IsDangerous)
        {
            EnterChase();
            return;
        }

        var playing = FindPlayingGramophone();
        if (playing != null)
        {
            EnterInvestigate(playing.transform.position);
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
        if (!IsDangerous) { EnterPatrol(); return; }

        agent.speed = chaseSpeed;
        if (player == null) { EnterPatrol(); return; }

        agent.SetDestination(player.position);

        float dist = HorizontalDistance(transform.position, player.position);
        if (dist <= catchDistance)
        {
            CatchPlayer();
        }
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

    void UpdateInvestigate()
    {
        if (IsDangerous) { EnterChase(); return; }

        agent.speed = patrolSpeed;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            investigateTimer += Time.deltaTime;
            if (investigateTimer >= investigateWaitDuration)
            {
                EnterPatrol();
            }
        }
    }

    Gramophone FindPlayingGramophone()
    {
        if (gramophones == null) return null;
        foreach (var g in gramophones)
        {
            if (g != null && g.IsPlaying) return g;
        }
        return null;
    }

    void EnterPatrol()
    {
        bool wasPatrol = state == State.Patrol;
        state = State.Patrol;
        waitTimer = 0f;

        if (stingerSource != null && stingerSource.isPlaying)
        {
            stingerSource.Stop();
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[currentWaypoint].position);
        if (!wasPatrol) PlayAnimTrigger(walkTrigger);
    }

    void EnterChase()
    {
        if (state != State.Chase)
        {
            if (stingerSource != null && chaseStinger != null)
            {
                stingerSource.clip = chaseStinger;
                stingerSource.loop = true;
                if (!stingerSource.isPlaying) stingerSource.Play();
            }
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

    void EnterInvestigate(Vector3 soundPos)
    {
        state = State.Investigate;
        investigateTimer = 0f;
        agent.SetDestination(soundPos);
        PlayAnimTrigger(walkTrigger);
    }

    string CurrentMovementTrigger() => state == State.Chase ? runTrigger : walkTrigger;

    void CatchPlayer()
    {
        if (state == State.Cooldown) return;
        if (!IsDangerous) return;

        var gm = GameManager.Instance;
        bool canPay = gm != null && gm.coins >= gm.debt;

        state = State.Cooldown;
        cooldownTimer = catchCooldown;
        agent.ResetPath();
        agent.isStopped = true;

        if (stingerSource != null && stingerSource.isPlaying)
        {
            stingerSource.Stop();
        }

        if (canPay)
        {
            PlayAnimTrigger("Idle");
            gm.ResolvePaidCapture();
        }
        else
        {
            StartCoroutine(FailedCaptureSequence(gm));
        }
    }

    System.Collections.IEnumerator FailedCaptureSequence(GameManager gm)
    {
        gm?.SetInputLocked(true, showCursor: false);
        gm?.PlayCaptureSound();           // Suara jumpscare langsung meledak
        gm?.TriggerBloodyVision(attackAnimDuration); // <=== LAYAR LANGSUNG BERDARAH!

        FacePlayerInstant();

        if (animator != null) animator.speed = 1f;
        PlayAnimTrigger(catchTrigger);

        var playerController = playerCC != null ? playerCC.GetComponent<PlayerController>() : null;
        Transform cam = playerController != null ? playerController.playerCamera : null;
        bool camAttached = cam != null && cameraHandAttachPoint != null;

        Transform originalParent = null;
        Vector3 originalLocalPos = Vector3.zero;
        Quaternion originalLocalRot = Quaternion.identity;

        if (camAttached)
        {
            originalParent = cam.parent;
            originalLocalPos = cam.localPosition;
            originalLocalRot = cam.localRotation;
            cam.SetParent(cameraHandAttachPoint, true);
        }

        float elapsed = 0f;
        while (elapsed < attackAnimDuration)
        {
            if (camAttached)
            {
                Vector3 lookPoint = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 3f;
                Vector3 dir = lookPoint - cam.position;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    cam.rotation = Quaternion.Slerp(cam.rotation, targetRot, Time.unscaledDeltaTime * captureCameraLookSpeed);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (camAttached)
        {
            cam.SetParent(originalParent, false);
            cam.localPosition = originalLocalPos;
            cam.localRotation = originalLocalRot;
        }

        gm?.ResolveFailedCapture(playerCC);

        if (gm != null && !gm.IsGameOver) gm.SetInputLocked(false);
    }

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

    void OnTriggerEnter(Collider other) => TryCatchFromTrigger(other);
    void OnTriggerStay(Collider other) => TryCatchFromTrigger(other);

    void TryCatchFromTrigger(Collider other)
    {
        if (state == State.Cooldown) return;
        if (player == null) return;
        if (other.transform != player && !other.transform.IsChildOf(player)) return;
        CatchPlayer();
    }

    // Menggunakan volume OverlapBox di area kepala atas dan sedikit di depan
    // untuk mendeteksi kusen pintu (lintel) tanpa meleset
    static readonly Collider[] crouchOverlapsBuffer = new Collider[6];
    void UpdateCrouch()
    {
        Vector3 boxCenter = transform.position + Vector3.up * 2.2f + transform.forward * 0.4f;
        Vector3 boxHalfExtents = new Vector3(0.4f, 0.5f, 0.4f);

        bool lowCeiling = false;
        int hitCount = Physics.OverlapBoxNonAlloc(boxCenter, boxHalfExtents, crouchOverlapsBuffer, transform.rotation, crouchCheckMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Transform t = crouchOverlapsBuffer[i].transform;
            if (t == transform || t.IsChildOf(transform)) continue;
            if (player != null && (t == player || t.IsChildOf(player))) continue;

            lowCeiling = true;
            break;
        }

        if (lowCeiling && !isCrouching)
        {
            isCrouching = true;
            PlayAnimTrigger(crouchTrigger);
        }
        else if (!lowCeiling && isCrouching)
        {
            isCrouching = false;
            PlayAnimTrigger(CurrentMovementTrigger());
        }

        crouchBlend = Mathf.MoveTowards(crouchBlend, isCrouching ? 1f : 0f, Time.deltaTime * crouchLerpSpeed);
        if (modelTransform != null)
        {
            modelTransform.localPosition = modelBasePos - Vector3.up * (crouchLowerAmount * crouchBlend);
        }
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
    }

    // Fungsi yang dipanggil oleh CrouchZone di pintu
    public void SetCrouching(bool crouch)
    {
        if (crouch && !isCrouching)
        {
            isCrouching = true;
            PlayAnimTrigger(crouchTrigger);
            Debug.Log("<color=yellow>[CANDLEMAN] Nunduk lewat pintu!</color>");
        }
        else if (!crouch && isCrouching)
        {
            isCrouching = false;
            PlayAnimTrigger(CurrentMovementTrigger());
            Debug.Log("<color=green>[CANDLEMAN] Berdiri tegak kembali!</color>");
        }
    }
}