using UnityEngine;

public class SlidingDrawer : MonoBehaviour, IInteractable
{
    [Header("Slide Direction & Distance")]
    public Vector3 slideOffset = new Vector3(0, 0, 0.4f); // Distance it pulls out
    public float slideSpeed = 3f;

    [Header("Audio (Optional)")]
    public AudioSource drawerAudio;
    public AudioClip openSound;
    public AudioClip closeSound;

    private bool isOpen = false;
    private Vector3 closedPos;
    private Vector3 openPos;
    private Vector3 targetPos;

    void Start()
    {
        closedPos = transform.localPosition;
        openPos = closedPos + slideOffset;
        targetPos = closedPos;
    }

    void Update()
    {
        // Smooth slide
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * slideSpeed);
    }

    public void Interact()
    {
        isOpen = !isOpen;
        targetPos = isOpen ? openPos : closedPos;

        if (drawerAudio != null)
        {
            AudioClip clip = isOpen ? openSound : closeSound;
            if (clip != null) drawerAudio.PlayOneShot(clip);
        }
    }

    public string GetPromptMessage()
    {
        return isOpen ? "[E] Close Drawer" : "[E] Open Drawer";
    }
}