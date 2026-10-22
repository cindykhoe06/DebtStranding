using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Enemy Capture")]
    public Transform playerSpawnPoint;       // Lobby spawn - where player is sent back to when caught
    public Image captureFlashImage;          // Full-screen red overlay on GameHUD (alpha animates in/out)
    public float captureFlashDuration = 0.6f;
    public AudioSource sfxAudioSource;       // Shared one-shot SFX player on GameManager
    public AudioClip captureSound;
    public AudioClip paidSound;
    public EnemyAI candleman;                // The one Candleman instance in Main Scene

    public bool IsGameOver => isGameOver;
    public bool HasWon => hasWon;
    public bool IsInputLocked { get; private set; }

    [Header("UI - Top Left (Finances)")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI debtText;

    [Header("UI - Top Right (Timer & Wave)")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI waveText;
    public Color timerNormalColor = Color.white;
    public Color timerWarningColor = Color.red;
    public float timerWarningThreshold = 15f;
    public AudioSource bellAudioSource;
    public AudioClip bellClip;
    public float bellLeadTime = 10f;

    [Header("UI - Bottom Left (Survival & Goal)")]
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI paymentsText;

    [Header("UI - Key")]
    public TextMeshProUGUI keyText; // Menampilkan nama kunci spesifik

    [Header("UI - Notifications")]
    public TextMeshProUGUI notificationText;
    public float notificationDuration = 2.5f;
    Coroutine notificationRoutine;

    [Header("Navigation")]
    public Unity.AI.Navigation.NavMeshSurface navMeshSurface;

    [Header("Blackout / Coins")]
    public BlackoutManager blackoutManager;

    [Header("Escape")]
    public GameObject escapeWayRoot;    // "Escape Way (Hidden)" - revealed once settledPayments reaches 3
    public GameObject escapeWayBarrier; // final wall blocking the way out - deactivated at the same time

    [Header("Game State")]
    public int coins = 0;
    public int debt = 0;
    public int lives = 3;
    public int settledPayments = 0; // Target is 3
    public int currentWave = 0;     // 0 = Free period, 1 = Wave 1, etc.
    public bool hasKey = false;     // Tetap dipertahankan untuk kompatibilitas script teman

    // Daftar nama kunci spesifik yang sudah dikoleksi
    private List<string> collectedKeyNames = new List<string>();

    [Header("Timer Settings")]
    public float waveDuration = 300f; // 5 Menit
    float timeRemaining;
    bool isGameOver = false;
    bool hasWon = false;
    bool bellPlayedThisWave = false;

    [Header("Win/Lose/Pause")]
    public EndScreenUI endScreenUI;
    public PauseMenu pauseMenu;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        timeRemaining = waveDuration;
        UpdateUI();
    }

    void Update()
    {
        if (isGameOver || hasWon) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;

            if (!bellPlayedThisWave && timeRemaining <= bellLeadTime)
            {
                bellPlayedThisWave = true;
                if (bellAudioSource != null && bellClip != null) bellAudioSource.PlayOneShot(bellClip);
            }
        }
        else
        {
            OnWaveTimerEnd();
        }

        DisplayTime(timeRemaining);
    }

    void DisplayTime(float time)
    {
        if (timerText == null) return;
        float minutes = Mathf.FloorToInt(time / 60);
        float seconds = Mathf.FloorToInt(time % 60);
        timerText.text = string.Format("Next Bill In:  {0:00}:{1:00}", minutes, seconds);
        timerText.color = time <= timerWarningThreshold ? timerWarningColor : timerNormalColor;
    }

    void OnWaveTimerEnd()
    {
        currentWave++;
        timeRemaining = waveDuration;
        bellPlayedThisWave = false;

        if (currentWave == 1)
        {
            debt = 20;
            Debug.Log("Free exploration ended! First debt wave starts: 20 coins.");
        }
        else
        {
            int interest = Mathf.RoundToInt(debt * 0.20f);
            debt = debt + interest + 20;
            Debug.Log($"New wave! 20% interest ({interest}) + 20 new debt = {debt} total.");
        }

        UpdateUI();
        if (blackoutManager != null) blackoutManager.TriggerBlackout();
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        UpdateUI();
    }

    public void OnEncounterUsher()
    {
        if (debt == 0)
        {
            ShowNotification("Tidak ada tagihan saat ini.");
            return;
        }

        if (coins >= debt)
        {
            coins -= debt;
            debt = 0;
            settledPayments++;
            ShowNotification($"PAID! Settled {settledPayments}/3");
            if (sfxAudioSource != null && paidSound != null) sfxAudioSource.PlayOneShot(paidSound);
            Debug.Log($"Payment successful! Streak: {settledPayments}/3");

            if (settledPayments >= 3) OpenEscapeWay();
        }
        else
        {
            if (!DebugManager.Invincible) lives--;
            settledPayments = 0; // STREAK RESET KEMBALI KE 0 KARENA GAGAL BAYAR!
            ShowNotification("GAGAL BAYAR! Streak Reset!");
            Debug.Log($"Cannot pay! Lost 1 life. Streak reset to 0/3. Lives left: {lives}");

            if (!DebugManager.Invincible && lives <= 0)
            {
                TriggerGameOver("Tidak mampu membayar tagihan.");
            }
        }

        UpdateUI();
    }

    void OpenEscapeWay()
    {
        if (escapeWayRoot != null) escapeWayRoot.SetActive(true);
        if (escapeWayBarrier != null) escapeWayBarrier.SetActive(false);
        SetObjective("Jalan keluar terbuka!");
        ShowNotification("Semua tagihan lunas! Jalan keluar terbuka.");
    }

    public void OnReachExit()
    {
        if (isGameOver || hasWon) return;
        if (settledPayments >= 3) TriggerWin();
    }

    public void SetObjective(string text)
    {
        if (waveText != null) waveText.text = text;
    }

    public void ShowNotification(string message)
    {
        if (notificationText == null) return;
        if (notificationRoutine != null) StopCoroutine(notificationRoutine);
        notificationRoutine = StartCoroutine(NotificationRoutine(message));
    }

    System.Collections.IEnumerator NotificationRoutine(string message)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(notificationDuration);
        notificationText.gameObject.SetActive(false);
    }

    public void SetInputLocked(bool locked)
    {
        IsInputLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = locked;
    }

    // --- FUNGSI KUNCI SPESIFIK & AMAN ---
    // Dipanggil saat mengambil kunci dengan nama spesifik (contoh: "Office Key")
    public void GrantKey(string keyDisplayName)
    {
        hasKey = true;
        if (!collectedKeyNames.Contains(keyDisplayName))
        {
            collectedKeyNames.Add(keyDisplayName);
        }
        UpdateKeyUI();
        ShowNotification($"Kamu mendapatkan {keyDisplayName}!");
    }

    // Tetap disediakan tanpa parameter agar script lama/teman tidak error!
    public void GrantKey()
    {
        GrantKey("Office Key");
    }

    void UpdateKeyUI()
    {
        if (keyText == null) return;

        if (collectedKeyNames.Count == 0)
        {
            keyText.text = "Key: None"; // Bersih di awal game!
        }
        else
        {
            keyText.text = "Key: " + string.Join(", ", collectedKeyNames);
        }
    }

    void UpdateUI()
    {
        if (coinsText) coinsText.text = $"Coins: {coins}";
        if (debtText) debtText.text = $"Debt: {debt}";

        if (waveText && currentWave == 0) waveText.text = "Free Exploration";
        else if (waveText && settledPayments < 3) waveText.text = $"Wave {currentWave}";

        if (livesText)
        {
            string hearts = "";
            for (int i = 0; i < lives; i++) hearts += "♥ ";
            livesText.text = hearts.TrimEnd();
        }

        if (paymentsText) paymentsText.text = $"Settled Payment: {settledPayments}/3";

        UpdateKeyUI();
    }

    void TriggerGameOver(string reason)
    {
        isGameOver = true;
        Debug.Log("GAME OVER - " + reason);
        SetInputLocked(true);
        Time.timeScale = 0f;
        if (endScreenUI != null) endScreenUI.ShowLose(reason, coins, settledPayments, lives);
    }

    public void TriggerWin()
    {
        hasWon = true;
        Debug.Log("VICTORY - 3/3 payments settled, exit reached!");
        SetInputLocked(true);
        Time.timeScale = 0f;
        if (endScreenUI != null) endScreenUI.ShowWin(coins, currentWave, settledPayments, lives);
    }

    public void OnPlayerCaught(CharacterController playerController)
    {
        if (isGameOver || hasWon) return;

        OnEncounterUsher();

        if (captureFlashImage != null) StartCoroutine(FlashCapture());
        if (sfxAudioSource != null && captureSound != null) sfxAudioSource.PlayOneShot(captureSound);

        RespawnPlayerAtLobby(playerController);
    }

    void RespawnPlayerAtLobby(CharacterController playerController)
    {
        if (playerController == null || playerSpawnPoint == null) return;
        playerController.enabled = false;
        playerController.transform.SetPositionAndRotation(playerSpawnPoint.position, playerSpawnPoint.rotation);
        playerController.enabled = true;
    }

    System.Collections.IEnumerator FlashCapture()
    {
        Color baseColor = captureFlashImage.color;
        float t = 0f;
        while (t < captureFlashDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Sin(Mathf.Clamp01(t / captureFlashDuration) * Mathf.PI);
            captureFlashImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, a * 0.6f);
            yield return null;
        }
        captureFlashImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
    }

    public void ResetState()
    {
        coins = 0;
        debt = 0;
        lives = 3;
        settledPayments = 0;
        currentWave = 0;
        hasKey = false;
        collectedKeyNames.Clear();
        timeRemaining = waveDuration;
        bellPlayedThisWave = false;
        isGameOver = false;
        hasWon = false;
        UpdateUI();
        SetObjective("Free Exploration");
    }
}