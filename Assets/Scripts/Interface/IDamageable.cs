using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage);

    /// <summary>
    /// Damage with hit info (used for knockback / death launch).
    /// Default just forwards to TakeDamage(damage), so implementers that don't care can ignore it.
    /// </summary>
    /// <param name="hitPoint">World point that was hit.</param>
    /// <param name="hitDirection">Direction the bullet was travelling (normalized).</param>
    void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection) => TakeDamage(damage);
}
