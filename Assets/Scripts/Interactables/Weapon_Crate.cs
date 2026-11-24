using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Pay <see cref="cost"/> with E to get a random weapon. The weapon appears at spawnPoint
/// lying in the world; the player picks it up with E like any other weapon.
/// The crate stays locked until that weapon is picked up (no stacking weapons on it).
/// Needs a Collider so Player_Interaction can find it.
/// </summary>
public class Weapon_Crate : MonoBehaviour, IInteractable
{
    [Serializable]
    public class WeaponEntry
    {
        public Weapon prefab;
        [Tooltip("Relative chance. 2 = twice as common as 1.")]
        public float weight = 1f;
    }

    [SerializeField] private int cost = 500;
    [SerializeField] private WeaponEntry[] weapons;
    [Tooltip("Where the weapon appears. Defaults to 1 unit above the crate.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Don't roll the same weapon type the player is holding (if there's another option).")]
    [SerializeField] private bool avoidCurrentWeapon = true;
    [Tooltip("Delay between paying and the weapon appearing (time for an opening animation/sound).")]
    [SerializeField] private float openDelay = 0.5f;

    [Header("Feedback (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buySfx;
    [SerializeField] private AudioClip deniedSfx;

    /// <summary>Fired when the crate is bought (animations, VFX).</summary>
    public event Action OnOpened;

    private Weapon waitingWeapon;
    private bool isOpening;

    private bool IsBusy => isOpening || (waitingWeapon != null && !waitingWeapon.IsEquipped);

    // ---------- IInteractable ----------

    public string GetPrompt(GameObject interactor) => $"Random weapon ({cost})";

    public bool CanInteract(GameObject interactor) => !IsBusy;

    public void Interact(GameObject interactor)
    {
        if (IsBusy) return;

        Economy_Manager economy = Economy_Manager.Instance;
        if (economy == null)
        {
            Debug.LogWarning("Weapon_Crate: no Economy_Manager in the scene.", this);
            return;
        }

        if (!economy.TrySpend(cost))
        {
            Play(deniedSfx);
            return;
        }

        Play(buySfx);
        StartCoroutine(Open(interactor));
    }

    // ---------- Logic ----------

    private IEnumerator Open(GameObject interactor)
    {
        isOpening = true;
        OnOpened?.Invoke();

        yield return new WaitForSeconds(openDelay);

        Weapon prefab = PickWeapon(interactor);
        if (prefab != null)
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
            waitingWeapon = Instantiate(prefab, pos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Weapon_Crate has no weapons configured.", this);
        }

        isOpening = false;
    }

    private Weapon PickWeapon(GameObject interactor)
    {
        Weapon_Data currentData = null;
        if (avoidCurrentWeapon && interactor != null)
        {
            Player_Weapons pw = interactor.GetComponent<Player_Weapons>();
            if (pw != null && pw.CurrentWeapon != null) currentData = pw.CurrentWeapon.Data;
        }

        Weapon result = Roll(currentData);
        return result != null ? result : Roll(null); // only option was the current weapon: allow it
    }

    /// <summary>Weighted random, skipping weapons that use <paramref name="exclude"/> data.</summary>
    private Weapon Roll(Weapon_Data exclude)
    {
        float total = 0f;
        foreach (WeaponEntry e in weapons)
            if (IsValid(e, exclude)) total += e.weight;

        if (total <= 0f) return null;

        float roll = UnityEngine.Random.Range(0f, total);
        foreach (WeaponEntry e in weapons)
        {
            if (!IsValid(e, exclude)) continue;
            roll -= e.weight;
            if (roll <= 0f) return e.prefab;
        }

        return null;
    }

    private static bool IsValid(WeaponEntry e, Weapon_Data exclude) =>
        e != null && e.prefab != null && e.weight > 0 && (exclude == null || e.prefab.Data != exclude);

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}
