using UnityEngine;

// The key item found in Main Gallery, unlocks the 2 locked Main Hall rooms. Floats/spins
// in place (idle animation) until picked up.
public class RoomKey : MonoBehaviour, IInteractable
{
    public float rotateSpeed = 60f;
    public float bobAmount = 0.08f;
    public float bobSpeed = 1.5f;
    public AudioClip pickupSound;

    bool collected;
    Vector3 basePos;
    float bobTimer;

    void Start()
    {
        basePos = transform.localPosition;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        bobTimer += Time.deltaTime * bobSpeed;
        transform.localPosition = basePos + new Vector3(0f, Mathf.Sin(bobTimer) * bobAmount, 0f);
    }

    public string GetPromptMessage() => "[E] Ambil Kunci";

    public void Interact()
    {
        if (collected) return;
        collected = true;

        GameManager.Instance?.GrantKey();
        if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        GetComponent<Collider>().enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        Destroy(gameObject, 0.5f);
    }
}
