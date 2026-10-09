using System.Collections.Generic;
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
    const string KeyBrightness = "Brightness";
    const string KeyFullscreen = "Fullscreen";
    const string KeyResolutionIndex = "ResolutionIndex";

    public const float DefaultVolume = 0.8f;
    public const float DefaultMouseSensitivity = 1f;
    public const float DefaultBrightness = 1f;
    public const bool DefaultFullscreen = true;

    public struct ResolutionOption
    {
        public int width;
        public int height;
        public bool isNative;
    }

    public static float MasterVolume { get; private set; }
    public static float MusicVolume { get; private set; }
    public static float SFXVolume { get; private set; }
    public static float MouseSensitivity { get; private set; }
    public static float Brightness { get; private set; }
    public static bool Fullscreen { get; private set; }
    public static int ResolutionIndex { get; private set; }

    public static List<ResolutionOption> ResolutionOptions { get; private set; } = new List<ResolutionOption>();

    // Dibaca BrightnessOverlay (UI) dan nanti bisa dipakai di level (mis. Color Adjustments post exposure).
    public static System.Action<float> OnBrightnessChanged;

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
        BuildResolutionOptions();
        LoadAll();
        ApplyAll();
    }

    // Daftar tetap 16:9 + resolusi native layar (jika beda), hanya yang <= resolusi layar.
    static void BuildResolutionOptions()
    {
        ResolutionOptions.Clear();
        var candidates = new (int w, int h)[] { (1280, 720), (1600, 900), (1920, 1080), (2560, 1440) };

        int nativeW = Screen.currentResolution.width;
        int nativeH = Screen.currentResolution.height;
        bool nativeMatchesCandidate = false;

        var list = new List<ResolutionOption>();
        foreach (var c in candidates)
        {
            if (c.w > nativeW || c.h > nativeH) continue;
            bool isNative = c.w == nativeW && c.h == nativeH;
            if (isNative) nativeMatchesCandidate = true;
            list.Add(new ResolutionOption { width = c.w, height = c.h, isNative = isNative });
        }
        if (!nativeMatchesCandidate && nativeW > 0 && nativeH > 0)
            list.Add(new ResolutionOption { width = nativeW, height = nativeH, isNative = true });

        list.Sort((a, b) => a.width != b.width ? a.width.CompareTo(b.width) : a.height.CompareTo(b.height));

        if (list.Count == 0)
            list.Add(new ResolutionOption { width = Mathf.Max(Screen.width, 1), height = Mathf.Max(Screen.height, 1), isNative = true });

        ResolutionOptions.AddRange(list);
    }

    static int GetDefaultResolutionIndex()
    {
        for (int i = 0; i < ResolutionOptions.Count; i++)
            if (ResolutionOptions[i].width == 1920 && ResolutionOptions[i].height == 1080)
                return i;
        return Mathf.Max(0, ResolutionOptions.Count - 1); // largest available (list sorted ascending)
    }

    public static void LoadAll()
    {
        MasterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, DefaultVolume);
        MusicVolume = PlayerPrefs.GetFloat(KeyMusicVolume, DefaultVolume);
        SFXVolume = PlayerPrefs.GetFloat(KeySFXVolume, DefaultVolume);
        MouseSensitivity = PlayerPrefs.GetFloat(KeyMouseSensitivity, DefaultMouseSensitivity);
        Brightness = PlayerPrefs.GetFloat(KeyBrightness, DefaultBrightness);
        Fullscreen = PlayerPrefs.GetInt(KeyFullscreen, DefaultFullscreen ? 1 : 0) == 1;
        ResolutionIndex = PlayerPrefs.GetInt(KeyResolutionIndex, GetDefaultResolutionIndex());
        ResolutionIndex = Mathf.Clamp(ResolutionIndex, 0, Mathf.Max(0, ResolutionOptions.Count - 1));
    }

    public static void ApplyAll()
    {
        ApplyMasterVolume(MasterVolume);
        ApplyMusicVolume(MusicVolume);
        ApplySFXVolume(SFXVolume);
        ApplyBrightness(Brightness);
        ApplyResolution(ResolutionIndex, Fullscreen);
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
        MouseSensitivity = Mathf.Clamp(v, 0.1f, 3f);
        PlayerPrefs.SetFloat(KeyMouseSensitivity, MouseSensitivity);
    }

    public static void SetBrightness(float v)
    {
        Brightness = Mathf.Clamp(v, 0.5f, 1.5f);
        PlayerPrefs.SetFloat(KeyBrightness, Brightness);
        ApplyBrightness(Brightness);
    }

    static void ApplyBrightness(float v) => OnBrightnessChanged?.Invoke(v);

    public static void SetFullscreen(bool on)
    {
        Fullscreen = on;
        PlayerPrefs.SetInt(KeyFullscreen, on ? 1 : 0);
        ApplyResolution(ResolutionIndex, Fullscreen);
    }

    public static void SetResolutionIndex(int index)
    {
        ResolutionIndex = Mathf.Clamp(index, 0, Mathf.Max(0, ResolutionOptions.Count - 1));
        PlayerPrefs.SetInt(KeyResolutionIndex, ResolutionIndex);
        ApplyResolution(ResolutionIndex, Fullscreen);
    }

    static void ApplyResolution(int index, bool fullscreen)
    {
        if (ResolutionOptions.Count == 0) return;
        index = Mathf.Clamp(index, 0, ResolutionOptions.Count - 1);
        var r = ResolutionOptions[index];
        Screen.SetResolution(r.width, r.height, fullscreen);
    }

    public static void ResetToDefault()
    {
        PlayerPrefs.DeleteKey(KeyMasterVolume);
        PlayerPrefs.DeleteKey(KeyMusicVolume);
        PlayerPrefs.DeleteKey(KeySFXVolume);
        PlayerPrefs.DeleteKey(KeyMouseSensitivity);
        PlayerPrefs.DeleteKey(KeyBrightness);
        PlayerPrefs.DeleteKey(KeyFullscreen);
        PlayerPrefs.DeleteKey(KeyResolutionIndex);
        LoadAll();
        ApplyAll();
    }
}