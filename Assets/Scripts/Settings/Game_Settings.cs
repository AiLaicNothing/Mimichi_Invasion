using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Saves / loads / applies player settings with PlayerPrefs.
/// Static so any menu (main menu, pause menu) can use it.
/// Resolution and fullscreen are also remembered by Unity itself; volume is not,
/// so call ApplySaved() once at startup (Main_Menu does it).
/// </summary>
public static class Game_Settings
{
    private const string KeyFullscreen = "settings_fullscreen";
    private const string KeyResWidth = "settings_res_width";
    private const string KeyResHeight = "settings_res_height";
    private const string KeyVolume = "settings_master_volume";

    /// <summary>Exposed parameter name in the AudioMixer (if one is used).</summary>
    public const string MixerVolumeParam = "MasterVolume";

    public static bool Fullscreen => PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
    public static float Volume => PlayerPrefs.GetFloat(KeyVolume, 1f);
    public static int ResolutionWidth => PlayerPrefs.GetInt(KeyResWidth, Screen.currentResolution.width);
    public static int ResolutionHeight => PlayerPrefs.GetInt(KeyResHeight, Screen.currentResolution.height);

    /// <summary>Applies everything that was saved. Mixer is optional.</summary>
    public static void ApplySaved(AudioMixer mixer = null)
    {
        SetVolume(Volume, mixer);
        Screen.SetResolution(ResolutionWidth, ResolutionHeight, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    public static void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        PlayerPrefs.SetInt(KeyFullscreen, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetResolution(int width, int height)
    {
        Screen.SetResolution(width, height, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        PlayerPrefs.SetInt(KeyResWidth, width);
        PlayerPrefs.SetInt(KeyResHeight, height);
        PlayerPrefs.Save();
    }

    /// <param name="volume">0..1</param>
    /// <param name="mixer">If set, drives the mixer's exposed "MasterVolume" (dB). Otherwise AudioListener.volume.</param>
    public static void SetVolume(float volume, AudioMixer mixer = null)
    {
        volume = Mathf.Clamp01(volume);

        if (mixer != null)
            mixer.SetFloat(MixerVolumeParam, Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f); // 0..1 -> -80..0 dB
        else
            AudioListener.volume = volume;

        PlayerPrefs.SetFloat(KeyVolume, volume);
        PlayerPrefs.Save();
    }
}
