using UnityEngine;
using TMPro;

// Simplified "Peta Museum": pressing M shows a panel listing the current objective and
// which rooms are relevant, rather than a full spatial minimap (kept simple on purpose).
public class MapOverlay : MonoBehaviour
{
    public GameObject panelRoot;
    public TextMeshProUGUI mapText;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.hasMap) return;
        if (GameManager.Instance.IsInputLocked && !(panelRoot != null && panelRoot.activeSelf)) return;

        if (Input.GetKeyDown(KeyCode.M))
        {
            bool show = panelRoot != null && !panelRoot.activeSelf;
            if (panelRoot != null) panelRoot.SetActive(show);

            if (show && mapText != null)
            {
                var tm = TaskManager.Instance;
                string a = tm != null ? (tm.taskAComplete ? "selesai" : $"{tm.fragmentsCollected}/{tm.fragmentsNeeded} fragmen - cari di ruangan museum") : "-";
                string b = tm != null ? (tm.taskBComplete ? "selesai" : $"{tm.switchesActivated}/{tm.switchesNeeded} sakelar - cari di ruangan museum") : "-";
                string c = tm != null ? (tm.taskCComplete ? "selesai (KUNCI EXIT didapat)" : tm.taskCUnlocked ? "cari Arsip Pengunjung" : "terkunci (selesaikan A atau B)") : "-";
                mapText.text = $"PETA MUSEUM\n\nLukisan Hilang: {a}\nGenerator: {b}\nArsip Pengunjung: {c}\n\nPayment Area: dekat Lobby\nExit: Escape Way (perlu Kunci + 3/3 lunas)";
            }
        }
    }
}
