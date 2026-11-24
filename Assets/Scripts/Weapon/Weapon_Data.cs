using UnityEngine;

/// <summary>
/// Shared stats for a weapon type (e.g. "Basic Pistol"). Every pistol in the world points to the same asset.
/// Things that depend on the model (fire point position) live on the Weapon component instead.
/// </summary>
[CreateAssetMenu(menuName = "Weapon/Data")]
public class Weapon_Data : ScriptableObject
{
    [Header("Information")]
    public string _name;
    public WeaponType _weaponType;
    [TextArea(5, 10)]
    public string _description;
    public GameObject prefab;
    [Tooltip("Shown in the weapon panel. Optional.")]
    public Sprite icon;

    [Header("Stat Values")]
    public float damage = 20f;
    [Tooltip("Seconds between shots.")]
    public float timeBtwShoot = 0.2f;
    public float range = 30f;
    [Tooltip("Hold to keep firing (rifles). If false, one shot per click (pistol, shotgun).")]
    public bool isAutomatic;
    [Tooltip("Random deviation in degrees for each bullet. 0 = perfect accuracy.")]
    public float spreadAngle;

    [Header("Ammo")]
    public float reloadTime = 1f;
    public int magSize = 12;
    [Tooltip("Extra magazines carried when the weapon is spawned.")]
    public int totalMag = 4;
    [Tooltip("Ignore reserve ammo (starting pistol, testing).")]
    public bool infiniteAmmo;

    [Header("Bullet Trail")]
    public TrailRenderer bulletTrailPrefab;
    public float trailTravelTime = 0.08f;
    public float trailDuration = 0.1f;

    [Header("Audio")]
    public AudioClip shootSfx;
    public AudioClip reloadSfx;
    public AudioClip emptySfx;

    public enum WeaponType
    {
        Pistol,
        Rifle,
        Shotgun,
        Sniper,
        Special
    }
}
