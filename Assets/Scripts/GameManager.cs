using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI - Top Left (Finances)")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI debtText;

    [Header("UI - Top Right (Timer & Waves)")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI waveText;

    [Header("UI - Bottom Left (Survival & Goal)")]
    public TextMeshProUGUI livesText; // Or GameObjects for hearts
    public TextMeshProUGUI paymentsText;

    [Header("Game State")]
    public int coins = 0;
    public int debt = 0;
    public int lives = 3;
    public int settledPayments = 0; // Target is 3
    public int currentWave = 0;     // 0 = Free period, 1 = Wave 1, etc.

    [Header("Timer Settings")]
    public float waveDuration = 300f; // 300 seconds = 5 minutes
    private float timeRemaining;
    private bool isGameOver = false;
    private bool hasWon = false;

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

        // Countdown timer
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else
        {
            OnWaveTimerEnd();
        }

        DisplayTime(timeRemaining);
    }

    void DisplayTime(float time)
    {
        float minutes = Mathf.FloorToInt(time / 60);
        float seconds = Mathf.FloorToInt(time % 60);
        timerText.text = string.Format("Next Bill In:  {0:00}:{1:00}", minutes, seconds);
    }

    // Called every time the 10-minute timer hits 0:00
    void OnWaveTimerEnd()
    {
        currentWave++;
        timeRemaining = waveDuration; // Reset to 10 minutes

        if (currentWave == 1)
        {
            // Free period just ended! First debt hits
            debt = 20;
            Debug.Log("Free exploration ended! First debt wave starts: 20 coins.");
        }
        else
        {
            // Unpaid previous debt gets 20% interest + 20 new debt
            int interest = Mathf.RoundToInt(debt * 0.20f);
            debt = debt + interest + 20;
            Debug.Log($"New wave! 20% interest ({interest}) + 20 new debt = {debt} total.");
        }

        UpdateUI();
    }

    // Call this when picking up coins in the map
    public void AddCoins(int amount)
    {
        coins += amount;
        UpdateUI();
    }

    // Call this when meeting the Usher
    public void OnEncounterUsher()
    {
        if (debt == 0)
        {
            Debug.Log("Usher: 'You owe nothing for this period.'");
            return;
        }

        if (coins >= debt)
        {
            // SUCCESSFUL PAYMENT
            coins -= debt;
            debt = 0;
            settledPayments++;
            Debug.Log($"Payment successful! Streak: {settledPayments}/3");

            if (settledPayments >= 3)
            {
                TriggerWin();
            }
        }
        else
        {
            // FAILED PAYMENT
            lives--;
            settledPayments = 0; // Streak resets to 0/3!
            Debug.Log($"Cannot pay! Lost 1 life. Streak reset to 0/3. Lives left: {lives}");

            if (lives <= 0)
            {
                TriggerGameOver();
            }
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        // Top Left
        if (coinsText) coinsText.text = $"Coins: {coins}";
        if (debtText)  debtText.text  = $"Debt: {debt}";

        // Top Right
        if (waveText)
        {
            if (currentWave == 0) waveText.text = "Free Exploration";
            else waveText.text = $"Wave {currentWave}";
        }

        // Bottom Left
        if (livesText)
        {
            string hearts = "";
            for (int i = 0; i < lives; i++) hearts += "♥ ";
            livesText.text = hearts.TrimEnd();
        }

        if (paymentsText)
        {
            paymentsText.text = $"Settled Payment: {settledPayments}/3";
        }
    }

    void TriggerGameOver()
    {
        isGameOver = true;
        Debug.Log("GAME OVER - The ushers claimed your soul.");
        // Show Game Over UI panel here
    }

    void TriggerWin()
    {
        hasWon = true;
        Debug.Log("VICTORY - 3/3 payments settled! Usher unlocks the hidden exit.");
        // Trigger Usher escape escort here
    }
}