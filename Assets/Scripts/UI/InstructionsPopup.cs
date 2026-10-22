using UnityEngine;

// Visitor Rules-styled instructions popup. Shows once automatically at game start, and can
// be reopened any time via the HUD icon, Tab, or I. Pauses the game while open.
public class InstructionsPopup : MonoBehaviour
{
    public static InstructionsPopup Instance;

    public GameObject panelRoot;
    bool openedBefore;
    bool wasPausedByPauseMenu; // avoid double-resuming if PauseMenu is also open

    void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Start()
    {
        Show(auto: true);
    }

    void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.HasWon)) return;

        bool isOpen = panelRoot != null && panelRoot.activeSelf;
        if ((Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I)) && !isOpen)
        {
            Show(auto: false);
        }
        else if (isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I)))
        {
            Close();
        }
    }

    public void Show(bool auto)
    {
        if (auto && openedBefore) return;
        openedBefore = true;

        if (panelRoot != null) panelRoot.SetActive(true);
        var scrollRect = panelRoot != null ? panelRoot.GetComponentInChildren<UnityEngine.UI.ScrollRect>() : null;
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f; // always open scrolled to the top
        Time.timeScale = 0f;
        GameManager.Instance?.SetInputLocked(true);
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        Time.timeScale = 1f;
        GameManager.Instance?.SetInputLocked(false);
    }
}
