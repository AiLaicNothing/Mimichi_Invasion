using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Weapon panel: background image + weapon icon + ammo text.
/// - Icon: uses Weapon_Data.icon. If the weapon has no icon, the Image keeps whatever sprite it already has.
/// - Ammo: "12 / 48", "RELOADING..." while reloading, "--" when unarmed.
/// If no Player_Weapons is assigned, it finds the object tagged "Player".
/// </summary>
public class Weapon_UI : MonoBehaviour
{
    [SerializeField] private Player_Weapons playerWeapons;

    [Header("UI")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("Text")]
    [Tooltip("{0} = ammo in magazine, {1} = reserve ammo")]
    [SerializeField] private string ammoFormat = "{0} / {1}";
    [SerializeField] private string infiniteReserveText = "INF";
    [SerializeField] private string reloadingText = "RELOADING...";
    [SerializeField] private string unarmedText = "--";

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 0.3f, 0.3f);

    private Weapon current;

    private void Start()
    {
        if (playerWeapons == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerWeapons = player.GetComponent<Player_Weapons>();
        }

        if (playerWeapons == null)
        {
            Debug.LogWarning("Weapon_UI: no Player_Weapons found.", this);
            return;
        }

        playerWeapons.OnWeaponChanged += SetWeapon;
        SetWeapon(playerWeapons.CurrentWeapon);
    }

    private void OnDestroy()
    {
        if (playerWeapons != null) playerWeapons.OnWeaponChanged -= SetWeapon;
        Unsubscribe();
    }

    private void SetWeapon(Weapon weapon)
    {
        Unsubscribe();
        current = weapon;

        if (current != null)
        {
            current.OnAmmoChanged += HandleAmmoChanged;
            current.OnReloadStateChanged += HandleReloadChanged;

            // Only replace the sprite if this weapon has one.
            if (weaponIcon != null && current.Data.icon != null)
                weaponIcon.sprite = current.Data.icon;
        }

        Refresh();
    }

    private void Unsubscribe()
    {
        if (current == null) return;
        current.OnAmmoChanged -= HandleAmmoChanged;
        current.OnReloadStateChanged -= HandleReloadChanged;
    }

    private void HandleAmmoChanged(int mag, int reserve) => Refresh();
    private void HandleReloadChanged(bool reloading) => Refresh();

    private void Refresh()
    {
        if (ammoText == null) return;

        if (current == null)
        {
            ammoText.text = unarmedText;
            ammoText.color = normalColor;
            return;
        }

        if (current.IsReloading)
        {
            ammoText.text = reloadingText;
            ammoText.color = normalColor;
            return;
        }

        string reserve = current.Data.infiniteAmmo ? infiniteReserveText : current.ReserveAmmo.ToString();
        ammoText.text = string.Format(ammoFormat, current.CurrentAmmo, reserve);
        ammoText.color = current.CurrentAmmo <= 0 ? emptyColor : normalColor;
    }
}
