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

    [Header("Walking Sound (Looping Track)")]
    public AudioSource footstepSource;
    public AudioClip footstepLoopClip; // Masukkan audio walking effect kamu di sini

    [Header("Flashlight (Optional)")]
    public Light flashlight; // Drag a spotlight here

    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private float defaultCameraPosY = 0;
    private float bobTimer = 0;
    private float stopDelayTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera != null)
        {
            defaultCameraPosY = playerCamera.localPosition.y;
        }

        // Setting AudioSource untuk looping
        if (footstepSource != null && footstepLoopClip != null)
        {
            footstepSource.clip = footstepLoopClip;
            footstepSource.loop = true;
            footstepSource.playOnAwake = false;
            footstepSource.Stop();
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsInputLocked) return;

        // 1. MOUSE LOOK (base sensitivity scaled live by the Settings menu's multiplier)
        if (playerCamera != null)
        {
            float sensitivity = mouseSensitivity * SettingsManager.MouseSensitivity;
            rotationX += -Input.GetAxis("Mouse Y") * sensitivity;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * sensitivity, 0);
        }

        // 2. INPUT GERAKAN
        float inputX = Input.GetAxisRaw("Horizontal"); // A / D
        float inputY = Input.GetAxisRaw("Vertical");   // W / S

        bool isMoving = (Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputY) > 0.1f);
        bool isSprinting = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        float speed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        Vector3 inputVector = new Vector3(inputX, 0, inputY);
        if (inputVector.sqrMagnitude > 1f) inputVector.Normalize();

        moveDirection = (forward * inputVector.z * speed) + (right * inputVector.x * speed);
        

        // 3. GRAVITASI
        if (controller.isGrounded)
        {
            moveDirection.y = -2f;
        }
        else
        {
            moveDirection.y += gravity * Time.deltaTime;
        }

        controller.Move(moveDirection * Time.deltaTime);

        // 4. SISTEM SUARA JALAN (DIPANGGIL DI SINI)
        HandleWalkingSound(isMoving, isSprinting);

        // 5. HEAD BOBBING
        if (enableHeadBob && playerCamera != null)
        {
            if (controller.isGrounded && isMoving)
            {
                bobTimer += Time.deltaTime * (isSprinting ? bobSpeed * 1.4f : bobSpeed);
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

        // 6. TOGGLE SENTER ('F')
        if (flashlight != null && Input.GetKeyDown(KeyCode.F))
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }

    // FUNGSI LOGIKA SUARA JALAN (DITARUH DI LUAR UPDATE)
    void HandleWalkingSound(bool isMoving, bool isSprinting)
    {
        if (footstepSource == null || footstepLoopClip == null) return;

        if (controller.isGrounded && isMoving)
        {
            // Beri waktu toleransi agar 1 hentakan langkah pertama tetap terdengar tuntas
            stopDelayTimer = 0.35f;

            footstepSource.pitch = isSprinting ? 1.35f : 1.0f;

            if (!footstepSource.isPlaying)
            {
                footstepSource.time = 0.05f; 
                footstepSource.Play();
            }
        }
        else
        {
            // Saat berhenti, jangan diputus seketika agar langkah tidak terpotong kasar
            if (stopDelayTimer > 0f)
            {
                stopDelayTimer -= Time.deltaTime;
            }
            else
            {
                if (footstepSource.isPlaying)
                {
                    footstepSource.Stop();
                }
            }
        }
    }
}