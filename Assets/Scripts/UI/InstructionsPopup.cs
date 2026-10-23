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
@"1. GRATIS 3 MENIT PERTAMA
Gunakan waktu untuk menjelajah & mengumpulkan koin.

2. HUTANG & BUNGA 20%
Tiap 3 menit, tagihan 20 Koin bertambah.
Hutang belum lunas kena bunga 20% majemuk.

3. PETUGAS PENAGIH (USHER)
• Koin Cukup: Hutang lunas (Streak +1).
• Koin Kurang: Hilang 1 Nyawa & streak kembali 0/3!

4. CARA MENANG (ESCAPE)
Lunasi hutang 3 KALI BERTURUT-TURUT untuk membuka pintu keluar.

KONTROL:
WASD    : Bergerak
Shift   : Lari
E       : Interaksi
Tab / I : Buka/Tutup Panduan
ESC     : Pause Menu";

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