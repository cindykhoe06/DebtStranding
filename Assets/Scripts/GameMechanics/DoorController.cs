using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Door Movement Settings")]
    public float openAngle = 90f;        // Ubah ke -90f jika arah buka terbalik
    public float openSpeed = 3f;
    public bool isLocked = false;
    public bool requiresKey = false;     // if true, Interact() while locked checks GameManager.hasKey
    public string lockedMessage = "[Locked]";

    [Header("Audio - Open Sound")]
    public AudioClip openSound;
    public float openSkipSilence = 0.2f;

    [Header("Audio - Close Sound")]
    public AudioClip closeSound;
    public float closeSkipSilence = 0.1f;
    public float closeSpeedPitch = 1.35f;

    [Header("Audio Source")]
    public AudioSource doorAudio;

    private bool isOpen = false;
    private Vector3 lockedPosition;       // Posisi dikunci agar tidak bisa bergeser!
    private float closedYAngle;
    private float targetYAngle;
    private float currentYAngle;

    public bool IsOpen => isOpen;

    void Start()
    {
        // 1. Kunci posisi lokal agar pintu TIDAK BISA BERGESER masuk/keluar
        lockedPosition = transform.localPosition;

        // 2. Simpan sudut awal engsel
        closedYAngle = transform.localEulerAngles.y;
        currentYAngle = closedYAngle;
        targetYAngle = closedYAngle;

        if (doorAudio == null)
        {
            doorAudio = GetComponent<AudioSource>();
        }

        if (doorAudio != null)
        {
            doorAudio.playOnAwake = false;
            doorAudio.spatialBlend = 1.0f; // 3D Audio
            doorAudio.minDistance = 1.5f;
            doorAudio.maxDistance = 12f;
        }
    }

    void Update()
    {
        // KUNCI POSISI: Pastikan posisi lokal tidak pernah bergeser 1 milimeter pun!
        transform.localPosition = lockedPosition;

        // ROTASI MURNI: Hanya memutar sudut Y engsel secara halus
        currentYAngle = Mathf.LerpAngle(currentYAngle, targetYAngle, Time.deltaTime * openSpeed);
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, currentYAngle, transform.localEulerAngles.z);
    }

    public void Interact()
    {
        ToggleDoor();
    }

    public string GetPromptMessage()
    {
        if (isLocked) return requiresKey ? "[E] " + lockedMessage : lockedMessage;
        return isOpen ? "[E] Close Door" : "[E] Open Door";
    }

    public void ToggleDoor()
    {
        if (isLocked)
        {
            if (requiresKey && GameManager.Instance != null && GameManager.Instance.hasKey)
            {
                isLocked = false;
                GameManager.Instance.ShowNotification("Pintu terbuka.");
                // fall through and open it below
            }
            else
            {
                if (requiresKey) GameManager.Instance?.ShowNotification(lockedMessage);
                Debug.Log("The door is locked...");
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

    // Forces the door open and locks it so it can't be closed again (used for the Escape Way).
    public void OpenPermanently()
    {
        isLocked = false;
        isOpen = true;
        targetYAngle = closedYAngle + openAngle;
        PlayOpenAudio();
        isLocked = true; // locked AFTER opening so Interact() can no longer toggle it shut
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