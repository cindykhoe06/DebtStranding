using UnityEngine;
using TMPro;

// Visitor Rules-styled instructions popup. Shows once automatically at game start, and can
// be reopened any time via the HUD icon, Tab, or I. Pauses the game while open.
public class InstructionsPopup : MonoBehaviour
{
    public static InstructionsPopup Instance;

    [Header("UI References")]
    public GameObject panelRoot;
    public TextMeshProUGUI instructionsText; // Slot untuk TextMeshPro teks rules

    [Header("Rules Content (Auto-set to avoid Unity Editor inspector bugs)")]
    [TextArea(10, 25)]
    public string defaultRules = 
@"MUSEUM VISITOR CODE

1. GRATIS 5 MENIT PERTAMA
Gunakan waktu untuk menjelajah & mengumpulkan koin.

2. HUTANG & BUNGA 20%
Tiap 5 menit, tagihan 20 Koin bertambah.
Hutang belum lunas kena bunga 20% majemuk.

3. PETUGAS PENAGIH (USHER)
• Koin Cukup: Hutang lunas (Streak +1).
• Koin Kurang: Hilang 1 Nyawa & streak kembali 0/3!

4. CARA MENANG (ESCAPE)
Lunasi hutang 3 KALI BERTURUT-TURUT untuk membuka pintu keluar.

KONTROL:
WASD    : Bergerak
Shift   : Lari
E       : Interaksi (Pintu / Laci / Kunci)
Tab / I : Buka / Tutup Panduan ini
ESC     : Pause Menu";

    bool openedBefore;
    bool wasPausedByPauseMenu; // avoid double-resuming if PauseMenu is also open

    void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Start()
    {
        // Pasang teks otomatis agar tidak perlu paste manual di TextMeshPro Inspector
        if (instructionsText != null && !string.IsNullOrEmpty(defaultRules))
        {
            instructionsText.text = defaultRules;
        }

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