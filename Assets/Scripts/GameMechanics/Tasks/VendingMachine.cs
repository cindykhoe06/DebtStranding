using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Receipt-styled vending machine near spawn. Battery and Map are needs; Lucky Charm is an
// expensive want with no effect. Opening the panel pauses player + Candleman input.
public class VendingMachine : MonoBehaviour, IInteractable
{
    [Header("Prices")]
    public int batteryPrice = 10;
    public int mapPrice = 10;
    public int luckyCharmPrice = 25;

    [Header("UI")]
    public GameObject panelRoot;
    public Button batteryButton;
    public Button mapButton;
    public Button luckyCharmButton;
    public Button closeButton;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (batteryButton != null) batteryButton.onClick.AddListener(BuyBattery);
        if (mapButton != null) mapButton.onClick.AddListener(BuyMap);
        if (luckyCharmButton != null) luckyCharmButton.onClick.AddListener(BuyLuckyCharm);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
    }

    void Update()
    {
        if (panelRoot != null && panelRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape)) ClosePanel();
    }

    public string GetPromptMessage() => "[E] Buka Vending Machine";

    public void Interact()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        GameManager.Instance?.SetInputLocked(true);
    }

    void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        GameManager.Instance?.SetInputLocked(false);
    }

    bool TrySpend(int price)
    {
        var gm = GameManager.Instance;
        if (gm == null) return false;
        if (gm.coins < price)
        {
            gm.ShowNotification("Koin tidak cukup.");
            return false;
        }
        gm.AddCoins(-price);
        return true;
    }

    void BuyBattery()
    {
        var gm = GameManager.Instance;
        if (!TrySpend(batteryPrice)) return;
        gm.SetBattery(gm.maxFlashlightBattery);
        gm.ShowNotification("Baterai senter terisi penuh.");
    }

    void BuyMap()
    {
        var gm = GameManager.Instance;
        if (gm.hasMap) { gm.ShowNotification("Kamu sudah punya Peta Museum."); return; }
        if (!TrySpend(mapPrice)) return;
        gm.hasMap = true;
        gm.ShowNotification("Peta Museum didapat. Tekan [M] untuk melihat objective.");
    }

    void BuyLuckyCharm()
    {
        var gm = GameManager.Instance;
        if (!TrySpend(luckyCharmPrice)) return;
        gm.ShowNotification("Kamu membeli Lucky Charm. (Tidak ada efek.)");
    }

    void OnDestroy()
    {
        if (batteryButton != null) batteryButton.onClick.RemoveListener(BuyBattery);
        if (mapButton != null) mapButton.onClick.RemoveListener(BuyMap);
        if (luckyCharmButton != null) luckyCharmButton.onClick.RemoveListener(BuyLuckyCharm);
        if (closeButton != null) closeButton.onClick.RemoveListener(ClosePanel);
    }
}
