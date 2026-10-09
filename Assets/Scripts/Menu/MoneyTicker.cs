using System.Text;
using TMPro;
using UnityEngine;

// Pasang di TMP_Text samar di belakang judul (alpha ~0.15-0.25, font monospace).
// Angka Rupiah berganti-ganti seperti mesin kasir yang error.
public class MoneyTicker : MonoBehaviour
{
    public TMP_Text text;
    public string header = "DEBT";
    public int lines = 3;
    public Vector2Int range = new Vector2Int(500, 25000);   // dalam ribuan
    public Vector2 updateInterval = new Vector2(0.4f, 1.5f);

    float next;
    readonly StringBuilder sb = new StringBuilder();

    void Reset() => text = GetComponent<TMP_Text>();

    void Update()
    {
        if (!text || Time.unscaledTime < next) return;
        next = Time.unscaledTime + Random.Range(updateInterval.x, updateInterval.y);

        sb.Clear();
        sb.AppendLine(header);
        for (int i = 0; i < lines; i++)
        {
            long value = Random.Range(range.x, range.y) * 1000L;
            sb.AppendLine("Rp " + value.ToString("N0").Replace(',', '.'));
        }
        text.text = sb.ToString();
    }
}
