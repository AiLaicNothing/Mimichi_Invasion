using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth;
    private float currentHealth;

    [Header("Move - AI")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private LayerMask whatIsObstacle;
    [SerializeField] private Transform groundCheck;

    private Rigidbody rb;
    private Animator anim;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0) OnDead();
    }

    private void OnDead()
    {

    }
}
