using UnityEngine;

// Loket near spawn - voluntary debt payment. Reuses GameManager.OnEncounterUsher() exactly
// as already implemented (coins >= debt -> paid, streak++; otherwise -> life lost).
public class PaymentArea : MonoBehaviour, IInteractable
{
    public string GetPromptMessage() => "[E] Bayar Tagihan di Loket";

    public void Interact() => GameManager.Instance?.OnEncounterUsher();
}
