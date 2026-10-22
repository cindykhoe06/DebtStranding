using System.Collections.Generic;
using UnityEngine;

// Visitor Rules-styled instructions popup. Shows once automatically at game start, and can
// be reopened any time via the HUD icon, Tab, or I. Pauses the game while open.
public class InstructionsPopup : MonoBehaviour
{
    public static InstructionsPopup Instance;

    public GameObject panelRoot;
    bool openedBefore;
    bool wasPausedByPauseMenu; // avoid double-resuming if PauseMenu is also open

    // Section names expected in Assets/Resources/Text/instructions.txt - each one maps to an
    // existing child TMP object under Paper/Viewport/Content with the same name. If the file
    // or a section is missing, that object just keeps whatever text is already in the scene.
    static readonly string[] TextSections = { "Title", "Subtitle", "Rule1", "Rule2", "Rule3", "Rule4", "Rule5", "Rule6", "Rule7", "Controls", "Closing" };

    void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
        LoadTextOverrides();
    }

    void LoadTextOverrides()
    {
        var textAsset = Resources.Load<TextAsset>("Text/instructions");
        if (textAsset == null || panelRoot == null) return;

        var content = panelRoot.transform.Find("Paper/Viewport/Content");
        if (content == null) return;

        var sections = ParseSections(textAsset.text);
        foreach (var name in TextSections)
        {
            if (!sections.TryGetValue(name.ToUpperInvariant(), out var value)) continue;
            var child = content.Find(name);
            var tmp = child != null ? child.GetComponent<TMPro.TextMeshProUGUI>() : null;
            if (tmp != null) tmp.text = value;
        }
    }

    // Parses a simple "###KEY\n<content until next ###KEY>" format so multi-line sections
    // (e.g. Controls) work without needing a single-line KEY=VALUE scheme.
    static Dictionary<string, string> ParseSections(string raw)
    {
        var result = new Dictionary<string, string>();
        string currentKey = null;
        var currentValue = new System.Text.StringBuilder();

        foreach (var line in raw.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.StartsWith("###"))
            {
                if (currentKey != null) result[currentKey] = currentValue.ToString().TrimEnd('\n');
                currentKey = line.Substring(3).Trim().ToUpperInvariant();
                currentValue.Clear();
            }
            else if (currentKey != null)
            {
                currentValue.AppendLine(line);
            }
        }
        if (currentKey != null) result[currentKey] = currentValue.ToString().TrimEnd('\n');
        return result;
    }

    void Start()
    {
        Show(auto: true);
    }

    void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.HasWon)) return;

        bool isOpen = panelRoot != null && panelRoot.activeSelf;
        if ((Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I)) && !isOpen)
        {
            Show(auto: false);
        }
        else if (isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I)))
        {
            Close();
        }
    }

    public void Show(bool auto)
    {
        if (auto && openedBefore) return;
        openedBefore = true;

        if (panelRoot != null) panelRoot.SetActive(true);
        var scrollRect = panelRoot != null ? panelRoot.GetComponentInChildren<UnityEngine.UI.ScrollRect>() : null;
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f; // always open scrolled to the top
        Time.timeScale = 0f;
        GameManager.Instance?.SetInputLocked(true);
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        Time.timeScale = 1f;
        GameManager.Instance?.SetInputLocked(false);
    }
}
