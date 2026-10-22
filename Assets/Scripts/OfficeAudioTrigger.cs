using UnityEngine;

public class OfficeAudioTrigger : MonoBehaviour
{
    public Gramophone targetGramophone;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            if (targetGramophone != null)
            {
                targetGramophone.StartPlaying();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            if (targetGramophone != null)
            {
                targetGramophone.StopPlaying();
            }
        }
    }
}