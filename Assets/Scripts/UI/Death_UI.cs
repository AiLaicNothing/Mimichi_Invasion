using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Game over screen. When the player dies: stops the waves, waits a moment, then shows
/// waves survived, the equipped weapon and enemies killed, with Retry and Main Menu buttons.
/// Put this on an always-active object (e.g. the Canvas), NOT on the panel it shows.
/// </summary>
public class Death_UI : MonoBehaviour
{
    [SerializeField] private GameObject deathPanel;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI wavesText;
    [SerializeField] private TextMeshProUGUI weaponText;
    [SerializeField] private TextMeshProUGUI killsText;
    [Tooltip("Optional: shows the weapon's icon. Hidden if the weapon has none.")]
    [SerializeField] private Image weaponIcon;

    [Header("Formats")]
    [Tooltip("{0} = waves survived (fully cleared), {1} = wave you died in")]
    [SerializeField] private string wavesFormat = "Waves survived: {0}";
    [SerializeField] private string weaponFormat = "Weapon: {0}";
    [SerializeField] private string killsFormat = "Enemies defeated: {0}";
    [SerializeField] private string noWeaponText = "None";

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Timing")]
    [Tooltip("Seconds between dying and the panel appearing (let the death play out).")]
    [SerializeField] private float showDelay = 1.5f;
    [SerializeField] private float fadeInTime = 0.5f;

    public static bool IsShowing { get; private set; }

    private Player_Health playerHealth;
    private Player_Weapons playerWeapons;

    private void Awake()
    {
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);

        IsShowing = false;
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<Player_Health>();
            playerWeapons = player.GetComponent<Player_Weapons>();
        }

        if (playerHealth == null)
        {
            Debug.LogWarning("Death_UI: no Player_Health found on the Player.", this);
            return;
        }

        playerHealth.OnDied += HandlePlayerDied;
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDied -= HandlePlayerDied;
        IsShowing = false;
    }

    private void HandlePlayerDied()
    {
        if (Wave_Manager.Instance != null) Wave_Manager.Instance.StopWaves();
        if (Game_Manager.Instance != null) Game_Manager.Instance.OnDefeat();

        StartCoroutine(ShowAfterDelay());
    }

    private IEnumerator ShowAfterDelay()
    {
        // Snapshot the weapon now, before anything can change it.
        Weapon weapon = playerWeapons != null ? playerWeapons.CurrentWeapon : null;

        yield return new WaitForSeconds(showDelay);

        FillStats(weapon);

        IsShowing = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        deathPanel.SetActive(true);

        // Fade in with a CanvasGroup if the panel has one.
        CanvasGroup group = deathPanel.GetComponent<CanvasGroup>();
        if (group != null)
        {
            float t = 0f;
            group.alpha = 0f;
            while (t < fadeInTime)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = t / fadeInTime;
                yield return null;
            }
            group.alpha = 1f;
        }
    }

    private void FillStats(Weapon weapon)
    {
        Wave_Manager waves = Wave_Manager.Instance;
        int survived = waves != null ? waves.WavesCompleted : 0;
        int reached = waves != null ? waves.CurrentWave : 0;
        int kills = waves != null ? waves.EnemiesKilled : 0;

        if (wavesText != null) wavesText.text = string.Format(wavesFormat, survived, reached);
        if (killsText != null) killsText.text = string.Format(killsFormat, kills);

        string weaponName = noWeaponText;
        if (weapon != null && weapon.Data != null)
            weaponName = string.IsNullOrEmpty(weapon.Data._name) ? weapon.Data.name : weapon.Data._name;

        if (weaponText != null) weaponText.text = string.Format(weaponFormat, weaponName);

        if (weaponIcon != null)
        {
            Sprite icon = weapon != null && weapon.Data != null ? weapon.Data.icon : null;
            weaponIcon.gameObject.SetActive(icon != null);
            if (icon != null) weaponIcon.sprite = icon;
        }
    }

    public void Retry()
    {
        ResetGlobalState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        ResetGlobalState();
        SceneManager.LoadScene(menuSceneName);
    }

    private void ResetGlobalState()
    {
        IsShowing = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
