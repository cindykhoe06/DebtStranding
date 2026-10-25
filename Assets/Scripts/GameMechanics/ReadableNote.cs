using UnityEngine;

// A clue note the player can read with E - displays noteText directly from the Inspector.
public class ReadableNote : MonoBehaviour, IInteractable
{
    [Header("Note Content")]
    [TextArea(5, 12)]
    public string noteText = 
        @"October 12,

        ...I couldn't risk leaving the keys together.

        The paintings are the perfect disguise. Everyone looks at the artwork, but no one ever considers what might be hiding on the other side.

        I only hope I remember which ones...
        ";

    [Header("Interaction Prompt")]
    public string promptMessage = "[E] Read Note";

    public string GetPromptMessage() => promptMessage;

    public void Interact()
    {
        // Langsung tampilkan teks dari Inspector ke UI popup
        NotePopup.Instance?.Show(noteText);
    }
}