using UnityEngine;
using UnityEngine.Audio;

// Singleton pengaturan global, auto-dimuat lewat RuntimeInitializeOnLoadMethod (tidak perlu
// ditaruh manual di scene manapun) dan bertahan lintas-scene lewat DontDestroyOnLoad.
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    const string KeyMasterVolume = "MasterVolume";
    const string KeyMusicVolume = "MusicVolume";
    const string KeySFXVolume = "SFXVolume";
    const string KeyMouseSensitivity = "MouseSensitivity";

    // Pengaturan lama yang sudah dihapus (Brightness/Fullscreen/ResolutionIndex) - kunci
    // PlayerPrefs-nya dibersihkan sekali saat startup supaya nilai lama tidak nyangkut di mesin
    // siapapun yang pernah memakai build sebelumnya.
    static readonly string[] RemovedKeys = { "Brightness", "Fullscreen", "ResolutionIndex" };

    public const float DefaultVolume = 0.8f;
    public const float DefaultMouseSensitivity = 1f;
    public const float MinMouseSensitivity = 0.5f;
    public const float MaxMouseSensitivity = 2f;

    public static float MasterVolume { get; private set; }
    public static float MusicVolume { get; private set; }
    public static float SFXVolume { get; private set; }
    public static float MouseSensitivity { get; private set; }

    // Dibagikan ke script lain (CoinItem, RoomKey, dll.) yang membuat AudioSource sendiri saat
    // runtime, supaya ikut ter-routing ke mixer group yang benar tanpa perlu reload mixer sendiri.
    public static AudioMixerGroup MasterGroup { get; private set; }
    public static AudioMixerGroup MusicGroup { get; private set; }
    public static AudioMixerGroup SFXGroup { get; private set; }

    static AudioMixer runtimeMixer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("SettingsManager");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<SettingsManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        runtimeMixer = Resources.Load<AudioMixer>("MainMixer");
        if (runtimeMixer != null)
        {
            var masterGroups = runtimeMixer.FindMatchingGroups("Master");
            var musicGroups = runtimeMixer.FindMatchingGroups("Music");
            var sfxGroups = runtimeMixer.FindMatchingGroups("SFX");
            MasterGroup = masterGroups.Length > 0 ? masterGroups[0] : null;
            MusicGroup = musicGroups.Length > 0 ? musicGroups[0] : null;
            SFXGroup = sfxGroups.Length > 0 ? sfxGroups[0] : null;
        }

        foreach (var key in RemovedKeys)
            if (PlayerPrefs.HasKey(key)) PlayerPrefs.DeleteKey(key);

        LoadAll();
        ApplyAll();
    }

    public static void LoadAll()
    {
        MasterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, DefaultVolume);
        MusicVolume = PlayerPrefs.GetFloat(KeyMusicVolume, DefaultVolume);
        SFXVolume = PlayerPrefs.GetFloat(KeySFXVolume, DefaultVolume);
        MouseSensitivity = PlayerPrefs.GetFloat(KeyMouseSensitivity, DefaultMouseSensitivity);
    }

    public static void ApplyAll()
    {
        ApplyMasterVolume(MasterVolume);
        ApplyMusicVolume(MusicVolume);
        ApplySFXVolume(SFXVolume);
    }

    static float ToDb(float v) => Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f;

    public static void SetMasterVolume(float v)
    {
        MasterVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KeyMasterVolume, MasterVolume);
        ApplyMasterVolume(MasterVolume);
    }

    public static void SetMusicVolume(float v)
    {
        MusicVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KeyMusicVolume, MusicVolume);
        ApplyMusicVolume(MusicVolume);
    }

    public static void SetSFXVolume(float v)
    {
        SFXVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KeySFXVolume, SFXVolume);
        ApplySFXVolume(SFXVolume);
    }

    static void ApplyMasterVolume(float v) { if (runtimeMixer) runtimeMixer.SetFloat("MasterVol", ToDb(v)); }
    static void ApplyMusicVolume(float v) { if (runtimeMixer) runtimeMixer.SetFloat("MusicVol", ToDb(v)); }
    static void ApplySFXVolume(float v) { if (runtimeMixer) runtimeMixer.SetFloat("SFXVol", ToDb(v)); }

    public static void SetMouseSensitivity(float v)
    {
        MouseSensitivity = Mathf.Clamp(v, MinMouseSensitivity, MaxMouseSensitivity);
        PlayerPrefs.SetFloat(KeyMouseSensitivity, MouseSensitivity);
    }

    public static void ResetToDefault()
    {
        PlayerPrefs.DeleteKey(KeyMasterVolume);
        PlayerPrefs.DeleteKey(KeyMusicVolume);
        PlayerPrefs.DeleteKey(KeySFXVolume);
        PlayerPrefs.DeleteKey(KeyMouseSensitivity);
        LoadAll();
        ApplyAll();
    }
}
