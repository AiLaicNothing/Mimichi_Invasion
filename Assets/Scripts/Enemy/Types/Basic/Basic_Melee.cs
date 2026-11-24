using System.Collections;
using UnityEngine;

/// <summary>
/// Zombie: walks straight at the player, stops in range, winds up and hits.
/// The hit only lands if the player is still in range when the wind-up ends,
/// so dashing away dodges it.
/// </summary>
public class Basic_Melee : Enemy
{
    [Header("Melee")]
    [Tooltip("Delay between starting the attack and the hit landing (dodge window).")]
    [SerializeField] private float attackWindUp = 0.35f;
    [Tooltip("Time standing still after the hit.")]
    [SerializeField] private float attackRecovery = 0.25f;
    [Tooltip("Extra reach when the hit lands, so it doesn't miss by a hair.")]
    [SerializeField] private float hitRangeBonus = 0.4f;
    [Tooltip("If true, wait for an Animation Event calling DoDamage() instead of the wind-up timer.")]
    [SerializeField] private bool useAnimationEvent;

    protected override void Attack()
    {
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // With useAnimationEvent, the animation calls DoDamage() itself during the wind-up.
        yield return new WaitForSeconds(attackWindUp);
        if (!useAnimationEvent) DoDamage();

        yield return new WaitForSeconds(attackRecovery);
        isAttacking = false;
    }

    /// <summary>Applies the hit. Public so an Animation Event can call it.</summary>
    public void DoDamage()
    {
        if (IsDead || target == null) return;
        if (DistanceToTarget > attackRange + hitRangeBonus) return;

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        damageable?.TakeDamage(damage, target.position, DirectionToTarget());
    }
}
