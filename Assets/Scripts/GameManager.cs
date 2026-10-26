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
    public int bellRings = 3;             // Jumlah bunyi lonceng
    public float bellInterval = 1.2f;     // Jeda waktu antar bunyi (detik)

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
    public Unity.AI.Navigation.NavMeshSurface navMeshSurface;

    [Header("Blackout / Coins")]
    public BlackoutManager blackoutManager;

    [Header("Escape")]
    public GameObject escapeWayRoot;    // "Escape Way (Hidden)"
    public GameObject escapeWayBarrier; // final wall blocking the way out
    public GameObject[] temporaryWalls; // 3x wall (temporary) di Starting Room

    [Header("Game State")]
    public int coins = 0;
    public int debt = 0;
    public int lives = 3;
    public int settledPayments = 0; // Target is 3
    public int currentWave = 0;     // 0 = Free period, 1 = Wave 1, etc.
    public bool hasKey = false;     // key for the locked Main Hall rooms, found via the painting mechanic
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
        KeyInventory.ResetInventory();
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
                StartCoroutine(PlayBellRingsRoutine());
            }
        }
        else
        {
            OnWaveTimerEnd();
        }

        DisplayTime(timeRemaining);
    }

    System.Collections.IEnumerator PlayBellRingsRoutine()
    {
        for (int i = 0; i < bellRings; i++)
        {
            if (bellAudioSource != null && bellClip != null)
            {
                bellAudioSource.PlayOneShot(bellClip);
            }
            yield return new WaitForSeconds(bellInterval);
        }
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

    public void PlayCaptureSound()
    {
        if (sfxAudioSource != null && captureSound != null)
            sfxAudioSource.PlayOneShot(captureSound);
    }

    public void ResolveFailedCapture(CharacterController playerController)
    {
        if (isGameOver || hasWon) return;

        if (!DebugManager.Invincible) lives--;
        settledPayments = 0;
        ShowNotification("FAILED PAYMENT - Settled Payment Streak Lost");
        Debug.Log($"Cannot pay! Lost 1 life, settled payments reset. Lives left: {lives}");

        if (captureFlashImage != null) StartCoroutine(FlashCapture());
        // (Baris sfxAudioSource captureSound sudah dipindahkan ke saat pertama kali tertangkap)
        
        RespawnPlayerAtLobby(playerController);
        UpdateUI();

        if (!DebugManager.Invincible && lives <= 0)
        {
            TriggerGameOver("Unable to pay the debt.");
        }
    }

    public void OpenEscapeWay()
    {
        Debug.Log("<color=green>[ESCAPE] OpenEscapeWay() DIPANGGIL!</color>");

        if (escapeWayRoot != null)
        {
            escapeWayRoot.SetActive(true);
            Debug.Log("[ESCAPE] escapeWayRoot diaktifkan: " + escapeWayRoot.name);
        }
        else
        {
            Debug.LogError("[ESCAPE] escapeWayRoot KOSONG (None) di Inspector GameManager!");
        }

        if (escapeWayBarrier != null) escapeWayBarrier.SetActive(false);

        if (temporaryWalls != null)
        {
            foreach (var wall in temporaryWalls)
            {
                if (wall != null)
                {
                    wall.SetActive(false);
                    Debug.Log("[ESCAPE] wall dimatikan: " + wall.name);
                }
            }
        }

        SetObjective("Exit is open!");

        if (notificationRoutine != null) StopCoroutine(notificationRoutine);
        notificationRoutine = StartCoroutine(EscapeNotificationSequence());
    }

    System.Collections.IEnumerator EscapeNotificationSequence()
    {
        if (notificationText == null) yield break;

        notificationText.text = "All debts paid! Exit is open.";
        notificationText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(notificationDuration);

        notificationText.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(0.3f);

        notificationText.text = "Exit is in the Starting Room!";
        notificationText.gameObject.SetActive(true);
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

    public void CollectExitKey()
    {
        hasKey = true;
        ShowNotification("You have obtained the room key!");
        UpdateUI();
    }

    public void NotifyDoorUnlocked()
    {
        lockedDoorsRemaining = Mathf.Max(0, lockedDoorsRemaining - 1);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (coinsText) coinsText.text = $"Coins: {coins}";
        if (debtText) debtText.text = $"Debt: {debt}";

        if (waveText)
        {
            if (currentWave == 0)
                waveText.text = "Free Exploration";
            else
                waveText.text = $"Wave {currentWave}";
        }

        if (livesText)
        {
            string hearts = "";
            for (int i = 0; i < lives; i++) hearts += "♥ ";
            livesText.text = hearts.TrimEnd();
        }

        if (paymentsText) paymentsText.text = $"Settled Payment: {settledPayments}/3";

        if (keyText)
        {
            System.Collections.Generic.List<string> activeKeys = new System.Collections.Generic.List<string>();

            if (KeyInventory.HasKey("OfficeKey"))
                activeKeys.Add("Office Key");

            if (KeyInventory.HasKey("RepairKey"))
                activeKeys.Add("Repair Key");

            if (activeKeys.Count == 0)
            {
                keyText.text = "Key: None";
            }
            else
            {
                keyText.text = "Key: " + string.Join(", ", activeKeys);
            }
        }
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

    // Panggil efek darah langsung saat pertama kali tertangkap
    public void TriggerBloodyVision(float duration = 2.5f)
    {
        if (captureFlashImage != null)
        {
            StopCoroutine(nameof(BloodyVisionRoutine));
            StartCoroutine(BloodyVisionRoutine(duration));
        }
    }

    System.Collections.IEnumerator BloodyVisionRoutine(float duration)
    {
        Color baseColor = captureFlashImage.color;
        float elapsed = 0f;

        // 1. Muncul cepat (0.2 detik) saat pertama kali kena bacok
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 0.85f, elapsed / 0.2f);
            captureFlashImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }

        // 2. Berdenyut tegang selama sisa animasi serangan
        float pulseTime = 0f;
        while (pulseTime < (duration - 0.7f))
        {
            pulseTime += Time.unscaledDeltaTime;
            // Denyut darah antara alpha 0.65 sampai 0.85
            float pulseAlpha = 0.75f + Mathf.Sin(pulseTime * 8f) * 0.1f;
            captureFlashImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, pulseAlpha);
            yield return null;
        }

        // 3. Memudar perlahan (fade out 0.5 detik) saat respawn kembali ke lobby
        elapsed = 0f;
        float currentAlpha = captureFlashImage.color.a;
        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(currentAlpha, 0f, elapsed / 0.5f);
            captureFlashImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
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
        lockedDoorsRemaining = 2;
        KeyInventory.ResetInventory();
        timeRemaining = waveDuration;
        bellPlayedThisWave = false;
        isGameOver = false;
        hasWon = false;
        UpdateUI();
    }
}