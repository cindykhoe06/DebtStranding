using UnityEngine;

public class HeroMove : MonoBehaviour
{
    public float speed = 5f;
    public Transform cameraTransform;

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        Vector3 movement = (forward * v + right * h).normalized;

        transform.Translate(movement * speed * Time.deltaTime, Space.World);
    }
}