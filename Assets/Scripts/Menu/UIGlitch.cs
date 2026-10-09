using UnityEngine;
using UnityEngine.UI;

// Pasang di parent judul "DEBT STRANDING".
// Struktur yang disarankan:
//   Title (UIGlitch)
//     ├─ Title_Cyan  (TMP/Image warna cyan, alpha ~0.5)  -> splitA
//     ├─ Title_Red   (TMP/Image warna merah terang, alpha ~0.5) -> splitB
//     └─ Title_Main  (stempel merah utama)
// Sesekali judul "bergetar" dan layer warna bergeser (efek RGB split).
// Title_Cyan/Title_Red diam di alpha 0 dan hanya "menyala" (splitBurstAlpha) selama burst glitch.
public class UIGlitch : MonoBehaviour
{
    public RectTransform main;
    public RectTransform splitA;
    public RectTransform splitB;
    public CanvasGroup canvasGroup;   // opsional, untuk flicker alpha

    [Header("Timing")]
    public Vector2 intervalRange = new Vector2(1.5f, 4f);
    public Vector2 burstDuration = new Vector2(0.08f, 0.25f);

    [Header("Strength (pixel)")]
    public float jitter = 6f;
    public float splitOffset = 10f;

    [Header("Split layer alpha")]
    public float splitIdleAlpha = 0f;
    public float splitBurstAlpha = 0.6f;

    Vector2 mainPos, aPos, bPos;
    float nextBurst, burstEnd;
    Graphic aGraphic, bGraphic;

    void Start()
    {
        if (main) mainPos = main.anchoredPosition;
        if (splitA) { aPos = splitA.anchoredPosition; aGraphic = splitA.GetComponent<Graphic>(); }
        if (splitB) { bPos = splitB.anchoredPosition; bGraphic = splitB.GetComponent<Graphic>(); }
        SetSplitAlpha(splitIdleAlpha);
        ScheduleNext();
    }

    void Update()
    {
        float t = Time.unscaledTime;

        if (t >= nextBurst && t < burstEnd)
        {
            if (main) main.anchoredPosition = mainPos + new Vector2(Random.Range(-jitter, jitter), Random.Range(-jitter * 0.3f, jitter * 0.3f));
            if (splitA) splitA.anchoredPosition = aPos + new Vector2(-Random.Range(0, splitOffset), 0);
            if (splitB) splitB.anchoredPosition = bPos + new Vector2(Random.Range(0, splitOffset), 0);
            if (canvasGroup) canvasGroup.alpha = Random.value > 0.2f ? 1f : 0.4f;
            SetSplitAlpha(splitBurstAlpha);
        }
        else if (t >= burstEnd)
        {
            ResetPos();
            ScheduleNext();
        }
    }

    void ScheduleNext()
    {
        nextBurst = Time.unscaledTime + Random.Range(intervalRange.x, intervalRange.y);
        burstEnd = nextBurst + Random.Range(burstDuration.x, burstDuration.y);
    }

    void ResetPos()
    {
        if (main) main.anchoredPosition = mainPos;
        if (splitA) splitA.anchoredPosition = aPos;
        if (splitB) splitB.anchoredPosition = bPos;
        if (canvasGroup) canvasGroup.alpha = 1f;
        SetSplitAlpha(splitIdleAlpha);
    }

    void SetSplitAlpha(float a)
    {
        if (aGraphic) { var c = aGraphic.color; c.a = a; aGraphic.color = c; }
        if (bGraphic) { var c = bGraphic.color; c.a = a; bGraphic.color = c; }
    }
}