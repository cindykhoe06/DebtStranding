using UnityEngine;

// A readable note giving the player the hint for which archive book is correct.
public class ClueNote : MonoBehaviour, IInteractable
{
    [TextArea] public string clueMessage = "Catatan: \"Carilah buku arsip berlabel '1985'.\"";

    public string GetPromptMessage() => "[E] Baca catatan";

    public void Interact() => GameManager.Instance?.ShowNotification(clueMessage);
}
