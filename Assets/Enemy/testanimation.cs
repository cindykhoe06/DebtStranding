using UnityEngine;

public class AnimationTester : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        // Mengambil komponen Animator dari GameObject
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("Komponen Animator tidak ditemukan pada GameObject ini!");
        }
    }

    void Update()
    {
        if (animator == null) return;

        // Press 'I' for Idle
        if (Input.GetKeyDown(KeyCode.I))
        {
            animator.SetTrigger("Idle");
            Debug.Log("Playing Animation: Idle");
        }

        // Press 'W' for Walk
        if (Input.GetKeyDown(KeyCode.W))
        {
            animator.SetTrigger("Walk");
            Debug.Log("Playing Animation: Walk");
        }

        // Press 'R' for Run
        if (Input.GetKeyDown(KeyCode.R))
        {
            animator.SetTrigger("Run");
            Debug.Log("Playing Animation: Run");
        }

        // Press 'N' for Nunduk
        if (Input.GetKeyDown(KeyCode.N))
        {
            animator.SetTrigger("Nunduk");
            Debug.Log("Playing Animation: Nunduk");
        }

        // Press 'B' for Bacok
        if (Input.GetKeyDown(KeyCode.B))
        {
            animator.SetTrigger("Bacok");
            Debug.Log("Playing Animation: Bacok");
        }
    }
}