using UnityEngine;

// The talking painting: offers to "double" the player's coins. It's a scam - accepting
// loses the coins offered, rejecting is rewarded slightly for being cautious.
public class ScamPainting : MonoBehaviour, IInteractable
{
    public int amountAsked = 20;
    public int amountPromised = 40;
    public int rejectReward = 2;
    bool used;

    public string GetPromptMessage() => used ? "[Lukisan diam]" : "[E] Dengarkan lukisan";

    public void Interact()
    {
        if (used) return;
        if (ChoicePanel.Instance == null) return;

        ChoicePanel.Instance.Show(
            $"\"Gandakan koinmu! Berikan {amountAsked}, aku kembalikan {amountPromised}.\"",
            "Terima", "Tolak",
            OnAccept, OnReject);
    }

    void OnAccept()
    {
        used = true;
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.coins < amountAsked)
        {
            gm.ShowNotification("Koinmu tidak cukup untuk ditawarkan.");
            used = false;
            return;
        }

        gm.AddCoins(-amountAsked);
        gm.ShowNotification("Money offered to you is not yours.");
    }

    void OnReject()
    {
        used = true;
        GameManager.Instance?.AddCoins(rejectReward);
        GameManager.Instance?.ShowNotification($"Kamu menolak. +{rejectReward} Coins.");
    }
}
