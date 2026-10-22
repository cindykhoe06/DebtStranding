using UnityEngine;

// The key item found in Main Gallery, unlocks the 2 locked Main Hall rooms. Floats/spins
// in place (idle animation), glows faintly, and chimes softly while the player is nearby -
// discoverable without being a beacon, so finding it still takes some exploring.
public class RoomKey : MonoBehaviour, IInteractable
{
    public float rotateSpeed = 60f;
    public float bobAmount = 0.08f;
    public float bobSpeed = 1.5f;
    public AudioClip pickupSound;

    [Header("Discoverability")]
    public float proximityRadius = 5f;
    public AudioClip proximityChimeClip;
    public float chimeInterval = 1.8f;
    [Range(0f, 1f)] public float chimeVolume = 0.35f;
    public Color glowColor = new Color(1f, 0.85f, 0.5f);
    public float glowIntensity = 1.2f;
    public float glowRange = 4f;
    AudioSource proximityAudio;
    SphereCollider proximityTrigger;

    bool collected;
    Vector3 basePos;
    float bobTimer;
    float chimeTimer;
    bool playerInRange;

    void Awake()
    {
        // Self-configures so the scene doesn't need a second hand-wired collider/AudioSource:
        // a plain, non-trigger BoxCollider already exists for the E-interact raycast, so the
        // 5m "you're close" detector is added here as its own trigger sphere.
        proximityTrigger = gameObject.AddComponent<SphereCollider>();
        proximityTrigger.isTrigger = true;
        proximityTrigger.radius = proximityRadius;

        proximityAudio = gameObject.AddComponent<AudioSource>();
        proximityAudio.playOnAwake = false;
        proximityAudio.spatialBlend = 1f;
        proximityAudio.minDistance = 1.5f;
        proximityAudio.maxDistance = proximityRadius + 3f;
        proximityAudio.volume = chimeVolume;

        var glowGO = new GameObject("KeyGlow");
        glowGO.transform.SetParent(transform, false);
        var glow = glowGO.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = glowColor;
        glow.intensity = glowIntensity;
        glow.range = glowRange;
        glow.shadows = LightShadows.None;
    }

    void Start()
    {
        basePos = transform.localPosition;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        bobTimer += Time.deltaTime * bobSpeed;
        transform.localPosition = basePos + new Vector3(0f, Mathf.Sin(bobTimer) * bobAmount, 0f);

        if (playerInRange && proximityChimeClip != null)
        {
            chimeTimer -= Time.deltaTime;
            if (chimeTimer <= 0f)
            {
                proximityAudio.PlayOneShot(proximityChimeClip, chimeVolume);
                chimeTimer = chimeInterval;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;
        playerInRange = true;
        chimeTimer = 0f; // chime immediately on entry
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
    }

    public string GetPromptMessage() => "[E] Ambil Kunci";

    public void Interact()
    {
        if (collected) return;
        collected = true;

        GameManager.Instance?.GrantKey();
        if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        GetComponent<Collider>().enabled = false;
        if (proximityTrigger != null) proximityTrigger.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        Destroy(gameObject, 0.5f);
    }
}
