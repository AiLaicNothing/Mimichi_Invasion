using TMPro;
using UnityEngine;

/// <summary>
/// Shows "[E] Pick up Shotgun" / "[E] Random weapon (500)" when something usable is in range.
/// Put this on an always-active object; it toggles promptRoot.
/// </summary>
public class Interaction_PromptUI : MonoBehaviour
{
    [Tooltip("Object shown/hidden (e.g. a background Image with the text inside).")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TextMeshProUGUI promptText;
    [Tooltip("{0} = the interactable's prompt")]
    [SerializeField] private string format = "[E] {0}";

    private Player_Interaction interaction;
    private GameObject player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) interaction = player.GetComponent<Player_Interaction>();

        if (interaction == null) Debug.LogWarning("Interaction_PromptUI: no Player_Interaction on the Player.", this);
        if (promptRoot != null) promptRoot.SetActive(false);
    }

    // Polled (not event-driven) so prices / names stay correct while standing in front of something.
    private void Update()
    {
        IInteractable current = interaction != null ? interaction.Current : null;
        string prompt = current != null ? current.GetPrompt(player) : null;

        bool show = !string.IsNullOrEmpty(prompt);
        if (promptRoot != null && promptRoot.activeSelf != show) promptRoot.SetActive(show);

        if (show) promptText.text = string.Format(format, prompt);
    }
}
