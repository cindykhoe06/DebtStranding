using UnityEngine;

// The final Escape Way door: requires KUNCI EXIT (from Arsip Pengunjung) AND 3/3 settled
// payments. Opens with a rotate animation (reuses DoorController) then triggers WIN.
[RequireComponent(typeof(DoorController))]
public class ExitDoor : MonoBehaviour, IInteractable
{
    DoorController door;
    bool triggered;

    void Awake()
    {
        door = GetComponent<DoorController>();
    }

    public string GetPromptMessage() => "[E] Buka Pintu Exit";

    public void Interact()
    {
        if (triggered) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        bool hasKey = gm.hasExitKey;
        bool paidInFull = gm.settledPayments >= 3;

        if (hasKey && paidInFull)
        {
            triggered = true;
            door.OpenPermanently();
            gm.TriggerWin();
            return;
        }

        if (!hasKey && !paidInFull)
        {
            gm.ShowNotification("Pintu terkunci: butuh KUNCI EXIT dan lunasi 3/3 tagihan.");
        }
        else if (!hasKey)
        {
            gm.ShowNotification("Pintu terkunci: butuh KUNCI EXIT (selesaikan Arsip Pengunjung).");
        }
        else
        {
            gm.ShowNotification($"Pintu terkunci: lunasi tagihan dulu ({gm.settledPayments}/3).");
        }
    }
}
