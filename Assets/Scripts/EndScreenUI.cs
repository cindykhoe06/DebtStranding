using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

// WIN / LOSE receipt screens. Retry reloads Main Scene fresh (simplest reliable way to
// reset every piece of mutable state - picked-up coins, opened doors/drawers, tasks, etc.)
public class EndScreenUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Win")]
    public TextMeshProUGUI winSummaryText;

    [Header("Lose")]
    public TextMeshProUGUI loseSummaryText;

    void Awake()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    public void ShowWin(int coins, int wave, int payments, int lives)
    {
        float elapsed = Time.timeSinceLevelLoad;
        int mins = Mathf.FloorToInt(elapsed / 60f);
        int secs = Mathf.FloorToInt(elapsed % 60f);
        if (winSummaryText != null)
            winSummaryText.text = $"PAID IN FULL\n\nCoins tersisa: {coins}\nWave: {wave}\nPembayaran lunas: {payments}/3\nLives: {lives}\nWaktu: {mins:00}:{secs:00}";
        if (winPanel != null) winPanel.SetActive(true);
    }

    public void ShowLose(string reason, int coins, int payments, int lives)
    {
        if (loseSummaryText != null)
            loseSummaryText.text = $"GAGAL BAYAR\n\nPenyebab: {reason}\nCoins: {coins}\nPembayaran lunas: {payments}/3\nLives: {lives}";
        if (losePanel != null) losePanel.SetActive(true);
    }

    public void OnRetryClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Scene");
    }

    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
