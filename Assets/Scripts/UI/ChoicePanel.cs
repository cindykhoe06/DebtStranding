using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// Generic reusable Accept/Reject popup (used by the talking painting; could be reused by
// any future yes/no prompt). Locks player+Candleman input and shows the cursor while open.
public class ChoicePanel : MonoBehaviour
{
    public static ChoicePanel Instance;

    public GameObject panelRoot;
    public TextMeshProUGUI messageText;
    public Button acceptButton;
    public Button rejectButton;
    public TextMeshProUGUI acceptLabel;
    public TextMeshProUGUI rejectLabel;

    Action onAccept;
    Action onReject;

    void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
        if (acceptButton != null) acceptButton.onClick.AddListener(Accept);
        if (rejectButton != null) rejectButton.onClick.AddListener(Reject);
    }

    public void Show(string message, string acceptText, string rejectText, Action accept, Action reject)
    {
        onAccept = accept;
        onReject = reject;
        if (messageText != null) messageText.text = message;
        if (acceptLabel != null) acceptLabel.text = acceptText;
        if (rejectLabel != null) rejectLabel.text = rejectText;
        if (panelRoot != null) panelRoot.SetActive(true);
        GameManager.Instance?.SetInputLocked(true);
    }

    void Accept()
    {
        Close();
        onAccept?.Invoke();
    }

    void Reject()
    {
        Close();
        onReject?.Invoke();
    }

    void Update()
    {
        if (panelRoot != null && panelRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Reject();
    }

    void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        GameManager.Instance?.SetInputLocked(false);
    }

    void OnDestroy()
    {
        if (acceptButton != null) acceptButton.onClick.RemoveListener(Accept);
        if (rejectButton != null) rejectButton.onClick.RemoveListener(Reject);
    }
}
