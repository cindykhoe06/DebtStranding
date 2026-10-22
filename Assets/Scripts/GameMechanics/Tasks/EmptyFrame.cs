using UnityEngine;

public class EmptyFrame : MonoBehaviour, IInteractable
{
    public string GetPromptMessage() => "[E] Pasang fragmen lukisan";

    public void Interact() => TaskManager.Instance?.TryPlaceFragments();
}
