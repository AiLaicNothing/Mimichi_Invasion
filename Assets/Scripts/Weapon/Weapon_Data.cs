using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/Data")]
public class Weapon_Data : ScriptableObject
{
    [Header("Information")]
    public string _name;
    public WeaponType _weaponType;
    [TextArea(5, 10)]
    public string _description;
    public GameObject prefab;

    [Header("Stat Values")]
    public float damage;
    public float reloadTime;
    public int magSize;
    public int totalMag;

    public enum WeaponType
    {
        Pistol,
        Rifle,
        Shotgun,
        Sniper,
        Special
    }
}
