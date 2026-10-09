using UnityEngine;

// Pasang di Light lampu neon loket tiket.
// Kebanyakan waktu stabil, sesekali berkedip cepat seperti neon rusak.
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    public float baseIntensity = 2f;
    public Vector2 stableTime = new Vector2(2f, 6f);
    public Vector2 flickerTime = new Vector2(0.1f, 0.6f);

    [Header("Opsional: material emisi tabung neon")]
    public Renderer tubeRenderer;
    public Color emissionColor = new Color(1f, 0.92f, 0.6f);
    public float emissionStrength = 3f;

    [Header("Opsional: bunyi buzz/klik")]
    public AudioSource buzz;

    Light lamp;
    float stateEnd;
    bool flickering;

    void Awake()
    {
        lamp = GetComponent<Light>();
        if (tubeRenderer) tubeRenderer.material.EnableKeyword("_EMISSION");
        NextState(false);
    }

    void Update()
    {
        if (Time.time >= stateEnd) NextState(!flickering);

        float k = flickering ? (Random.value > 0.5f ? Random.Range(0f, 0.3f) : 1f) : 1f;
        Apply(k);
    }

    void NextState(bool flicker)
    {
        flickering = flicker;
        Vector2 r = flicker ? flickerTime : stableTime;
        stateEnd = Time.time + Random.Range(r.x, r.y);
        if (flicker && buzz) buzz.Play();
    }

    void Apply(float k)
    {
        lamp.intensity = baseIntensity * k;
        if (tubeRenderer)
            tubeRenderer.material.SetColor("_EmissionColor", emissionColor * emissionStrength * k);
    }
}
