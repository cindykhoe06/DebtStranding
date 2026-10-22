using UnityEngine;

public class PaintingFragment : MonoBehaviour, IInteractable
{
    bool collected;

    public string GetPromptMessage() => "[E] Ambil fragmen lukisan";

    public void Interact()
    {
        if (collected) return;
        collected = true;
        TaskManager.Instance?.CollectFragment();
        Destroy(gameObject);
    }
}
