using UnityEngine;
using TMPro;

// Editor-only testing helpers. All #if UNITY_EDITOR blocks are stripped from real builds,
// so the static flags below simply stay false forever outside the Editor.
public class DebugManager : MonoBehaviour
{
    public static bool EnemyDisabled { get; private set; }
    public static bool Invincible { get; private set; }

    [Header("UI (optional)")]
    public TextMeshProUGUI debugLabel;

    void Awake()
    {
#if UNITY_EDITOR
        EnemyDisabled = false;
        Invincible = false;
        UpdateLabel();
#else
        if (debugLabel != null) debugLabel.gameObject.SetActive(false);
        enabled = false;
#endif
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            EnemyDisabled = !EnemyDisabled;
            Debug.Log("[DEBUG] Enemy " + (EnemyDisabled ? "DISABLED" : "ENABLED"));
            UpdateLabel();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (GameManager.Instance != null) GameManager.Instance.AddCoins(10);
            Debug.Log("[DEBUG] +10 coins");
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            Invincible = !Invincible;
            Debug.Log("[DEBUG] Invincible " + (Invincible ? "ON" : "OFF"));
            UpdateLabel();
        }
    }

    void UpdateLabel()
    {
        if (debugLabel == null) return;
        bool any = EnemyDisabled || Invincible;
        debugLabel.gameObject.SetActive(any);
        debugLabel.text = "DEBUG" + (EnemyDisabled ? " [Enemy OFF]" : "") + (Invincible ? " [Invincible]" : "");
    }
#endif
}
