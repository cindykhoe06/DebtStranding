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

        // 2. Satu sistem kunci untuk HUD/objective: KeyInventory di atas menyimpan ID kunci
        // per-pintu, CollectExitKey() yang mengatur flag "punya kunci", teks HUD "Kunci: Ada",
        // notifikasi, dan objective - sama seperti RoomKey (kunci lama) memakainya.
        GameManager.Instance?.CollectExitKey();

        Debug.Log($"Collected: {keyDisplayName}!");

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        Destroy(gameObject); // Kunci di lantai lenyap
    }
}