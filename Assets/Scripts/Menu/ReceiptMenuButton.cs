using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pasang di tiap tombol menu (ENTER, VISITOR RULES, dst).
// Efek hover: bar merah "mengisi" dari kiri, panah muncul, teks berubah warna, bunyi ka-ching.
public class ReceiptMenuButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("References")]
    public Image highlightBar;      // Image Type = Filled, Fill Method = Horizontal, Origin = Left
    public GameObject arrow;        // teks/gambar "▶"
    public TMP_Text label;

    [Header("Style")]
    public Color normalTextColor = new Color(0.12f, 0.12f, 0.12f);
    public Color hoverTextColor  = new Color(0.95f, 0.9f, 0.85f);
    public float fillSpeed = 8f;

    [Header("Audio (opsional)")]
    public AudioSource sfxSource;
    public AudioClip hoverClip;

    bool hovered;

    void Awake()
    {
        if (highlightBar) highlightBar.fillAmount = 0f;
        if (arrow) arrow.SetActive(false);
        if (label) label.color = normalTextColor;
    }

    void Update()
    {
        if (!highlightBar) return;
        float target = hovered ? 1f : 0f;
        highlightBar.fillAmount = Mathf.MoveTowards(
            highlightBar.fillAmount, target, fillSpeed * Time.unscaledDeltaTime);
    }

    void SetHover(bool on)
    {
        if (hovered == on) return;
        hovered = on;
        if (arrow) arrow.SetActive(on);
        if (label) label.color = on ? hoverTextColor : normalTextColor;
        if (on && sfxSource && hoverClip)
        {
            sfxSource.pitch = Random.Range(0.85f, 1.05f); // sedikit "rusak"
            sfxSource.PlayOneShot(hoverClip);
        }
    }

    public void OnPointerEnter(PointerEventData e) => SetHover(true);
    public void OnPointerExit(PointerEventData e)  => SetHover(false);
    public void OnSelect(BaseEventData e)          => SetHover(true);   // support keyboard/gamepad
    public void OnDeselect(BaseEventData e)        => SetHover(false);
}
