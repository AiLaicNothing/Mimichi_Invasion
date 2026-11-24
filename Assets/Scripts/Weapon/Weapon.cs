using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// A weapon that exists in the world. While unequipped it lies on the ground and can be
/// picked up (needs a trigger Collider for detection). While equipped it is parented to the
/// player's weapon socket and fires when Player_Weapons tells it to.
///
/// Each weapon prefab owns its own fire point and ammo; shared stats come from Weapon_Data.
/// </summary>
public abstract class Weapon : MonoBehaviour, IInteractable
{
    [SerializeField] protected Weapon_Data data;
    [Tooltip("Muzzle of this weapon model. Bullets and trails start here.")]
    [SerializeField] protected Transform firePoint;

    [Header("World")]
    [Tooltip("Height above the ground when the weapon is dropped.")]
    [SerializeField] private float dropHeight = 0.3f;
    [SerializeField] private float idleSpinSpeed = 90f;

    public Weapon_Data Data => data;
    public int CurrentAmmo { get; private set; }
    public int ReserveAmmo { get; private set; }
    public bool IsReloading { get; private set; }
    public bool IsEquipped => owner != null;

    /// <summary>(currentAmmo, reserveAmmo) — hook UI here.</summary>
    public event Action<int, int> OnAmmoChanged;
    /// <summary>true = reload started, false = finished or cancelled.</summary>
    public event Action<bool> OnReloadStateChanged;

    protected GameObject owner;
    private float shootTimer;
    private Coroutine reloadRoutine;
    private AudioSource audioSource;
    private Collider[] colliders;
    private Rigidbody rb;

    protected Transform FirePoint => firePoint != null ? firePoint : transform;
    public Vector3 MuzzlePosition => FirePoint.position;

    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        colliders = GetComponentsInChildren<Collider>(true);
        rb = GetComponent<Rigidbody>();

