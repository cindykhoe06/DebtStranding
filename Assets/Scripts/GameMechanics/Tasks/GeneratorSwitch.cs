using UnityEngine;

// A lever that animates (rotates) when pulled. Counts once toward TaskManager's generator task.
public class GeneratorSwitch : MonoBehaviour, IInteractable
{
    public float pulledAngle = -45f;
    public float animSpeed = 4f;

    bool activated;
    Quaternion idleRotation;
    Quaternion targetRotation;

    void Start()
    {
        idleRotation = transform.localRotation;
        targetRotation = idleRotation;
    }

    void Update()
    {
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * animSpeed);
    }

    public string GetPromptMessage() => activated ? "[Sakelar sudah aktif]" : "[E] Tarik sakelar";

    public void Interact()
    {
        if (activated) return;
        activated = true;
        targetRotation = idleRotation * Quaternion.Euler(pulledAngle, 0f, 0f);
        TaskManager.Instance?.ActivateSwitch();
    }
}
