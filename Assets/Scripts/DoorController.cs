using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public float openAngle = 90f;      // How far it swings (try -90f if it swings the wrong way)
    public float openSpeed = 3f;       // Smooth opening speed
    public bool isLocked = false;      // Can be used for locked doors!

    [Header("Audio (Optional)")]
    public AudioSource doorAudio;
    public AudioClip openSound;
    public AudioClip closeSound;

    private bool isOpen = false;
    private bool playerIsNearby = false;
    private Quaternion closedRotation;
    private Quaternion targetRotation;

    void Start()
    {
        // Remember the original starting angle
        closedRotation = transform.localRotation;
        targetRotation = closedRotation;
    }

    void Update()
    {
        // Smoothly swing toward the target angle
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * openSpeed);

        // Check if player presses [E] while nearby
        if (playerIsNearby && Input.GetKeyDown(KeyCode.E))
        {
            ToggleDoor();
        }
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

    // Detect when player steps close to the door
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            playerIsNearby = true;
        }
    }

    // Detect when player walks away
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            playerIsNearby = false;
        }
    }
}