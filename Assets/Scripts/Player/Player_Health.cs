using System;
using UnityEngine;

public class Player_Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private float invulnerableUntil;

    public bool IsDead => currentHealth <= 0;
    /// <summary>While true, all damage is ignored (dash i-frames, respawn, etc.).</summary>
    public bool IsInvulnerable => Time.time < invulnerableUntil;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    /// <summary>(current, max) — hook a health bar here.</summary>
    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Ignore damage for <paramref name="duration"/> seconds.
    /// Overlapping calls keep the longest one (never shortens an existing invulnerability).
    /// </summary>
    public void SetInvulnerable(float duration)
    {
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || IsInvulnerable) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0) OnDead();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void OnDead()
    {
        Debug.Log("Player died");
        OnDied?.Invoke();
    }
}
