using UnityEngine;
using System.Collections.Generic;

// Triggered by GameManager when a wave timer ends: flickers every museum light off, holds
// darkness for a few seconds, restores the lights, then tells CoinManager to relocate coins.
// Does NOT touch the player's flashlight, fog, or post-processing - only existing Light
// intensities, restored exactly afterwards.
public class BlackoutManager : MonoBehaviour
{
    public Transform lightsRoot;     // parent to search for Light components (e.g. "[ MAP MUSEUM ]")
    public Light[] extraLights;      // e.g. the Directional Light, not under lightsRoot
    public CoinManager coinManager;

    public float flickerDuration = 0.6f;
    public int flickerCount = 4;
    public Vector2 darknessDuration = new Vector2(3f, 5f);

    public AudioSource sfxSource;
    public AudioClip blackoutClip;

    Light[] lights;
    float[] originalIntensities;
    bool running;

    void Awake()
    {
        var found = new List<Light>();
        if (lightsRoot != null) found.AddRange(lightsRoot.GetComponentsInChildren<Light>(true));
        if (extraLights != null) found.AddRange(extraLights);

        lights = found.ToArray();
        originalIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            originalIntensities[i] = lights[i] != null ? lights[i].intensity : 0f;
    }

    public void TriggerBlackout()
    {
        if (running) return;
        StartCoroutine(BlackoutRoutine());
    }

    System.Collections.IEnumerator BlackoutRoutine()
    {
        running = true;
        if (sfxSource != null && blackoutClip != null) sfxSource.PlayOneShot(blackoutClip);

        // Flicker
        float t = 0f;
        bool on = true;
        while (t < flickerDuration)
        {
            SetAll(on ? 0f : 1f, useOriginal: on == false);
            on = !on;
            float step = flickerDuration / Mathf.Max(flickerCount, 1);
            yield return new WaitForSeconds(step);
            t += step;
        }

        // Full darkness
        SetAll(0f, useOriginal: false);
        float dark = Random.Range(darknessDuration.x, darknessDuration.y);
        yield return new WaitForSeconds(dark);

        // Restore
        RestoreAll();

        if (coinManager != null) coinManager.RespawnCoins();
        running = false;
    }

    void SetAll(float multiplier, bool useOriginal)
    {
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;
            lights[i].intensity = useOriginal ? originalIntensities[i] : originalIntensities[i] * multiplier;
        }
    }

    void RestoreAll()
    {
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;
            lights[i].intensity = originalIntensities[i];
        }
    }
}
