using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public float openAngle = 90f;      // Try -90f if it swings the wrong way
    public float openSpeed = 3f;
    public bool isLocked = false;

    [Header("Audio (Optional)")]
    public AudioSource doorAudio;
    public AudioClip openSound;
    public AudioClip closeSound;

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion targetRotation;

    void Start()
    {
        closedRotation = transform.localRotation;
        targetRotation = closedRotation;
    }

    void Update()
    {
        // Smoothly swing toward the target rotation
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * openSpeed);
        
        // NOTE: We do NOT check Input.GetKeyDown(KeyCode.E) here anymore!
        // PlayerInteraction.cs handles the [E] press via Interact()!
    }

    // Called automatically by PlayerInteraction.cs when pressing [E]
    public void Interact()
    {
        ToggleDoor();
    }

    public string GetPromptMessage()
    {
        if (isLocked) return "[Locked]";
        return isOpen ? "[E] Close Door" : "[E] Open Door";
    }

    public void ToggleDoor()
    {
        if (isLocked)
        {
            Debug.Log("The door is locked...");
            return;
        }

        isOpen = !isOpen;

        if (isOpen)
        {
            targetRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
            PlaySound(openSound);
        }
        else
        {
            targetRotation = closedRotation;
            PlaySound(closeSound);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (doorAudio != null && clip != null)
        {
            doorAudio.PlayOneShot(clip);
        }
    }
}