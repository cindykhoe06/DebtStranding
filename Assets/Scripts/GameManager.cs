using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Enemy Capture")]
    public Transform playerSpawnPoint;       // Lobby spawn - where player is sent back to when caught
    public Image captureFlashImage;          // Full-screen red overlay on GameHUD (alpha animates in/out)
    public float captureFlashDuration = 0.6f;
    public AudioSource sfxAudioSource;       // Shared one-shot SFX player on GameManager
    public AudioClip captureSound;
    public EnemyAI candleman;                // The one Candleman instance in Main Scene

    public bool IsGameOver => isGameOver;
    public bool HasWon => hasWon;
    public bool IsInputLocked { get; private set; }

    [Header("UI - Top Left (Finances)")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI debtText;

    [Header("UI - Top Right (Timer & Waves)")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI waveText; // doubles as the active task "objective" label
    public Color timerNormalColor = Color.white;
    public Color timerWarningColor = Color.red;
    public float timerWarningThreshold = 15f;
    public AudioSource bellAudioSource;
    public AudioClip bellClip;
    public float bellLeadTime = 10f;

    [Header("UI - Bottom Left (Survival & Goal)")]
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI paymentsText;

    [Header("UI - Key / Battery")]
    public TextMeshProUGUI exitKeyText;
    public Slider batterySlider;
    public TextMeshProUGUI batteryText;

    [Header("UI - Notifications")]
    public TextMeshProUGUI notificationText;
    public float notificationDuration = 2.5f;
    Coroutine notificationRoutine;

    [Header("Navigation")]
    public Unity.AI.Navigation.NavMeshSurface navMeshSurface; // rebaked at runtime when a task opens a door

    [Header("Game State")]
    public int coins = 0;
    public int debt = 0;
    public int lives = 3;
    public int settledPayments = 0; // Target is 3
    public int currentWave = 0;     // 0 = Free period, 1 = Wave 1, etc.
    public bool hasExitKey = false;
    public bool hasMap = false;
    public float flashlightBattery = 100f;
    public float maxFlashlightBattery = 100f;

    [Header("Timer Settings")]
    public float waveDuration = 300f;
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
            if (debt > 0)
            {
                // Previous bill was never paid before the new one arrived.
                if (!DebugManager.Invincible) lives--;
                settledPayments = 0;
                ShowNotification("PEMBAYARAN GAGAL! Debt belum lunas.");
                Debug.Log($"Payment missed on timer! Lives left: {lives}");
                TriggerHunting(20f);

                if (!DebugManager.Invincible && lives <= 0)
                {
                    TriggerGameOver("Gagal membayar tagihan tepat waktu.");
                    return;
                }
            }

            int interest = Mathf.RoundToInt(debt * 0.20f);
            debt = debt + interest + 20;
            Debug.Log($"New wave! 20% interest ({interest}) + 20 new debt = {debt} total.");
        }

        UpdateUI();
        TaskManager.Instance?.RefreshObjective();
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        UpdateUI();
    }

    // Voluntary payment at the Payment Area (loket). Reused by both the loket interaction
    // and (indirectly) nothing else - OnWaveTimerEnd has its own failure branch above since
    // a missed timer and a voluntary underpaid visit are different situations.
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
            ShowNotification($"Pembayaran berhasil! {settledPayments}/3");
            Debug.Log($"Payment successful! Streak: {settledPayments}/3");

            if (settledPayments >= 3)
            {
                ShowNotification("Semua tagihan lunas! Cari KUNCI EXIT dan menuju pintu keluar.");
            }
        }
        else
        {
            if (!DebugManager.Invincible) lives--;
            settledPayments = 0;
            ShowNotification("Koin tidak cukup untuk membayar!");
            Debug.Log($"Cannot pay! Lost 1 life. Streak reset to 0/3. Lives left: {lives}");

            if (!DebugManager.Invincible && lives <= 0)
            {
                TriggerGameOver("Tidak mampu membayar tagihan.");
                return;
            }
        }

        UpdateUI();
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

    public void TriggerHunting(float duration, float speedMultiplier = 1.5f)
    {
        if (candleman != null) candleman.StartHunting(duration, speedMultiplier);
    }

    public void RebakeNavMesh()
    {
        if (navMeshSurface != null) navMeshSurface.BuildNavMesh();
    }

    public void GrantExitKey()
    {
        hasExitKey = true;
        if (exitKeyText != null) exitKeyText.text = "KUNCI EXIT: Didapat";
        ShowNotification("Kamu mendapatkan KUNCI EXIT!");
    }

    public void SetBattery(float value)
    {
        flashlightBattery = Mathf.Clamp(value, 0f, maxFlashlightBattery);
        if (batterySlider != null) batterySlider.value = flashlightBattery / maxFlashlightBattery;
        if (batteryText != null) batteryText.text = $"Baterai: {Mathf.CeilToInt(flashlightBattery)}%";
    }

    void UpdateUI()
    {
        if (coinsText) coinsText.text = $"Coins: {coins}";
        if (debtText) debtText.text = $"Debt: {debt}";

        if (livesText)
        {
            string hearts = "";
            for (int i = 0; i < lives; i++) hearts += "♥ ";
            livesText.text = hearts.TrimEnd();
        }

        if (paymentsText) paymentsText.text = $"Settled Payment: {settledPayments}/3";
        if (exitKeyText) exitKeyText.text = hasExitKey ? "KUNCI EXIT: Didapat" : "KUNCI EXIT: Belum ada";
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
        if (endScreenUI != null) endScreenUI.ShowWin(coins, settledPayments);
    }

    public void OnPlayerCaught(CharacterController playerController)
    {
        if (isGameOver || hasWon) return;

        if (DebugManager.Invincible)
        {
            Debug.Log("[DEBUG] Invincible active - caught but no life lost.");
        }
        else
        {
            lives--;
            Debug.Log($"Caught by the enemy! Lives left: {lives}");
        }

        UpdateUI();

        if (captureFlashImage != null) StartCoroutine(FlashCapture());
        if (sfxAudioSource != null && captureSound != null) sfxAudioSource.PlayOneShot(captureSound);

        RespawnPlayerAtLobby(playerController);

        if (!DebugManager.Invincible && lives <= 0) TriggerGameOver("Tertangkap oleh Candleman.");
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

    // Full state reset used by Retry (EndScreenUI) - everything the HUD/game reads must
    // go back to a fresh-game value since this GameObject is not destroyed/recreated on retry.
    public void ResetState()
    {
        coins = 0;
        debt = 0;
        lives = 3;
        settledPayments = 0;
        currentWave = 0;
        hasExitKey = false;
        hasMap = false;
        flashlightBattery = maxFlashlightBattery;
        timeRemaining = waveDuration;
        bellPlayedThisWave = false;
        isGameOver = false;
        hasWon = false;
        SetBattery(flashlightBattery);
        UpdateUI();
        SetObjective("Free Exploration");
    }
}
