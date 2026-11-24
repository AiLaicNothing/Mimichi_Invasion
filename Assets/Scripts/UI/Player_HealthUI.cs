using TMPro;
using UnityEngine;

/// <summary>
/// Shows the player's health as text, e.g. "HP 75 / 100".
/// If no Player_Health is assigned, it finds the object tagged "Player".
/// </summary>
public class Player_HealthUI : MonoBehaviour
{
    [SerializeField] private Player_Health playerHealth;
    [SerializeField] private TextMeshProUGUI healthText;

    [Tooltip("{0} = current health, {1} = max health")]
    [SerializeField] private string format = "HP {0} / {1}";

    [Header("Low Health")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowColor = new Color(1f, 0.3f, 0.3f);
    [Range(0f, 1f)]
    [SerializeField] private float lowThreshold = 0.3f;

    private void Start()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerHealth = player.GetComponent<Player_Health>();
        }

        if (playerHealth == null)
        {
            Debug.LogWarning("Player_HealthUI: no Player_Health found.", this);
            return;
        }

        playerHealth.OnHealthChanged += UpdateHealth;
        UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHealth;
    }

    private void UpdateHealth(float current, float max)
    {
        healthText.text = string.Format(format, Mathf.CeilToInt(current), Mathf.CeilToInt(max));
        healthText.color = max > 0 && current / max <= lowThreshold ? lowColor : normalColor;
    }
}
