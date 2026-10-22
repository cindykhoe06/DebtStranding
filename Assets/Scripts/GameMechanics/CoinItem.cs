using UnityEngine;

public class CoinItem : MonoBehaviour, IInteractable
{
    public int coinValue = 5;
    public AudioClip pickupSound;
    public float skipSilenceTime = 0.15f; // Ubah angka ini (misal 0.1 sampai 0.3 detik) untuk potong hening!

    [Header("Idle animation")]
    public float rotateSpeed = 90f;   // degrees/sec around Y
    public float bobAmount = 0.1f;
    public float bobSpeed = 2f;

    private bool isCollected = false;
    Vector3 basePos;
    float bobTimer;

    void Start()
    {
        basePos = transform.localPosition;
        bobTimer = Random.Range(0f, 10f); // desync multiple coins
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        bobTimer += Time.deltaTime * bobSpeed;
        transform.localPosition = basePos + new Vector3(0f, Mathf.Sin(bobTimer) * bobAmount, 0f);
    }

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
            GameManager.Instance.ShowNotification($"+{coinValue} Coins");
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
