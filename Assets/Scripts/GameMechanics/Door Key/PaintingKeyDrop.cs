using UnityEngine;

public class PaintingKeyDrop : MonoBehaviour, IInteractable
{
    [Header("Painting Info")]
    public string paintingName = "Antique Portrait";

    [Header("Key to Drop")]
    public GameObject keyPrefab;          // Drag Prefab Kunci 3D ke sini
    public Transform dropSpawnPoint;      // Titik kunci jatuh (opsional)
    public Vector3 dropForce = new Vector3(0, -1f, 0.4f);

    [Header("Tilt Target Rotation")]
    // Sudut miring persis dari screenshot kamu:
    public Vector3 tiltedRotation = new Vector3(-83f, 270f, -90f);
    public float tiltSpeed = 4f;

    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip tiltSound;           // Suara kayu bergeser
    public AudioClip keyDropSound;        // Suara logam kunci jatuh

    private bool hasDroppedKey = false;
    private bool isTilting = false;
    private Quaternion targetRotation;

    void Start()
    {
        targetRotation = transform.localRotation;
    }

    void Update()
    {
        // Berayun miring secara halus
        if (isTilting)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * tiltSpeed);
        }
    }

    public string GetPromptMessage()
    {
        return hasDroppedKey ? $"[{paintingName} - Inspected]" : $"[E] Inspect {paintingName}";
    }

    public void Interact()
    {
        if (hasDroppedKey) return;
        hasDroppedKey = true;

        // Mulai animasi miring ke target rotasi
        targetRotation = Quaternion.Euler(tiltedRotation);
        isTilting = true;

        // Suara lukisan bergeser
        if (audioSource != null && tiltSound != null)
        {
            audioSource.PlayOneShot(tiltSound);
        }

        // Jatuhkan kunci
        DropKey();
    }

    void DropKey()
    {
        Vector3 spawnPos = dropSpawnPoint != null ? dropSpawnPoint.position : (transform.position + Vector3.down * 0.2f + transform.forward * 0.2f);

        if (keyPrefab != null)
        {
            GameObject droppedKey = Instantiate(keyPrefab, spawnPos, Quaternion.identity);

            Rigidbody rb = droppedKey.GetComponent<Rigidbody>();
            if (rb == null) rb = droppedKey.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(dropForce, ForceMode.Impulse);

            if (keyDropSound != null)
            {
                AudioSource.PlayClipAtPoint(keyDropSound, spawnPos);
            }

            Debug.Log($"A key dropped from {paintingName}!");
        }
    }
}