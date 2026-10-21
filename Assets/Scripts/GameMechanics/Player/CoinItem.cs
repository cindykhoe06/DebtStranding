using UnityEngine;

public class CoinItem : MonoBehaviour, IInteractable
{
    public int coinValue = 5;
    public AudioClip pickupSound;

    public string GetPromptMessage()
    {
        return $"[E] Take {coinValue} Coins";
    }

    public void Interact()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoins(coinValue);
        }

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        Destroy(gameObject);
    }
}