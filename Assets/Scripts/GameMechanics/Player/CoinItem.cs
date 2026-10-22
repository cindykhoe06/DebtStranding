using UnityEngine;

public class CoinItem : MonoBehaviour, IInteractable
{
    public int coinValue = 5;
    public AudioClip pickupSound;

    [Header("Idle animation")]
    public float rotateSpeed = 90f;   // degrees/sec around Y
    public float bobAmount = 0.1f;
    public float bobSpeed = 2f;

    bool collected;
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
        if (collected) return; // guards against a double Interact() call in the same frame
        collected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoins(coinValue);
            GameManager.Instance.ShowNotification($"+{coinValue} Coins");
        }

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        Destroy(gameObject);
    }
}
