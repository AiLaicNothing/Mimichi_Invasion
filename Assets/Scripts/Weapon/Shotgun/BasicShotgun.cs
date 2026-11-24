using UnityEngine;

/// <summary>
/// Shotgun blast: pellets scatter randomly inside a cone, clustered toward the center,
/// each with its own range and damage that falls off with distance.
/// Every shot produces a different pattern. Weapon_Data.damage is damage PER PELLET.
/// </summary>
public class BasicShotgun : Weapon
{
    [Header("Shotgun")]
    [SerializeField] private int pelletCount = 8;
    [Tooltip("Total width of the cone in degrees.")]
    [SerializeField] private float bulletSpread = 35f;
    [Tooltip("1 = pellets evenly distributed in the cone. Higher = more pellets near the center.")]
    [Range(1, 4)]
    [SerializeField] private int centerBias = 2;

    [Header("Pellet Range")]
    [Tooltip("Each pellet travels between this fraction of Weapon_Data.range and the full range.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float minRangePercent = 0.6f;
    [Tooltip("Fraction of damage lost at max range. 0.6 = 40% damage at the end of the range.")]
    [Range(0f, 1f)]
    [SerializeField] private float damageFalloff = 0.6f;

    protected override void Fire(Vector3 direction, LayerMask hitMask)
    {
        float halfSpread = bulletSpread * 0.5f;

        for (int i = 0; i < pelletCount; i++)
        {
            float angle = RandomCentered() * halfSpread;
            Vector3 pelletDir = Quaternion.Euler(0, angle, 0) * direction;

            float range = data.range * Random.Range(minRangePercent, 1f);

            FireBullet(pelletDir, hitMask, range, data.damage, damageFalloff);
        }
    }

    /// <summary>
    /// Random value in [-1, 1]. Averaging several randoms makes values near 0 more likely
    /// (bell-like curve), so the blast is dense in the middle and sparse at the edges.
    /// </summary>
    private float RandomCentered()
    {
        float sum = 0f;
        for (int i = 0; i < centerBias; i++) sum += Random.Range(-1f, 1f);
        return sum / centerBias;
    }
}
