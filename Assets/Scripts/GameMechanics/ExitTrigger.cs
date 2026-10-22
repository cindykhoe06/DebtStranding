using UnityEngine;

// Placed at the Escape Way exit point. Fires WIN once the player physically reaches it
// (GameManager.OnReachExit() itself still checks settledPayments >= 3 as a safety net).
public class ExitTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CharacterController>() != null)
        {
            GameManager.Instance?.OnReachExit();
        }
    }
}
