using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// ESC pause menu: Resume / Settings (minimal subset of the existing SettingsManager) / Main Menu.
public class PauseMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("Settings sliders (bound directly to SettingsManager's static setters)")]
    public Slider masterVolumeSlider;
    public Slider mouseSensitivitySlider;
    public Slider brightnessSlider;

    bool isPaused;

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = SettingsManager.MasterVolume;
            masterVolumeSlider.onValueChanged.AddListener(SettingsManager.SetMasterVolume);
        }
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = 0.1f;
            mouseSensitivitySlider.maxValue = 3f;
            mouseSensitivitySlider.value = SettingsManager.MouseSensitivity;
            mouseSensitivitySlider.onValueChanged.AddListener(SettingsManager.SetMouseSensitivity);
        }
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0.5f;
            brightnessSlider.maxValue = 1.5f;
            brightnessSlider.value = SettingsManager.Brightness;
            brightnessSlider.onValueChanged.AddListener(SettingsManager.SetBrightness);
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.HasWon)) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel != null && settingsPanel.activeSelf) { CloseSettings(); return; }
            if (isPaused) Resume(); else Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        if (GameManager.Instance != null) GameManager.Instance.SetInputLocked(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.SetInputLocked(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    void OnDestroy()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(SettingsManager.SetMasterVolume);
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.onValueChanged.RemoveListener(SettingsManager.SetMouseSensitivity);
        if (brightnessSlider != null) brightnessSlider.onValueChanged.RemoveListener(SettingsManager.SetBrightness);
    }
}