        CurrentAmmo = data.magSize;
        ReserveAmmo = data.totalMag * data.magSize;
    }

    protected virtual void Update()
    {
        if (shootTimer > 0) shootTimer -= Time.deltaTime;

        if (!IsEquipped && idleSpinSpeed != 0)
            transform.Rotate(0, idleSpinSpeed * Time.deltaTime, 0, Space.World);
    }

    // ---------- Shooting ----------

    /// <summary>
    /// Called every frame the player wants to fire. Handles fire rate, ammo and auto-reload.
    /// </summary>
    /// <param name="direction">Flat aim direction.</param>
    /// <param name="hitMask">Layers bullets can hit.</param>
    public bool TryShoot(Vector3 direction, LayerMask hitMask)
    {
        if (IsReloading || shootTimer > 0) return false;

        if (CurrentAmmo <= 0)
        {
            if (data.emptySfx != null) audioSource.PlayOneShot(data.emptySfx);
            shootTimer = data.timeBtwShoot;
            StartReload();
            return false;
        }

        shootTimer = data.timeBtwShoot;
        CurrentAmmo--;
        OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);

        if (data.shootSfx != null) audioSource.PlayOneShot(data.shootSfx);

        Fire(direction, hitMask);

        if (CurrentAmmo <= 0) StartReload();
        return true;
    }

    /// <summary>
    /// One trigger pull. Default: a single bullet with random spread.
    /// Override for shotguns, burst fire, projectiles, etc.
    /// </summary>
    protected virtual void Fire(Vector3 direction, LayerMask hitMask)
    {
        FireBullet(ApplySpread(direction, data.spreadAngle), hitMask);
    }

    protected Vector3 ApplySpread(Vector3 direction, float angle)
    {
        if (angle <= 0) return direction;
        return Quaternion.Euler(0, UnityEngine.Random.Range(-angle, angle), 0) * direction;
    }

    /// <summary>Hitscan bullet + trail. Damages any IDamageable that isn't the owner.</summary>
    protected void FireBullet(Vector3 direction, LayerMask hitMask) =>
        FireBullet(direction, hitMask, data.range, data.damage);

    /// <param name="range">Max distance of this bullet.</param>
    /// <param name="damage">Damage at point blank.</param>
    /// <param name="falloff">0..1, fraction of damage lost at max range (linear).</param>
    protected void FireBullet(Vector3 direction, LayerMask hitMask, float range, float damage, float falloff = 0f)
    {
        Vector3 origin = FirePoint.position;
        Vector3 hitPoint = origin + direction * range;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            hitPoint = hit.point;

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            bool isOwner = owner != null && hit.collider.transform.IsChildOf(owner.transform);

            if (damageable != null && !isOwner)
            {
                float finalDamage = damage * (1f - falloff * (hit.distance / range));
                damageable.TakeDamage(finalDamage, hit.point, direction);
            }
        }

        Debug.DrawLine(origin, hitPoint, Color.yellow, 0.1f);

        if (data.bulletTrailPrefab != null)
        {
            TrailRenderer trail = Instantiate(data.bulletTrailPrefab, origin, Quaternion.identity);
            StartCoroutine(MoveTrail(trail, hitPoint));
        }
    }

    private IEnumerator MoveTrail(TrailRenderer trail, Vector3 hitPoint)
    {
        float t = 0f;
        Vector3 start = trail.transform.position;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, data.trailTravelTime);
            trail.transform.position = Vector3.Lerp(start, hitPoint, Mathf.Clamp01(t));
            yield return null;
        }

        Destroy(trail.gameObject, data.trailDuration);
    }

    // ---------- Reload ----------

    public void StartReload()
    {
        if (IsReloading || CurrentAmmo >= data.magSize) return;
        if (!data.infiniteAmmo && ReserveAmmo <= 0) return;

        reloadRoutine = StartCoroutine(Reload());
    }

    private IEnumerator Reload()
    {
        IsReloading = true;
        OnReloadStateChanged?.Invoke(true);
        if (data.reloadSfx != null) audioSource.PlayOneShot(data.reloadSfx);

        yield return new WaitForSeconds(data.reloadTime);

        int needed = data.magSize - CurrentAmmo;

        if (data.infiniteAmmo)
        {
            CurrentAmmo = data.magSize;
        }
        else
        {
            int loaded = Mathf.Min(needed, ReserveAmmo);
            CurrentAmmo += loaded;
            ReserveAmmo -= loaded;
        }

        IsReloading = false;
        reloadRoutine = null;
        OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);
        OnReloadStateChanged?.Invoke(false);
    }

    private void CancelReload()
    {
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = null;

        if (IsReloading)
        {
            IsReloading = false;
            OnReloadStateChanged?.Invoke(false);
        }
    }

    public void AddAmmo(int amount)
    {
        ReserveAmmo += amount;
        OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);
    }

    // ---------- Interaction (pick up with E) ----------

    public string GetPrompt(GameObject interactor)
    {
        string weaponName = data != null && !string.IsNullOrEmpty(data._name) ? data._name : name;
        return $"Pick up {weaponName}";
    }

    public bool CanInteract(GameObject interactor) => !IsEquipped;

    public void Interact(GameObject interactor)
    {
        Player_Weapons weapons = interactor.GetComponent<Player_Weapons>();
        if (weapons != null) weapons.Equip(this);
    }

    // ---------- Equip / Drop ----------

    public virtual void Equip(Transform socket, GameObject newOwner)
    {
        owner = newOwner;

        SetPhysicsActive(false);

        transform.SetParent(socket, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);
    }

    /// <param name="groundPosition">Point on the floor where the weapon should rest.</param>
    public virtual void Drop(Vector3 groundPosition)
    {
        CancelReload();
        owner = null;

        transform.SetParent(null, true);
        transform.SetPositionAndRotation(groundPosition + Vector3.up * dropHeight, Quaternion.identity);

        SetPhysicsActive(true);
    }

    private void SetPhysicsActive(bool active)
    {
        foreach (Collider col in colliders) col.enabled = active;

        if (rb != null)
        {
            rb.isKinematic = true; // weapons don't roll around; they float in place to be picked up
            rb.useGravity = false;
        }
    }
}
