using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExitTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // Cek jika yang menginjak tangga adalah Player (bisa via Tag "Player" atau CharacterController)
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null || other.name == "hero")
        {
            hasTriggered = true;
            Debug.Log("<color=green>Player reached exit trigger!</color>");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnReachExit();
            }
        }
    }
}