using UnityEngine;
using UnityEngine.UI;

// Pasang di Image fullscreen paling atas di Canvas. Mendengarkan SettingsManager.OnBrightnessChanged
// dan menggelapkan/mencerahkan seluruh layar sesuai nilai Brightness.
[RequireComponent(typeof(Image))]
public class BrightnessOverlay : MonoBehaviour
{
    Image img;

    void Awake()
    {
        img = GetComponent<Image>();
        img.raycastTarget = false;
    }

    void OnEnable()
    {
        SettingsManager.OnBrightnessChanged += Apply;
        Apply(SettingsManager.Brightness);
    }

    void OnDisable()
    {
        SettingsManager.OnBrightnessChanged -= Apply;
    }

    void Apply(float brightness)
    {
        if (brightness < 1f)
            img.color = new Color(0f, 0f, 0f, 1f - brightness);
        else
            img.color = new Color(1f, 1f, 1f, (brightness - 1f) * 0.3f);
    }
}