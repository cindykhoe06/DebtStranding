using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float interactDistance = 2.5f;

    [Header("UI Reference")]
    public TextMeshProUGUI promptText; // Drag Text_InteractionPrompt here

    private Camera playerCam;

    void Start()
    {
        playerCam = GetComponentInChildren<Camera>();
        if (playerCam == null) playerCam = Camera.main;

        if (promptText != null) promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsInputLocked)
        {
            HidePrompt();
            return;
        }

        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        RaycastHit hit;

        // Cast a ray forward from the center of the camera
        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            Debug.Log("Hit object: " + hit.collider.gameObject.name);
            // Look for any component that implements IInteractable
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                ShowPrompt(interactable.GetPromptMessage());

                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact();
                }
                return;
            }
        }

        // Hide prompt if looking at a normal wall/air
        HidePrompt();
    }

    void ShowPrompt(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
            promptText.gameObject.SetActive(true);
        }
    }

    void HidePrompt()
    {
        if (promptText != null && promptText.gameObject.activeSelf)
        {
            promptText.gameObject.SetActive(false);
        }
    }
}