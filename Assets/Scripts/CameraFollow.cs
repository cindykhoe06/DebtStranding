using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform hero;

    public float mouseSensitivity = 200f;
    public float cameraHeight = 1f;

    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yRotation = hero.eulerAngles.y;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);

        transform.position = hero.position + Vector3.up * cameraHeight;
    }
}