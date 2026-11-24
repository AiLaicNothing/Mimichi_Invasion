using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu navigation: Play, Options, Leaderboard, Quit.
/// Only one panel is visible at a time; Back buttons return to the main panel.
/// Put this on the Canvas (always active).
/// </summary>
public class Main_Menu : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Must be in Build Profiles > Scene List.")]
    [SerializeField] private string gameSceneName = "Game_World";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject leaderboardPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button quitButton;

    [Header("Back Buttons")]
    [SerializeField] private Button optionsBackButton;
    [SerializeField] private Button leaderboardBackButton;

    [Header("Audio")]
    [Tooltip("Same mixer as Options_Menu (optional).")]
    [SerializeField] private AudioMixer audioMixer;

    private void Awake()
    {
        // Coming back from the game: make sure nothing stays paused/hidden.
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Bind(playButton, Play);
        Bind(optionsButton, () => ShowPanel(optionsPanel));
        Bind(leaderboardButton, () => ShowPanel(leaderboardPanel));
        Bind(quitButton, Quit);

        Bind(optionsBackButton, () => ShowPanel(mainPanel));
        Bind(leaderboardBackButton, () => ShowPanel(mainPanel));
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.AddListener(action);
    }

    private void Start()
    {
        Game_Settings.ApplySaved(audioMixer);
        ShowPanel(mainPanel);
    }

    private void ShowPanel(GameObject panel)
    {
        if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
        if (optionsPanel != null) optionsPanel.SetActive(panel == optionsPanel);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(panel == leaderboardPanel);
    }

    public void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
