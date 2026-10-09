using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menghubungkan slider/toggle/dropdown di SettingsPanel ke SettingsManager.
// Mengisi nilai saat panel diaktifkan (OnEnable) dan menyimpan setiap kali nilai berubah.
public class SettingsPanelUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider mouseSensitivitySlider;
    public Slider brightnessSlider;

    [Header("Slider value labels")]
    public TMP_Text masterValueLabel;
    public TMP_Text musicValueLabel;
    public TMP_Text sfxValueLabel;
    public TMP_Text mouseSensitivityValueLabel;
    public TMP_Text brightnessValueLabel;

    [Header("Toggle / Dropdown")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;

    [Header("Editor-only hint")]
    public GameObject editorHintObject;

    bool suppressCallbacks;
    bool listenersBound;

    void Start()
    {
        if (editorHintObject != null)
            editorHintObject.SetActive(Application.isEditor);
    }

    void OnEnable()
    {
        BindListeners();
        RefreshFromSettings();
    }

    void BindListeners()
    {
        if (listenersBound) return;
        listenersBound = true;
        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        brightnessSlider.onValueChanged.AddListener(OnBrightnessSliderChanged);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    public void RefreshFromSettings()
    {
        suppressCallbacks = true;

        masterSlider.minValue = 0f; masterSlider.maxValue = 1f;
        musicSlider.minValue = 0f; musicSlider.maxValue = 1f;
        sfxSlider.minValue = 0f; sfxSlider.maxValue = 1f;
        mouseSensitivitySlider.minValue = 0.1f; mouseSensitivitySlider.maxValue = 3f;
        brightnessSlider.minValue = 0.5f; brightnessSlider.maxValue = 1.5f;

        masterSlider.value = SettingsManager.MasterVolume;
        musicSlider.value = SettingsManager.MusicVolume;
        sfxSlider.value = SettingsManager.SFXVolume;
        mouseSensitivitySlider.value = SettingsManager.MouseSensitivity;
        brightnessSlider.value = SettingsManager.Brightness;
        fullscreenToggle.isOn = SettingsManager.Fullscreen;

        RefreshResolutionOptions();

        UpdateVolumeLabel(masterValueLabel, masterSlider.value);
        UpdateVolumeLabel(musicValueLabel, musicSlider.value);
        UpdateVolumeLabel(sfxValueLabel, sfxSlider.value);
        UpdateMouseSensitivityLabel(mouseSensitivitySlider.value);
        UpdateBrightnessLabel(brightnessSlider.value);

        suppressCallbacks = false;
    }

    void RefreshResolutionOptions()
    {
        resolutionDropdown.ClearOptions();
        var options = new List<string>();
        foreach (var r in SettingsManager.ResolutionOptions)
        {
            string text = r.width + " x " + r.height;
            if (r.isNative) text += " (Native)";
            options.Add(text);
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = Mathf.Clamp(SettingsManager.ResolutionIndex, 0, Mathf.Max(0, options.Count - 1));
        resolutionDropdown.RefreshShownValue();
    }

    // Master/Music/SFX: persen 0-100% langsung dari nilai (range slider sudah 0-1).
    static void UpdateVolumeLabel(TMP_Text label, float value)
    {
        if (label == null) return;
        label.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    // Mouse Sensitivity: "1.0x" (format 0.0 + akhiran x).
    void UpdateMouseSensitivityLabel(float value)
    {
        if (mouseSensitivityValueLabel == null) return;
        mouseSensitivityValueLabel.text = value.ToString("0.0") + "x";
    }

    // Brightness: persen dari NILAI ASLI langsung (0.5 -> 50%, 1 -> 100%, 1.5 -> 150%), bukan
    // persen relatif terhadap rentang slider.
    void UpdateBrightnessLabel(float value)
    {
        if (brightnessValueLabel == null) return;
        brightnessValueLabel.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    void OnMasterChanged(float v)
    {
        if (suppressCallbacks) return;
        SettingsManager.SetMasterVolume(v);
        UpdateVolumeLabel(masterValueLabel, v);
    }

    void OnMusicChanged(float v)
    {
        if (suppressCallbacks) return;
        SettingsManager.SetMusicVolume(v);
        UpdateVolumeLabel(musicValueLabel, v);
    }

    void OnSfxChanged(float v)
    {
        if (suppressCallbacks) return;
        SettingsManager.SetSFXVolume(v);
        UpdateVolumeLabel(sfxValueLabel, v);
    }

    void OnMouseSensitivityChanged(float v)
    {
        if (suppressCallbacks) return;
        SettingsManager.SetMouseSensitivity(v);
        UpdateMouseSensitivityLabel(v);
    }

    void OnBrightnessSliderChanged(float v)
    {
        if (suppressCallbacks) return;
        SettingsManager.SetBrightness(v);
        UpdateBrightnessLabel(v);
    }

    void OnFullscreenChanged(bool on)
    {
        if (suppressCallbacks) return;
        SettingsManager.SetFullscreen(on);
    }

    void OnResolutionChanged(int index)
    {
        if (suppressCallbacks) return;
        SettingsManager.SetResolutionIndex(index);
    }

    public void OnResetClicked()
    {
        SettingsManager.ResetToDefault();
        RefreshFromSettings();
    }
}