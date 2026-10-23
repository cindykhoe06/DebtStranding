using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CrouchZone : MonoBehaviour
{
    private void Awake()
    {
        // Pastikan box collider ini adalah Trigger
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Jika yang lewat adalah Candleman
        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemy == null) enemy = other.GetComponentInParent<EnemyAI>();

        if (enemy != null)
        {
            enemy.SetCrouching(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemy == null) enemy = other.GetComponentInParent<EnemyAI>();

        if (enemy != null)
        {
            enemy.SetCrouching(false);
        }
    }
}