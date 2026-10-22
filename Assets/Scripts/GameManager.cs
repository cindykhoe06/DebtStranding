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
    public TextMeshProUGUI keyText;

    [Header("UI - Notifications")]
    public TextMeshProUGUI notificationText;
    public float notificationDuration = 2.5f;
    Coroutine notificationRoutine;

    [Header("Navigation")]
    public Unity.AI.Navigation.NavMeshSurface navMeshSurface; // rebaked at runtime when a locked door opens

    [Header("Blackout / Coins")]
    public BlackoutManager blackoutManager;

    [Header("Escape")]
    public DoorController escapeDoor; // opened automatically once settledPayments reaches 3

    [Header("Game State")]
    public int coins = 0;
    public int debt = 0;
    public int lives = 3;
    public int settledPayments = 0; // Target is 3
    public int currentWave = 0;     // 0 = Free period, 1 = Wave 1, etc.
    public bool hasKey = false;     // key for the 2 locked Main Hall rooms, found in Main Gallery

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

    // Timer hits 0: wave advances, debt grows, lights go out briefly and every remaining
    // coin relocates to a fresh random spot (BlackoutManager). Debt itself only causes a
    // life loss through being CAUGHT (see ResolvePayment) - the timer no longer punishes
    // directly, it just raises the stakes and shuffles the map.
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

    // Shared debt-resolution logic: coins >= debt -> auto-pay, settledPayments++, "PAID".
    // coins < debt -> lose a life, "GAGAL BAYAR". Called both by EnemyAI on capture and
    // available as a standalone check (kept from the original design).
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
            ShowNotification("GAGAL BAYAR!");
            Debug.Log($"Cannot pay! Lost 1 life. Lives left: {lives}");

            if (!DebugManager.Invincible && lives <= 0)
            {
                TriggerGameOver("Tidak mampu membayar tagihan.");
            }
        }

        UpdateUI();
    }

    void OpenEscapeWay()
    {
        if (escapeDoor != null) escapeDoor.OpenPermanently();
        if (navMeshSurface != null) navMeshSurface.BuildNavMesh();
        SetObjective("Jalan keluar terbuka!");
        ShowNotification("Semua tagihan lunas! Jalan keluar terbuka.");
    }

    // Called by ExitTrigger when the player physically reaches the Escape Way exit point.
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

    public void RebakeNavMesh()
    {
        if (navMeshSurface != null) navMeshSurface.BuildNavMesh();
    }

    public void GrantKey()
    {
        hasKey = true;
        if (keyText != null) keyText.text = "Kunci: Didapat";
        ShowNotification("Kamu mendapatkan kunci ruangan!");
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
        if (keyText) keyText.text = hasKey ? "Kunci: Didapat" : "Kunci: Belum ada";
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

    // Called by EnemyAI when it catches the player: resolve the debt (pay or lose a life),
    // then always teleport back to spawn regardless of outcome.
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

    // Full state reset used by Retry (EndScreenUI reloads the scene, so this mainly matters
    // if GameManager ever survives a reload via DontDestroyOnLoad - kept for safety).
    public void ResetState()
    {
        coins = 0;
        debt = 0;
        lives = 3;
        settledPayments = 0;
        currentWave = 0;
        hasKey = false;
        timeRemaining = waveDuration;
        bellPlayedThisWave = false;
        isGameOver = false;
        hasWon = false;
        UpdateUI();
        SetObjective("Free Exploration");
    }
}
