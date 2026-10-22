using UnityEngine;

public class CoinItem : MonoBehaviour, IInteractable
{
    public int coinValue = 5;
    public AudioClip pickupSound;
    public float skipSilenceTime = 0.15f; // Ubah angka ini (misal 0.1 sampai 0.3 detik) untuk potong hening!

    private bool isCollected = false;

    public string GetPromptMessage()
    {
        return $"[E] Take {coinValue} Coins";
    }

    public void Interact()
    {
        if (isCollected) return;
        isCollected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoins(coinValue);
        }

        if (pickupSound != null)
        {
            // Buat audio sementara yang langsung melompat ke detik suara cling
            GameObject tempAudio = new GameObject("TempCoinAudio");
            tempAudio.transform.position = Camera.main.transform.position;
            AudioSource source = tempAudio.AddComponent<AudioSource>();
            source.clip = pickupSound;
            source.spatialBlend = 0f; // 2D jelas di telinga
            source.time = skipSilenceTime; // LOMPAT MELEWATI DETIK KOSONG!
            source.Play();

            Destroy(tempAudio, pickupSound.length);
        }

        // Sembunyikan koin instan
        GetComponent<Collider>().enabled = false;
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        Destroy(gameObject, 0.5f);
    }
}