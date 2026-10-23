using UnityEngine;
using UnityEngine.SceneManagement;

public class EndScreenButtons : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("Nama Scene Main Menu Anda persis seperti di Build Settings")]
    public string mainMenuSceneName = "MainMenu";

    private void OnEnable()
    {
        // Pastikan kursor mouse selalu aktif dan bisa digerakkan saat layar ini muncul
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ==========================================
    // 1. FUNGSI TOMBOL RETRY / TRY AGAIN
    // ==========================================
    public void RetryGame()
    {
        // Kembalikan waktu normal sebelum reload scene
        Time.timeScale = 1f;

        // Reset state input GameManager jika ada
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetInputLocked(false);
        }

        // Muat ulang scene yang sedang aktif saat ini
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    // ==========================================
    // 2. FUNGSI TOMBOL BACK TO MAIN MENU
    // ==========================================
    public void GoToMainMenu()
    {
        // Kembalikan waktu normal
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetInputLocked(false);
        }

        // Buka scene Main Menu
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ==========================================
    // 3. FUNGSI TOMBOL QUIT GAME (OPSIONAL)
    // ==========================================
    public void QuitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}