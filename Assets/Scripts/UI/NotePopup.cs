using UnityEngine;
using TMPro;

// Shared paper-popup reader for ReadableNote clues. One panel, reused for every note in the
// scene (so placing a new clue never means duplicating UI) - same VT323/#2A1A12 paper style
// as InstructionsPopup, closable with E or Escape, pauses the game while open.
public class NotePopup : MonoBehaviour
{
    public static NotePopup Instance;

    public GameObject panelRoot;
    public TextMeshProUGUI bodyText;

    bool isOpen;

    void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Update()
    {
        if (!isOpen) return;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public void Show(string text)
    {
        if (bodyText != null) bodyText.text = text;
        if (panelRoot != null) panelRoot.SetActive(true);
        isOpen = true;
        Time.timeScale = 0f;
        GameManager.Instance?.SetInputLocked(true);
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        isOpen = false;
        Time.timeScale = 1f;
        GameManager.Instance?.SetInputLocked(false);
    }
}
