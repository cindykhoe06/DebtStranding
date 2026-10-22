using UnityEngine;

public class ArchiveBook : MonoBehaviour, IInteractable
{
    public string bookLabel = "1985";
    public bool isCorrect = false;

    public string GetPromptMessage() => $"[E] Baca buku arsip '{bookLabel}'";

    public void Interact() => TaskManager.Instance?.TryReadBook(isCorrect, bookLabel);
}
