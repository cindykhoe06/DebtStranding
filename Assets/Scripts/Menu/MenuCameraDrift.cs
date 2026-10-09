using UnityEngine;

// Pasang di Main Camera scene MainMenu.
// Kamera pelan-pelan maju + bergoyang halus (seperti napas), plus sedikit parallax ikut posisi mouse.
public class MenuCameraDrift : MonoBehaviour
{
    [Header("Push-in")]
    public float pushDistance = 1.5f;
    public float pushDuration = 60f;   // detik sampai jarak penuh, lalu bolak-balik

    [Header("Sway")]
    public float swayAmount = 0.03f;
    public float swaySpeed = 0.3f;

    [Header("Mouse parallax (derajat)")]
    public float mouseTilt = 1.5f;
    public float smooth = 2f;

    Vector3 startPos;
    Quaternion startRot;
    Vector2 currentTilt;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;
    }

    void Update()
    {
        float t = Time.time;

        float push = Mathf.PingPong(t / pushDuration, 1f);
        push = Mathf.SmoothStep(0f, 1f, push) * pushDistance;

        Vector3 sway = new Vector3(
            Mathf.Sin(t * swaySpeed) * swayAmount,
            Mathf.Sin(t * swaySpeed * 1.3f) * swayAmount * 0.6f,
            0f);

        transform.position = startPos + startRot * (Vector3.forward * push + sway);

        Vector2 mouse = new Vector2(
            (Input.mousePosition.x / Screen.width - 0.5f) * 2f,
            (Input.mousePosition.y / Screen.height - 0.5f) * 2f);
        currentTilt = Vector2.Lerp(currentTilt, mouse * mouseTilt, Time.deltaTime * smooth);

        transform.rotation = startRot * Quaternion.Euler(-currentTilt.y, currentTilt.x, 0f);
    }
}
