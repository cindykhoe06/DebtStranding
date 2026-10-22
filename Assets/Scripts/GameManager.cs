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
    public GameObject escapeWayRoot;    // "Escape Way (Hidden)" - revealed once settledPayments reaches 3
    public GameObject escapeWayBarrier; // final wall blocking the way out - deactivated at the same time

    [Header("Game State")]
    public int coins = 0;
    public int debt = 0;
    public int lives = 3;
    public int settledPayments = 0; // Target is 3
    public int currentWave = 0;     // 0 = Free period, 1 = Wave 1, etc.
    public bool hasKey = false;     // key for the 2 locked Main Hall rooms, found in Main Gallery
    int lockedDoorsRemaining = 2;   // drives the "Buka ruangan terkunci di Main Hall" objective

    [Header("Timer Settings")]
    public float waveDuration = 180f;
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

    // Candleman only ever calls one of these two - it decides which before touching the
    // player (see EnemyAI.CatchPlayer), since the two outcomes behave too differently
    // (no teleport/flash vs. a choreographed attack+teleport) to share one method anymore.
    // debt == 0 is not handled here at all: EnemyAI.CatchPlayer no-ops entirely in that case.

    // Case: coins >= debt. Auto-pay, settle a payment, no teleport, no attack animation.
    public void ResolvePaidCapture()
    {
        if (isGameOver || hasWon) return;

        coins -= debt;
        debt = 0;
        settledPayments++;
        ShowNotification($"PAID! Settled {settledPayments}/3");
        if (sfxAudioSource != null && paidSound != null) sfxAudioSource.PlayOneShot(paidSound);
        Debug.Log($"Payment successful! Streak: {settledPayments}/3");

        if (settledPayments >= 3) OpenEscapeWay();
        UpdateUI();
    }

    // Case: coins < debt. Called by EnemyAI AFTER its "Bacok" attack animation finishes.
    // Loses a life, wipes the current settled-payment streak, teleports the player back to
    // spawn, and triggers LOSE if that was the last life.
    public void ResolveFailedCapture(CharacterController playerController)
    {
        if (isGameOver || hasWon) return;

        if (!DebugManager.Invincible) lives--;
        settledPayments = 0;
        ShowNotification("GAGAL BAYAR - Settled Payment hangus");
        Debug.Log($"Cannot pay! Lost 1 life, settled payments reset. Lives left: {lives}");

        if (captureFlashImage != null) StartCoroutine(FlashCapture());
        if (sfxAudioSource != null && captureSound != null) sfxAudioSource.PlayOneShot(captureSound);
        RespawnPlayerAtLobby(playerController);
        UpdateUI();

        if (!DebugManager.Invincible && lives <= 0)
        {
            TriggerGameOver("Tidak mampu membayar tagihan.");
        }
    }

    void OpenEscapeWay()
    {
        if (escapeWayRoot != null) escapeWayRoot.SetActive(true);
        if (escapeWayBarrier != null) escapeWayBarrier.SetActive(false);
        // Note: deliberately NOT rebaking NavMesh here - Unity's runtime NavMesh builder
        // needs "Read/Write Enabled" on every source mesh, which the imported museum meshes
        // don't have (changing that project-wide is a heavy, unnecessary cost since Candleman
        // isn't required to path into the Escape Way - the player reaches it via physics only).
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

    // showCursor=false is for in-gameplay freezes (e.g. Candleman's attack animation) where
    // player movement must stop but the mouse cursor should stay hidden/locked, unlike the
    // pause/instructions menus which want the cursor free to click buttons.
    public void SetInputLocked(bool locked, bool showCursor = true)
    {
        IsInputLocked = locked;
        if (locked && !showCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }
        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = locked;
    }

    public void GrantKey()
    {
        hasKey = true;
        if (keyText != null) keyText.text = "Kunci: Ada";
        ShowNotification("Kamu mendapatkan kunci ruangan!");
        UpdateUI();
    }

    // Called by DoorController once a locked Main Hall door is successfully unlocked with
    // the key, so the HUD objective can clear once both rooms are open.
    public void NotifyDoorUnlocked()
    {
        lockedDoorsRemaining = Mathf.Max(0, lockedDoorsRemaining - 1);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (coinsText) coinsText.text = $"Coins: {coins}";
        if (debtText) debtText.text = $"Debt: {debt}";

        if (waveText && !hasKey) waveText.text = "Cari kunci di Main Gallery";
        else if (waveText && lockedDoorsRemaining > 0) waveText.text = "Buka ruangan terkunci di Main Hall";
        else if (waveText && currentWave == 0) waveText.text = "Free Exploration";
        else if (waveText && settledPayments < 3) waveText.text = $"Wave {currentWave}";

        if (livesText)
        {
            string hearts = "";
            for (int i = 0; i < lives; i++) hearts += "♥ ";
            livesText.text = hearts.TrimEnd();
        }

        if (paymentsText) paymentsText.text = $"Settled Payment: {settledPayments}/3";
        if (keyText) keyText.text = hasKey ? "Kunci: Ada" : "Kunci: Belum ada";
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
        lockedDoorsRemaining = 2;
        timeRemaining = waveDuration;
        bellPlayedThisWave = false;
        isGameOver = false;
        hasWon = false;
        UpdateUI();
    }
}
