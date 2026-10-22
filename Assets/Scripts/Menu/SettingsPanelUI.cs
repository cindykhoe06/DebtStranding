using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menghubungkan slider di SettingsPanel ke SettingsManager.
// Mengisi nilai saat panel diaktifkan (OnEnable) dan menyimpan setiap kali nilai berubah.
public class SettingsPanelUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider mouseSensitivitySlider;

    [Header("Slider value labels")]
    public TMP_Text masterValueLabel;
    public TMP_Text musicValueLabel;
    public TMP_Text sfxValueLabel;
    public TMP_Text mouseSensitivityValueLabel;

    bool suppressCallbacks;
    bool listenersBound;

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
    }

    public void RefreshFromSettings()
    {
        suppressCallbacks = true;

        masterSlider.minValue = 0f; masterSlider.maxValue = 1f;
        musicSlider.minValue = 0f; musicSlider.maxValue = 1f;
        sfxSlider.minValue = 0f; sfxSlider.maxValue = 1f;
        mouseSensitivitySlider.minValue = SettingsManager.MinMouseSensitivity;
        mouseSensitivitySlider.maxValue = SettingsManager.MaxMouseSensitivity;

        masterSlider.value = SettingsManager.MasterVolume;
        musicSlider.value = SettingsManager.MusicVolume;
        sfxSlider.value = SettingsManager.SFXVolume;
        mouseSensitivitySlider.value = SettingsManager.MouseSensitivity;

        UpdateVolumeLabel(masterValueLabel, masterSlider.value);
        UpdateVolumeLabel(musicValueLabel, musicSlider.value);
        UpdateVolumeLabel(sfxValueLabel, sfxSlider.value);
        UpdateMouseSensitivityLabel(mouseSensitivitySlider.value);

        suppressCallbacks = false;
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

    public void OnResetClicked()
    {
        SettingsManager.ResetToDefault();
        RefreshFromSettings();
    }
}
