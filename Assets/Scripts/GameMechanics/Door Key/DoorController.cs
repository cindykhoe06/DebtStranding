using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Door Movement Settings")]
    public float openAngle = 90f;        // Ubah ke -90f jika buka terbalik
    public float openSpeed = 3f;

    [Header("Key Requirement")]
    public bool isLocked = false;
    public string requiredKeyID = "OfficeKey"; // Harus SAMA dengan keyID di KeyPickup!
    public string lockedMessage = "[Locked - Requires Office Key]";

    [Header("Audio - Open Sound")]
    public AudioClip openSound;
    public float openSkipSilence = 1.35f;

    [Header("Audio - Close Sound")]
    public AudioClip closeSound;
    public float closeSkipSilence = 0.1f;
    public float closeSpeedPitch = 1.35f;

    [Header("Audio Source")]
    public AudioSource doorAudio;

    private bool isOpen = false;
    private Vector3 lockedPosition;
    private float closedYAngle;
    private float targetYAngle;
    private float currentYAngle;

    void Start()
    {
        lockedPosition = transform.localPosition;
        closedYAngle = transform.localEulerAngles.y;
        currentYAngle = closedYAngle;
        targetYAngle = closedYAngle;

        if (doorAudio == null) doorAudio = GetComponent<AudioSource>();
    }

    void Update()
    {
        transform.localPosition = lockedPosition;
        currentYAngle = Mathf.LerpAngle(currentYAngle, targetYAngle, Time.deltaTime * openSpeed);
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, currentYAngle, transform.localEulerAngles.z);
    }

    public void Interact()
    {
        ToggleDoor();
    }

    public string GetPromptMessage()
    {
        if (isLocked)
        {
            if (KeyInventory.HasKey(requiredKeyID))
            {
                return "[E] Unlock Door";
            }
            return lockedMessage;
        }

        return isOpen ? "[E] Close Door" : "[E] Open Door";
    }

    public void ToggleDoor()
    {
        if (isLocked)
        {
            if (KeyInventory.HasKey(requiredKeyID))
            {
                isLocked = false; // Buka kunci permanen
                GameManager.Instance?.ShowNotification("Pintu terbuka.");
                GameManager.Instance?.NotifyDoorUnlocked();
                Debug.Log("Door unlocked!");
                // fall through and open it below
            }
            else
            {
                Debug.Log("You don't have the key!");
                return;
            }
        }

        isOpen = !isOpen;

        if (isOpen)
        {
            targetYAngle = closedYAngle + openAngle;
            PlayOpenAudio();
        }
        else
        {
            targetYAngle = closedYAngle;
            PlayCloseAudio();
        }
    }

    void PlayOpenAudio()
    {
        if (doorAudio != null && openSound != null)
        {
            doorAudio.Stop();
            doorAudio.clip = openSound;
            doorAudio.pitch = 1.0f;
            doorAudio.time = Mathf.Clamp(openSkipSilence, 0f, openSound.length - 0.1f);
            doorAudio.Play();
        }
    }

    void PlayCloseAudio()
    {
        if (doorAudio != null && closeSound != null)
        {
            doorAudio.Stop();
            doorAudio.clip = closeSound;
            doorAudio.pitch = closeSpeedPitch;
            doorAudio.time = Mathf.Clamp(closeSkipSilence, 0f, closeSound.length - 0.1f);
            doorAudio.Play();
        }
    }
}