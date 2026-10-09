using UnityEngine;
using UnityEngine.InputSystem;

// Pasang di Image "Background" scene MainMenu.
// Zoom pelan bolak-balik 1.0-1.05 (~40 detik per arah) + pan halus mengikuti mouse (maks beberapa px).
[RequireComponent(typeof(RectTransform))]
public class BackgroundDrift : MonoBehaviour
{
    [Header("Zoom")]
    public float minScale = 1f;
    public float maxScale = 1.05f;
    public float zoomCycleDuration = 40f;

    [Header("Mouse pan")]
    public float maxOffset = 15f;
    public float followSmooth = 2f;

    RectTransform rt;
    Vector2 basePos;
    Vector2 currentOffset;

    void Awake()
    {
        rt = (RectTransform)transform;
        basePos = rt.anchoredPosition;
    }

    void Update()
    {
        float phase = Mathf.PingPong(Time.unscaledTime / zoomCycleDuration, 1f);
        float scale = Mathf.Lerp(minScale, maxScale, phase);
        rt.localScale = new Vector3(scale, scale, 1f);

        // Available overscan margin at the current scale, per axis. Clamping the pan to this
        // (instead of a flat maxOffset) guarantees the screen edge never shows a gap, even at
        // the tightest aspect ratio / zoom-phase combination.
        var parent = rt.parent as RectTransform;
        Vector2 bgSize = rt.rect.size * scale;
        Vector2 parentSize = parent != null ? parent.rect.size : bgSize;
        float marginX = Mathf.Max(0f, (bgSize.x - parentSize.x) * 0.5f);
        float marginY = Mathf.Max(0f, (bgSize.y - parentSize.y) * 0.5f);

        Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 mouse = new Vector2(
            Mathf.Clamp((mousePosition.x / Screen.width - 0.5f) * 2f, -1f, 1f),
            Mathf.Clamp((mousePosition.y / Screen.height - 0.5f) * 2f, -1f, 1f));
        Vector2 target = new Vector2(
            Mathf.Clamp(mouse.x * maxOffset, -marginX, marginX),
            Mathf.Clamp(mouse.y * maxOffset, -marginY, marginY));
        currentOffset = Vector2.Lerp(currentOffset, target, Time.unscaledDeltaTime * followSmooth);
        rt.anchoredPosition = basePos + currentOffset;
    }
}