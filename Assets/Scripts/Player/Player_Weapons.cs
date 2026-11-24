using System;
using UnityEngine;

/// <summary>
/// Holds the single weapon the player is carrying.
/// - Picking up (E) is handled by Player_Interaction -> Weapon.Interact -> Equip. The current one is dropped at the player's feet.
/// - Left click: shoot toward the cursor (hold for automatic weapons).
/// - R: reload.
/// All weapon-specific data (fire point, trail, damage, ammo...) lives on the Weapon itself.
/// </summary>
public class Player_Weapons : MonoBehaviour
{
    [Header("Equip")]
    [Tooltip("Child transform (e.g. the hand) where the equipped weapon is attached.")]
    [SerializeField] private Transform weaponSocket;
    [Tooltip("Optional weapon prefab given to the player at start.")]
    [SerializeField] private Weapon startingWeapon;

    [Header("Drop")]
    [Tooltip("Used to place dropped weapons on the floor.")]
    [SerializeField] private LayerMask groundMask = 1 << 8;

    [Header("Shooting")]
    [Tooltip("Layers bullets can hit (enemies + walls). Exclude the Player layer.")]
    [SerializeField] private LayerMask hitMask = ~0;

    public Weapon CurrentWeapon { get; private set; }

    /// <summary>Fired when the equipped weapon changes (null = unarmed). Hook UI here.</summary>
    public event Action<Weapon> OnWeaponChanged;

    private Player_InputsReciver inputsReciver;
    private Player_Controller controller;
    private Player_Health health;

    private void Awake()
    {
        inputsReciver = GetComponent<Player_InputsReciver>();
        controller = GetComponent<Player_Controller>();
        health = GetComponent<Player_Health>();

        if (weaponSocket == null) weaponSocket = transform;
    }

    private void Start()
    {
        if (startingWeapon != null)
            Equip(Instantiate(startingWeapon));
    }

    private void Update()
    {
        if (health != null && health.IsDead) return;
        if (Pause_Menu.IsPaused) return;

        if (CurrentWeapon == null) return;

        if (inputsReciver.hasReloaded) CurrentWeapon.StartReload();

        if (controller != null && controller.IsDashing) return;

        bool wantsToShoot = CurrentWeapon.Data.isAutomatic ? inputsReciver.hasShoot : inputsReciver.hasShootPressed;
        if (wantsToShoot) CurrentWeapon.TryShoot(GetShootDirection(), hitMask);
    }

    /// <summary>
    /// Aim from the muzzle (not the player center) to the cursor, so bullets actually
    /// pass under the cursor even though the gun is offset to the side.
    /// </summary>
    private Vector3 GetShootDirection()
    {
        Vector3 fallback = controller != null ? controller.AimDirection : transform.forward;
        if (controller == null) return fallback;

        Vector3 dir = controller.AimPoint - CurrentWeapon.MuzzlePosition;
        dir.y = 0;

        // Cursor too close to the muzzle: direction is unstable, use the body's aim.
        return dir.sqrMagnitude > 1f ? dir.normalized : fallback;
    }

    // ---------- Equip ----------

    public void Equip(Weapon weapon)
    {
        if (weapon == null || weapon == CurrentWeapon) return;

        if (CurrentWeapon != null) CurrentWeapon.Drop(GetGroundPosition());

        CurrentWeapon = weapon;
        CurrentWeapon.Equip(weaponSocket, gameObject);

        OnWeaponChanged?.Invoke(CurrentWeapon);
    }

    public void DropCurrent()
    {
        if (CurrentWeapon == null) return;

        CurrentWeapon.Drop(GetGroundPosition());
        CurrentWeapon = null;

        OnWeaponChanged?.Invoke(null);
    }

    private Vector3 GetGroundPosition() => GetGroundPosition(transform.position);

    private Vector3 GetGroundPosition(Vector3 from)
    {
        if (Physics.Raycast(from + Vector3.up, Vector3.down, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point;

        return new Vector3(from.x, transform.position.y - 1f, from.z);
    }
}
