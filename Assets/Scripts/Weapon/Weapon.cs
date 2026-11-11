using System.Collections;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [SerializeField] private Weapon_Data data;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    private int currentAmmo;
    private int totalAmmo;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip shootSfx;
    [SerializeField] private AudioClip reloadSfx;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        currentAmmo = data.magSize;
        totalAmmo = data.totalMag * data.magSize;
    }


    public virtual void OnShoot()
    {
        if (shootSfx != null) _audioSource.PlayOneShot(shootSfx);

        if (bulletPrefab != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        }
    }

    public virtual void OnReload()
    {
        Reload();
    }

    private IEnumerator Reload()
    {
        yield return new WaitForSeconds(data.reloadTime);

        int ammoReload = currentAmmo - data.magSize;

        totalAmmo -= ammoReload;

        currentAmmo = data.magSize;
    }
}
