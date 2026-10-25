using UnityEngine;
using TMPro;

public class InstructionsPopup : MonoBehaviour
{
    public static InstructionsPopup Instance;

    [Header("UI References")]
    public GameObject panelRoot;            // Dibutuhkan oleh PauseMenu.cs
    public TextMeshProUGUI instructionsText; // Slot Text_Instructions kamu

    [Header("Default Instructions")]
    [TextArea(10, 25)]
    public string defaultRules = 
        @"1.<indent=8%><b>FREE FIRST 3 MINUTES</b>
        Use this grace period to explore & collect coins.</indent>

        2.<indent=8%><b>DEBT & 20% INTEREST</b>
        Every 3 minutes, a 20-Coin bill is added.
        Unpaid debt incurs a 20% compound interest penalty.</indent>

        3.<indent=8%><b>THE USHER (COLLECTOR)</b>
        • Sufficient Coins: Debt cleared (+1 Streak).
        • Insufficient Coins: Lose 1 Life & streak resets to 0/3!</indent>

        4.<indent=8%><b>HOW TO WIN (ESCAPE)</b>
        Settle your debt 3 CONSECUTIVE TIMES to unlock the exit.</indent>

        <b>CONTROLS:</b>
        <indent=8%>WASD    : Move
        Shift   : Sprint
        E       : Interact
        Tab / I : Toggle Guide
        ESC     : Pause Menu</indent>";

    private bool openedBefore = false;

    void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Start()
    {
        // Pasang teks otomatis
        if (instructionsText != null && !string.IsNullOrEmpty(defaultRules))
        {
            instructionsText.text = defaultRules;
        }

        // Tampilkan otomatis sekali di awal game
        Show(auto: true);
    }

    void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.HasWon)) return;

        bool isOpen = panelRoot != null && panelRoot.activeSelf;

        // Toggle via Tab atau I
        if ((Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I)) && !isOpen)
        {
            Show(auto: false);
        }
        else if (isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I)))
        {
            Close();
        }
    }

    // Dibutuhkan oleh PauseMenu.cs
    public void Show(bool auto)
    {
        if (auto && openedBefore) return;
        openedBefore = true;

        if (panelRoot != null) panelRoot.SetActive(true);

        Time.timeScale = 0f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetInputLocked(true);
        }
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        Time.timeScale = 1f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetInputLocked(false);
        }
    }

    // Helper untuk tombol UI
    public void TogglePopup()
    {
        bool isOpen = panelRoot != null && panelRoot.activeSelf;
        if (isOpen) Close();
        else Show(auto: false);
    }
}