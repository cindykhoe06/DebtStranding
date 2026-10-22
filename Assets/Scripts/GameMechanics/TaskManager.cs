using UnityEngine;

// Coordinates the 3 tasks (Lukisan Hilang / Generator / Arsip Pengunjung) and keeps the
// HUD objective line (GameManager.waveText) up to date. One instance in Main Scene.
public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance;

    [Header("Task A: Lukisan Hilang")]
    public int fragmentsNeeded = 3;
    public int fragmentsCollected = 0;
    public bool taskAComplete;
    public DoorController mainGalleryDoor;
    public int taskARewardCoins = 15;

    [Header("Task B: Generator")]
    public int switchesNeeded = 3;
    public int switchesActivated = 0;
    public bool taskBComplete;
    public DoorController loungeDoor;
    public int taskBRewardCoins = 15;

    [Header("Task C: Arsip Pengunjung")]
    public bool taskCUnlocked;
    public bool taskCComplete;
    public int wrongBookPenalty = 3;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        RefreshObjective();
    }

    public void CollectFragment()
    {
        if (taskAComplete) return;
        fragmentsCollected = Mathf.Min(fragmentsCollected + 1, fragmentsNeeded);
        GameManager.Instance?.ShowNotification($"Fragmen lukisan: {fragmentsCollected}/{fragmentsNeeded}");
        RefreshObjective();
    }

    // Called by the empty frame when interacted with.
    public void TryPlaceFragments()
    {
        if (taskAComplete)
        {
            GameManager.Instance?.ShowNotification("Lukisan sudah lengkap terpasang.");
            return;
        }

        if (fragmentsCollected < fragmentsNeeded)
        {
            GameManager.Instance?.ShowNotification($"Butuh {fragmentsNeeded - fragmentsCollected} fragmen lagi.");
            return;
        }

        taskAComplete = true;
        GameManager.Instance?.AddCoins(taskARewardCoins);
        GameManager.Instance?.ShowNotification($"Lukisan Hilang selesai! +{taskARewardCoins} Coins. Main Gallery terbuka.");

        if (mainGalleryDoor != null) mainGalleryDoor.OpenPermanently();
        GameManager.Instance?.RebakeNavMesh();

        CheckUnlockTaskC();
        RefreshObjective();
    }

    public void ActivateSwitch()
    {
        if (taskBComplete) return;

        switchesActivated = Mathf.Min(switchesActivated + 1, switchesNeeded);
        GameManager.Instance?.ShowNotification($"Generator: {switchesActivated}/{switchesNeeded}");

        if (switchesActivated >= switchesNeeded)
        {
            taskBComplete = true;
            GameManager.Instance?.AddCoins(taskBRewardCoins);
            GameManager.Instance?.ShowNotification($"Generator selesai! +{taskBRewardCoins} Coins. Lounge terbuka.");

            if (loungeDoor != null) loungeDoor.OpenPermanently();
            GameManager.Instance?.RebakeNavMesh();

            CheckUnlockTaskC();
        }

        RefreshObjective();
    }

    void CheckUnlockTaskC()
    {
        if (!taskCUnlocked && (taskAComplete || taskBComplete))
        {
            taskCUnlocked = true;
            GameManager.Instance?.ShowNotification("Arsip Pengunjung terbuka! Cari catatan petunjuk.");
        }
    }

    public void TryReadBook(bool isCorrect, string bookLabel)
    {
        if (taskCComplete)
        {
            GameManager.Instance?.ShowNotification("Kamu sudah menemukan arsip yang benar.");
            return;
        }

        if (!taskCUnlocked)
        {
            GameManager.Instance?.ShowNotification("Arsip ini masih terkunci.");
            return;
        }

        if (isCorrect)
        {
            taskCComplete = true;
            GameManager.Instance?.GrantExitKey();
            GameManager.Instance?.ShowNotification($"Arsip '{bookLabel}' benar! KUNCI EXIT didapat.");
        }
        else
        {
            GameManager.Instance?.ShowNotification("Bukan ini...");
            if (GameManager.Instance != null)
                GameManager.Instance.AddCoins(-wrongBookPenalty);
        }

        RefreshObjective();
    }

    public void RefreshObjective()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (taskCComplete)
        {
            gm.SetObjective(gm.settledPayments >= 3 ? "Objective: Menuju EXIT di Escape Way!" : "Objective: Lunasi sisa tagihan di Payment Area.");
            return;
        }

        if (taskCUnlocked)
        {
            gm.SetObjective("Objective: Cari Arsip Pengunjung - baca petunjuk, pilih buku yang benar.");
            return;
        }

        if (gm.currentWave == 0 && !taskAComplete && !taskBComplete && fragmentsCollected == 0 && switchesActivated == 0)
        {
            gm.SetObjective("Free Exploration");
            return;
        }

        string partA = taskAComplete ? "Lukisan Hilang (selesai)" : $"Lukisan Hilang ({fragmentsCollected}/{fragmentsNeeded})";
        string partB = taskBComplete ? "Generator (selesai)" : $"Generator ({switchesActivated}/{switchesNeeded})";
        gm.SetObjective($"Objective: {partA}  |  {partB}");
    }
}
