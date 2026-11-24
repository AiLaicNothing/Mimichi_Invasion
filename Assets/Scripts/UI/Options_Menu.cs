using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Options panel: fullscreen toggle, resolution dropdown, volume slider.
/// Changes apply and save immediately. Reusable (main menu or pause menu).
/// </summary>
public class Options_Menu : MonoBehaviour
{
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Slider volumeSlider;
    [Tooltip("Optional: shows the volume as a percentage.")]
    [SerializeField] private TextMeshProUGUI volumeValueText;

    [Tooltip("Optional. Expose the Master group's volume as \"MasterVolume\". If empty, AudioListener.volume is used.")]
    [SerializeField] private AudioMixer audioMixer;

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();

    private void Awake()
    {
        BuildResolutionList();
    }

    // Refresh every time the panel opens so it always shows the current values.
    private void OnEnable()
    {
        LoadIntoUI();

        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnDisable()
    {
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    private void BuildResolutionList()
    {
        resolutions.Clear();

        // Screen.resolutions repeats each size once per refresh rate: keep unique sizes only.
        foreach (Resolution r in Screen.resolutions)
        {
            Vector2Int size = new Vector2Int(r.width, r.height);
            if (!resolutions.Contains(size)) resolutions.Add(size);
        }

        if (resolutions.Count == 0) // editor / unusual platforms
            resolutions.Add(new Vector2Int(Screen.width, Screen.height));

        if (resolutionDropdown == null) return;

        List<string> options = new List<string>();
        foreach (Vector2Int size in resolutions) options.Add($"{size.x} x {size.y}");

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    private void LoadIntoUI()
    {
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Game_Settings.Fullscreen);

        if (resolutionDropdown != null)
        {
            int index = resolutions.IndexOf(new Vector2Int(Game_Settings.ResolutionWidth, Game_Settings.ResolutionHeight));
            if (index < 0) index = resolutions.Count - 1; // largest
            resolutionDropdown.SetValueWithoutNotify(index);
            resolutionDropdown.RefreshShownValue();
        }

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(Game_Settings.Volume);
        }

        UpdateVolumeText(Game_Settings.Volume);
    }

    private void OnFullscreenChanged(bool fullscreen) => Game_Settings.SetFullscreen(fullscreen);

    private void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= resolutions.Count) return;
        Game_Settings.SetResolution(resolutions[index].x, resolutions[index].y);
    }

    private void OnVolumeChanged(float value)
    {
        Game_Settings.SetVolume(value, audioMixer);
        UpdateVolumeText(value);
    }

    private void UpdateVolumeText(float value)
    {
        if (volumeValueText != null) volumeValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
}
