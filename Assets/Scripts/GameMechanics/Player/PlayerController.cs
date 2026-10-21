using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 4.0f;
    public float sprintSpeed = 7.0f;
    public float gravity = -9.81f;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2.0f;
    public Transform playerCamera;
    public float lookXLimit = 80.0f;

    [Header("Head Bobbing (Cinematic Feel)")]
    public bool enableHeadBob = true;
    public float bobSpeed = 10f;
    public float bobAmount = 0.05f;

    [Header("Flashlight (Optional)")]
    public Light flashlight; // Drag a spotlight here

    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private float defaultCameraPosY = 0;
    private float bobTimer = 0;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Lock mouse to center of screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera != null)
        {
            defaultCameraPosY = playerCamera.localPosition.y;
        }
    }

    void Update()
    {
        // 1. MOUSE LOOK
        if (playerCamera != null)
        {
            rotationX += -Input.GetAxis("Mouse Y") * mouseSensitivity;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * mouseSensitivity, 0);
        }

        // 2. WASD MOVEMENT + SPRINT (LEFT SHIFT)
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float speed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float curSpeedX = speed * Input.GetAxis("Vertical");
        float curSpeedY = speed * Input.GetAxis("Horizontal");

        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // 3. GRAVITY
        if (controller.isGrounded)
        {
            moveDirection.y = -2f; // Stick to the floor
        }
        else
        {
            moveDirection.y = movementDirectionY + (gravity * Time.deltaTime);
        }

        controller.Move(moveDirection * Time.deltaTime);

        // 4. HEAD BOBBING
        if (enableHeadBob && playerCamera != null)
        {
            if (controller.isGrounded && (Mathf.Abs(curSpeedX) > 0.1f || Mathf.Abs(curSpeedY) > 0.1f))
            {
                bobTimer += Time.deltaTime * (isSprinting ? bobSpeed * 1.5f : bobSpeed);
                playerCamera.localPosition = new Vector3(
                    playerCamera.localPosition.x,
                    defaultCameraPosY + Mathf.Sin(bobTimer) * bobAmount,
                    playerCamera.localPosition.z
                );
            }
            else
            {
                bobTimer = 0;
                playerCamera.localPosition = new Vector3(
                    playerCamera.localPosition.x,
                    Mathf.Lerp(playerCamera.localPosition.y, defaultCameraPosY, Time.deltaTime * bobSpeed),
                    playerCamera.localPosition.z
                );
            }
        }

        // 5. FLASHLIGHT TOGGLE (PRESS 'F')
        if (flashlight != null && Input.GetKeyDown(KeyCode.F))
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }
}