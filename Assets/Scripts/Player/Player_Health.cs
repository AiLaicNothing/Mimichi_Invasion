using UnityEngine;

public class Player_Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth;
    private float currentHealth;

    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0) OnDead();
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
    }

    private void OnDead()
    {
        Debug.Log("Player died");
    }
}
