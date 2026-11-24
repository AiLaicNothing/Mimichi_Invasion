using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Esc toggles the pause panel. Pausing freezes time (Time.timeScale = 0), pauses audio
/// and frees the cursor. Player scripts check Pause_Menu.IsPaused to ignore input.
/// Put this on an always-active object (e.g. the Canvas), NOT on the panel it hides.
/// </summary>
public class Pause_Menu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    [Tooltip("Scene loaded by the Quit button. Must be in Build Profiles > Scene List.")]
    [SerializeField] private string menuSceneName = "MainMenu";

    private Player_InputsReciver inputs;
    private Player_Health health;

    private void Awake()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (quitButton != null) quitButton.onClick.AddListener(QuitToMenu);

        SetPaused(false);
    }

    private void Update()
    {
        if (inputs == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                inputs = player.GetComponent<Player_InputsReciver>();
                health = player.GetComponent<Player_Health>();
            }
            if (inputs == null) return;
        }

        // No pausing once dead: the death screen takes over.
        if (Death_UI.IsShowing || (health != null && health.IsDead)) return;

        if (inputs.hasPaused) SetPaused(!IsPaused);
    }

    public void Pause() => SetPaused(true);
    public void Resume() => SetPaused(false);

    public void QuitToMenu()
    {
        SetPaused(false); // never carry timeScale = 0 into the next scene
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(menuSceneName);
    }

    private void SetPaused(bool paused)
    {
        IsPaused = paused;

        if (pausePanel != null) pausePanel.SetActive(paused);

        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;

        Cursor.visible = true;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Confined;
    }

    private void OnDestroy()
    {
        // Scene unloaded while paused: restore global state.
        if (IsPaused)
        {
            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
