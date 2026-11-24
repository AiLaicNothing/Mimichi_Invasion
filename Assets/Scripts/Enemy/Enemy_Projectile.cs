using UnityEngine;

/// <summary>
/// Straight-flying enemy bullet. Moves manually and sweeps a sphere each frame,
/// so it never tunnels through thin walls and needs no Rigidbody/Collider.
/// </summary>
public class Enemy_Projectile : MonoBehaviour
{
    [SerializeField] private float radius = 0.15f;
    [SerializeField] private float lifetime = 5f;
    [Tooltip("What the projectile can hit: Player + Ground + Wall.")]
    [SerializeField] private LayerMask hitMask = (1 << 8) | (1 << 9) | (1 << 14);
    [SerializeField] private GameObject hitVfx;

    private Vector3 direction;
    private float speed;
    private float damage;
    private GameObject owner;
    private bool launched;

    public void Launch(Vector3 dir, float projectileSpeed, float projectileDamage, GameObject shooter)
    {
        direction = dir.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        owner = shooter;
        launched = true;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!launched) return;

        float step = speed * Time.deltaTime;

        if (Physics.SphereCast(transform.position, radius, direction, out RaycastHit hit, step, hitMask, QueryTriggerInteraction.Ignore)
            && (owner == null || !hit.collider.transform.IsChildOf(owner.transform)))
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(damage, hit.point, direction);

            if (hitVfx != null) Instantiate(hitVfx, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(gameObject);
            return;
        }

        transform.position += direction * step;
    }
}
