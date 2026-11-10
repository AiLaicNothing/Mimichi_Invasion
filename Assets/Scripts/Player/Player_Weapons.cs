using System.Collections;
using UnityEngine;

public class Player_Weapons : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private GameObject camHolder;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float shotRange = 70f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float timeBtwShoot = 0.2f;

    [Header("Bullet Trail")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private TrailRenderer bulletTrailPrefab;
    [SerializeField] private float trailDuration = 0.1f;
    [SerializeField] private float trailTravelTime = 0.08f;
    private float shootTimer;

    private Player_InputsReciver inputsReciver;

    private void Awake()
    {
        inputsReciver = GetComponent<Player_InputsReciver>();
    }

    private void Update()
    {
        if (shootTimer > 0)
            shootTimer -= Time.deltaTime;

        if (inputsReciver.hasShoot && shootTimer <= 0)
        {
            Debug.Log("Shoot");
            Shoot();
            shootTimer = timeBtwShoot;
        }
    }

    private void Shoot()
    {
        Ray ray = new Ray(camHolder.transform.position, camHolder.transform.forward);

        Vector3 hitPoint = ray.origin + ray.direction * shotRange;

        if (Physics.Raycast(ray, out RaycastHit hit, shotRange))
        {
            hitPoint = hit.point;

            if (((1 << hit.collider.gameObject.layer) & targetLayer.value) != 0)
            {
                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

                if (damageable != null) damageable.TakeDamage(damage);
            }
        }

        if (bulletTrailPrefab != null && firePoint != null)
        {
            TrailRenderer trail = Instantiate(bulletTrailPrefab, firePoint.position, Quaternion.identity);

            StartCoroutine(SpawnTrail(trail, hitPoint));
        }
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint)
    {
        float time = 0f;
        Vector3 startPos = trail.transform.position;

        while (time < 1f)
        {
            time += Time.deltaTime / Mathf.Max(0.01f, trailTravelTime);

            trail.transform.position = Vector3.Lerp(startPos, hitPoint, Mathf.Clamp01(time));

            yield return null;
        }

        trail.transform.position = hitPoint;

        Destroy(trail.gameObject, trailDuration);
    }
}
