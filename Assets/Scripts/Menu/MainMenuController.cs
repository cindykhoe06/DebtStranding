using UnityEngine;
using UnityEngine.SceneManagement;

// Pasang di GameObject "MainMenuController" (kosong) di scene MainMenu.
// Hubungkan ke OnClick() tiap tombol.
public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject rulesPanel;
    public GameObject settingsPanel;

    [Header("Scene")]
    public string gameSceneName = "Level1";

    [Header("Audio (opsional)")]
    public AudioSource sfxSource;
    public AudioClip clickClip;

    void Start()
    {
        ShowMain();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnEnter()
    {
        PlayClick();
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnRules()    { PlayClick(); SetPanel(rulesPanel); }
    public void OnSettings() { PlayClick(); SetPanel(settingsPanel); }
    public void OnBack()     { PlayClick(); ShowMain(); }

    public void OnExit()
    {
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ShowMain() => SetPanel(mainPanel);

    void SetPanel(GameObject target)
    {
        if (mainPanel) mainPanel.SetActive(target == mainPanel);
        if (rulesPanel) rulesPanel.SetActive(target == rulesPanel);
        if (settingsPanel) settingsPanel.SetActive(target == settingsPanel);
    }

    void PlayClick()
    {
        if (sfxSource && clickClip) sfxSource.PlayOneShot(clickClip);
    }
}
