using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Gramophone : MonoBehaviour
{
    [Header("Vinyl Disc Settings")]
    public Transform vinylDisc;        
    public float spinSpeed = 90f;      
    public Vector3 spinAxis = Vector3.up; 

    [Header("Audio Settings")]
    public AudioClip gramophoneMusic;  
    [Range(0f, 1f)] public float volume = 0.8f;

    private AudioSource audioSource;
    private bool isPlaying = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 1.0f; 
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 14f; 

        if (gramophoneMusic != null)
        {
            audioSource.clip = gramophoneMusic;
        }
    }

    void Update()
    {
        if (isPlaying && vinylDisc != null)
        {
            vinylDisc.Rotate(spinAxis * spinSpeed * Time.deltaTime, Space.Self);
        }
    }

    
    [Header("Start Offset (Skip Hening)")]
    public float startDelaySkip = 10f; 

    public void StartPlaying()
    {
        if (isPlaying) return;

        isPlaying = true;
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.volume = volume;
            
            // Langsung lompat melewati detik hening di awal
            audioSource.time = startDelaySkip; 
            
            audioSource.Play();
            Debug.Log("The antique gramophone began to play immediately!");
        }
    }

    // Call this if you ever want to stop it
    public void StopPlaying()
    {
        isPlaying = false;
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}