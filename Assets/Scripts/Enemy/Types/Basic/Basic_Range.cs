using System.Collections;
using UnityEngine;

/// <summary>
/// Shooter: approaches until it has line of sight inside attackRange, then shoots projectiles.
/// Backs off if the player gets closer than retreatDistance.
/// </summary>
public class Basic_Range : Enemy
{
    [Header("Ranged")]
    [SerializeField] private Transform firepoint;
    [SerializeField] private Enemy_Projectile bulletPrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [Tooltip("Pause before firing so the player sees the shot coming.")]
    [SerializeField] private float aimTime = 0.3f;
    [Tooltip("Random deviation in degrees.")]
    [SerializeField] private float inaccuracy = 3f;

    [Header("Spacing")]
    [Tooltip("If the player is closer than this, walk backwards. 0 = never retreat.")]
    [SerializeField] private float retreatDistance = 4f;

    // Sensible defaults when the component is first added.
    private void Reset()
    {
        attackRange = 10f;
        timeBtwAttack = 1.5f;
    }

    protected override bool CanAttack()
    {
        // Too close: prefer to back off instead of standing still.
        if (retreatDistance > 0 && DistanceToTarget < retreatDistance) return false;
        return base.CanAttack();
    }

    protected override Vector3 GetDesiredMoveDirection()
    {
        // Back off along the NavMesh (won't walk into walls or off ledges).
        if (retreatDistance > 0 && DistanceToTarget < retreatDistance)
            return GetPathDirection(transform.position - DirectionToTarget() * 3f);

        return base.GetDesiredMoveDirection();
    }

    protected override void Attack()
    {
        StartCoroutine(ShootRoutine());
    }

    private IEnumerator ShootRoutine()
    {
        isAttacking = true;
        yield return new WaitForSeconds(aimTime);

        if (!IsDead && target != null) Shoot();

        isAttacking = false;
    }

    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("Basic_Range has no bulletPrefab assigned.", this);
            return;
        }

        Vector3 origin = firepoint != null ? firepoint.position : transform.position + Vector3.up * 0.5f;

        Vector3 dir = target.position - origin;
        dir.y = 0;
        dir = Quaternion.Euler(0, Random.Range(-inaccuracy, inaccuracy), 0) * dir.normalized;

        Enemy_Projectile bullet = Instantiate(bulletPrefab, origin, Quaternion.LookRotation(dir));
        bullet.Launch(dir, projectileSpeed, damage, gameObject);
    }
}
