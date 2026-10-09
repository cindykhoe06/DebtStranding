using UnityEngine;
using UnityEngine.UI;

// Pasang di RawImage layer kabut. Menggeser uvRect.x terus-menerus (kecepatan diatur),
// sedikit gelombang sinus di uvRect.y, dan alpha "bernapas" pelan antara minAlpha-maxAlpha.
[RequireComponent(typeof(RawImage))]
public class FogScroller : MonoBehaviour
{
    [Header("Scroll")]
    public float scrollSpeed = 0.01f;
    public float verticalWaveAmplitude = 0.02f;
    public float verticalWaveSpeed = 0.15f;

    [Header("Alpha breathing")]
    public float minAlpha = 0.25f;
    public float maxAlpha = 0.45f;
    public float breatheDuration = 6f;

    RawImage raw;
    Color baseColor;
    Rect baseUv;
    float elapsed;

    void Awake()
    {
        raw = GetComponent<RawImage>();
        baseColor = raw.color;
        baseUv = raw.uvRect;
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;

        var uv = raw.uvRect;
        uv.x = baseUv.x + elapsed * scrollSpeed;
        uv.y = baseUv.y + Mathf.Sin(elapsed * verticalWaveSpeed) * verticalWaveAmplitude;
        raw.uvRect = uv;

        float breatheDur = Mathf.Max(0.01f, breatheDuration);
        float phase = (Mathf.Sin(elapsed * (2f * Mathf.PI / breatheDur)) + 1f) * 0.5f;
        var c = baseColor;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, phase);
        raw.color = c;
    }
}