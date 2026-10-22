using UnityEngine;

// A clue note the player can read with E - opens the shared NotePopup instead of carrying
// its own UI. Used for the Main Gallery key clues.
public class ReadableNote : MonoBehaviour, IInteractable
{
    [TextArea(3, 10)]
    public string noteText = "Catatan kosong.";

    public string GetPromptMessage() => "[E] Baca Catatan";

    public void Interact()
    {
        var textAsset = Resources.Load<TextAsset>("Text/clue_key");
        string text = textAsset != null ? textAsset.text.TrimEnd('\n', '\r') : noteText;
        NotePopup.Instance?.Show(text);
    }
}
