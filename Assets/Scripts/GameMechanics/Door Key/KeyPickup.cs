using UnityEngine;

public class KeyPickup : MonoBehaviour, IInteractable
{
    [Header("Key Info")]
    public string keyID = "OfficeKey";             // ID unik kunci (harus sama dengan pintu!)
    public string keyDisplayName = "Office Key";   // Nama spesifik yang muncul di HUD
    public AudioClip pickupSound;

    public string GetPromptMessage()
    {
        return $"[E] Take {keyDisplayName}";
    }

    public void Interact()
    {
        // 1. Catat ke sistem inventori static
        KeyInventory.AddKey(keyID);

        // 2. Tampilkan nama kunci spesifik di HUD GameManager!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GrantKey(keyDisplayName);
        }

        Debug.Log($"Collected: {keyDisplayName}!");

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        Destroy(gameObject); // Kunci di lantai lenyap
    }
}