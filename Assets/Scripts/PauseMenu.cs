using UnityEngine;
using UnityEngine.SceneManagement;

// ESC pause menu: Resume / Instructions / Main Menu.
public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;
    public InstructionsPopup instructionsPopup;

    bool isPaused;

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.HasWon)) return;
        if (instructionsPopup != null && instructionsPopup.panelRoot != null && instructionsPopup.panelRoot.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume(); else Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        GameManager.Instance?.SetInputLocked(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        GameManager.Instance?.SetInputLocked(false);
    }

    public void OnInstructionsClicked()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        instructionsPopup?.Show(auto: false);
    }

    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
