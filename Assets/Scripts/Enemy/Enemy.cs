using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Base enemy: health, chasing the player, attack timing and ragdoll death.
///
/// Subclasses decide HOW they attack (Attack) and can change WHERE they move
/// (GetDesiredMoveDirection) or WHEN they can attack (CanAttack).
///
/// Flow every frame (while alive):
///   - CanAttack()?  -> stop and call Attack() when the cooldown is ready
///   - otherwise     -> move with GetDesiredMoveDirection()
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class Enemy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] protected float maxHealth = 50f;
    protected float currentHealth;

    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 3.5f;
    [SerializeField] protected float acceleration = 25f;
    [SerializeField] protected float rotationSpeed = 540f;
    [Tooltip("Walls / props to steer around.")]
    [SerializeField] protected LayerMask whatIsObstacle = 1 << 9;
    [SerializeField] protected float obstacleCheckDistance = 1.5f;

    [Header("Pathfinding (NavMesh queries, no NavMeshAgent)")]
    [Tooltip("Seconds between path recalculations.")]
    [SerializeField] protected float repathInterval = 0.3f;
    [Tooltip("Distance at which a path corner counts as reached.")]
    [SerializeField] protected float cornerReachDistance = 0.5f;
    [Tooltip("How far from the NavMesh a position may be and still be snapped onto it.")]
    [SerializeField] protected float navSampleDistance = 2f;
    [Tooltip("Walk straight when the destination is visible and on (almost) the same floor height.")]
    [SerializeField] protected float directMaxHeightDiff = 0.5f;

    [Header("Attack")]
    [SerializeField] protected float damage = 10f;
    [Tooltip("Distance at which the enemy stops and attacks.")]
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected float timeBtwAttack = 1f;
    [Tooltip("Layers that block line of sight to the player.")]
    [SerializeField] protected LayerMask lineOfSightMask = 1 << 9;

    [Header("Reward")]
    [Tooltip("Money given to the player when this enemy dies.")]
    [SerializeField] protected int moneyReward = 10;
    public int MoneyReward => moneyReward;

    [Header("Death")]
    [Tooltip("Layer the body switches to on death.")]
    [SerializeField] private string deadBodyLayer = "DeadBody";
    [Tooltip("Layers the dead body will NOT collide with (player, living enemies, weapons).")]
    [SerializeField] private LayerMask deadBodyIgnore = (1 << 14) | (1 << 15) | (1 << 17);
    [SerializeField] private float launchForce = 8f;
    [SerializeField] private float launchUpForce = 3f;
    [SerializeField] private float launchTorque = 10f;
    [Tooltip("Seconds before the body is removed. 0 = never.")]
    [SerializeField] private float bodyLifetime = 10f;

    public bool IsDead { get; private set; }
    public float HealthPercent => currentHealth / maxHealth;

    /// <summary>Raised once when this enemy dies (Wave_Manager / score / drops).</summary>
    public event Action<Enemy> OnDied;
    /// <summary>Global version, handy for managers that don't track each enemy.</summary>
    public static event Action<Enemy> OnAnyEnemyDied;

    protected Rigidbody rb;
    protected Animator anim;
    protected Transform target;
    protected Player_Health targetHealth;

    protected float attackTimer;
    /// <summary>True while an attack is playing (wind-up, shooting...). Enemy won't move.</summary>
    protected bool isAttacking;

    /// <summary>True while a scripted movement (dash, knockback...) controls the Rigidbody.</summary>
    protected bool isDashing;

    protected Vector3 moveDirection;
    private Vector3 lastHitDirection;

    // Pathfinding
    private NavMeshPath path;
    private int cornerIndex;
    private float repathTimer;
    private Vector3 pathDestination;
    private bool hasPath;
    /// <summary>Distance from the pivot down to the feet (NavMesh points are at floor level).</summary>
    private float feetOffset;

    protected float DistanceToTarget =>
        target == null ? float.MaxValue : FlatDistance(transform.position, target.position);

    // ---------- Unity ----------

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        currentHealth = maxHealth;

        path = new NavMeshPath();
        repathTimer = UnityEngine.Random.Range(0f, repathInterval); // spread repaths across frames

        Collider col = GetComponent<Collider>();
        feetOffset = col != null ? transform.position.y - col.bounds.min.y : 0f;
    }

    protected virtual void Start()
    {
        FindTarget();
    }

    protected virtual void Update()
    {
        if (IsDead) return;

        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        if (repathTimer > 0) repathTimer -= Time.deltaTime;

        if (isDashing) return;

        if (target == null || (targetHealth != null && targetHealth.IsDead))
        {
            FindTarget();
            moveDirection = Vector3.zero;
            return;
        }

        if (isAttacking)
        {
            moveDirection = Vector3.zero;
            return;
        }

        if (CanAttack())
        {
            moveDirection = Vector3.zero;

            if (attackTimer <= 0)
            {
                attackTimer = timeBtwAttack;
                Attack();
            }
        }
        else
        {
            moveDirection = GetDesiredMoveDirection();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (IsDead || isDashing) return;

        Move();
        FaceTarget();
    }

    // ---------- Overridable behaviour ----------

    /// <summary>Perform the attack. Called when CanAttack() is true and the cooldown is ready.</summary>
    protected abstract void Attack();

    /// <summary>Default: in range and with line of sight.</summary>
    protected virtual bool CanAttack()
    {
        return DistanceToTarget <= attackRange && HasLineOfSight();
    }

    /// <summary>Default: follow the NavMesh path to the player.</summary>
    protected virtual Vector3 GetDesiredMoveDirection()
    {
        return target != null ? GetPathDirection(target.position) : Vector3.zero;
    }

    /// <summary>Extra logic on death (drops, VFX, sounds). Call base.</summary>
    protected virtual void OnDeath() { }

    // ---------- Helpers for subclasses ----------

    protected Vector3 DirectionToTarget()
    {
        if (target == null) return Vector3.zero;
        Vector3 dir = target.position - transform.position;
        dir.y = 0;
        return dir.normalized;
    }

    /// <summary>
    /// Can this enemy SEE the player? Used to decide if it may attack (no shooting through walls)
    /// and as a shortcut to walk straight. Enemies always KNOW where the player is.
    /// </summary>
    protected bool HasLineOfSight() => target != null && HasLineOfSight(target.position);

    protected bool HasLineOfSight(Vector3 point)
    {
        Vector3 eye = transform.position + Vector3.up * 0.5f;
        Vector3 targetPoint = point + Vector3.up * 0.5f;
        return !Physics.Linecast(eye, targetPoint, lineOfSightMask, QueryTriggerInteraction.Ignore);
    }

    protected static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0; b.y = 0;
        return Vector3.Distance(a, b);
    }

    // ---------- Pathfinding ----------

    private readonly Vector3[] corners = new Vector3[32];
    private int cornerCount;

    /// <summary>
    /// Direction to walk to reach <paramref name="destination"/> following the NavMesh.
    /// Walks straight if nothing on the NavMesh is in the way. Falls back to a straight
    /// line when off the mesh (mid-air, knocked back) so the enemy never freezes.
    /// </summary>
    protected Vector3 GetPathDirection(Vector3 destination)
    {
        Vector3 straight = FlatDirection(transform.position, destination);

        if (!TryGetNavPoint(transform.position, out Vector3 selfNav) ||
            !TryGetNavPoint(destination, out Vector3 destNav))
        {
            hasPath = false;
            return straight;
        }

        // Shortcut: same floor and a clear straight line on the NavMesh (no walls, holes, ledges).
        bool sameFloor = Mathf.Abs(selfNav.y - destNav.y) <= directMaxHeightDiff;
        if (sameFloor && !NavMesh.Raycast(selfNav, destNav, out _, NavMesh.AllAreas))
        {
            hasPath = false;
            return FlatDirection(transform.position, destNav);
        }

        if (!hasPath || repathTimer <= 0 || (destNav - pathDestination).sqrMagnitude > 4f)
            RecalculatePath(selfNav, destNav);

        if (!hasPath) return straight;

        // Skip the corners we've already reached.
        while (cornerIndex < cornerCount && FlatDistance(transform.position, corners[cornerIndex]) <= cornerReachDistance)
            cornerIndex++;

        if (cornerIndex >= cornerCount) return FlatDirection(transform.position, destNav);

        return FlatDirection(transform.position, corners[cornerIndex]);
    }

    private void RecalculatePath(Vector3 from, Vector3 to)
    {
        repathTimer = repathInterval;
        pathDestination = to;

        hasPath = NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) && path.status != NavMeshPathStatus.PathInvalid;
        if (!hasPath) return;

        cornerCount = path.GetCornersNonAlloc(corners);
        cornerIndex = 1; // corner 0 is our own position
    }

    /// <summary>Forget the current path (after a dash, teleport or knockback).</summary>
    protected void InvalidatePath()
    {
        hasPath = false;
        repathTimer = 0;
    }

    /// <summary>Closest point on the NavMesh (floor level) to <paramref name="point"/>.</summary>
    protected bool TryGetNavPoint(Vector3 point, out Vector3 navPoint)
    {
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, navSampleDistance, NavMesh.AllAreas))
        {
            navPoint = hit.position;
            return true;
        }

        navPoint = point;
        return false;
    }

    protected static Vector3 FlatDirection(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        dir.y = 0;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
    }

    // ---------- Dash / Teleport helpers ----------

    /// <summary>
    /// Physical dash: pushes the Rigidbody for <paramref name="duration"/> seconds.
    /// Use with StartCoroutine. Pathfinding resumes from wherever it ends.
    /// </summary>
    protected IEnumerator DashRoutine(Vector3 direction, float speed, float duration)
    {
        isDashing = true;
        direction.y = 0;
        direction.Normalize();

        float t = 0f;
        while (t < duration && !IsDead)
        {
            rb.linearVelocity = new Vector3(direction.x * speed, rb.linearVelocity.y, direction.z * speed);
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
        InvalidatePath();
    }

    /// <summary>
    /// Instantly moves to the closest valid NavMesh point near <paramref name="point"/>.
    /// Returns false (and doesn't move) if there's no walkable ground there.
    /// </summary>
    protected bool TryTeleport(Vector3 point)
    {
        if (!TryGetNavPoint(point, out Vector3 navPoint)) return false;

        Vector3 newPos = navPoint + Vector3.up * feetOffset;
        rb.position = newPos;
        transform.position = newPos;
        rb.linearVelocity = Vector3.zero;

        InvalidatePath();
        return true;
    }

    // ---------- Movement ----------

    private void Move()
    {
        Vector3 targetVelocity = moveDirection * moveSpeed;
        Vector3 currentFlat = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 newFlat = Vector3.MoveTowards(currentFlat, targetVelocity, acceleration * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(newFlat.x, rb.linearVelocity.y, newFlat.z);
    }

    private void FaceTarget()
    {
        Vector3 look = target != null ? DirectionToTarget() : moveDirection;
        if (look.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(look, Vector3.up);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, rot, rotationSpeed * Time.fixedDeltaTime));
    }

    /// <summary>
    /// Simple steering: if a wall is ahead, slide along it.
    /// Not needed for NavMesh paths; useful for enemies that move off the mesh (flying).
    /// </summary>
    protected Vector3 AvoidObstacles(Vector3 desired)
    {
        if (desired.sqrMagnitude < 0.001f) return desired;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.SphereCast(origin, 0.3f, desired, out RaycastHit hit, obstacleCheckDistance, whatIsObstacle, QueryTriggerInteraction.Ignore))
        {
            Vector3 normal = hit.normal; normal.y = 0;
            Vector3 slide = Vector3.ProjectOnPlane(desired, normal).normalized;
            return (slide + normal * 0.3f).normalized;
        }

        return desired;
    }

    private void FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) { target = null; return; }

        targetHealth = player.GetComponent<Player_Health>();
        target = (targetHealth != null && targetHealth.IsDead) ? null : player.transform;
    }

    // ---------- Damage / Death ----------

    public void TakeDamage(float damage) => TakeDamage(damage, transform.position, Vector3.zero);

    public virtual void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (IsDead) return;

        currentHealth -= damage;
        if (hitDirection.sqrMagnitude > 0.001f) lastHitDirection = hitDirection.normalized;

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        IsDead = true;
        isAttacking = false;
        isDashing = false;
        StopAllCoroutines();

        BecomeRagdoll();

        OnDeath();
        OnDied?.Invoke(this);
        OnAnyEnemyDied?.Invoke(this);

        if (bodyLifetime > 0) Destroy(gameObject, bodyLifetime);
    }

    /// <summary>
    /// No bones: the whole body is one rigidbody that tumbles.
    /// It switches to the DeadBody layer and ignores player / enemies / weapons,
    /// so it only collides with the world and other corpses.
    /// </summary>
    private void BecomeRagdoll()
    {
        if (anim != null) anim.enabled = false;

        int layer = LayerMask.NameToLayer(deadBodyLayer);
        if (layer >= 0) SetLayerRecursive(gameObject, layer);
        else Debug.LogWarning($"Layer '{deadBodyLayer}' does not exist. Add it in Tags & Layers.", this);

        tag = "Untagged";

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.excludeLayers = deadBodyIgnore;
        rb.excludeLayers = deadBodyIgnore;

        rb.constraints = RigidbodyConstraints.None;
        rb.linearVelocity = Vector3.zero;

        // Launched the way the bullet was travelling = away from the shooter.
        Vector3 dir = lastHitDirection.sqrMagnitude > 0.001f ? lastHitDirection : -transform.forward;
        dir.y = 0;
        dir.Normalize();

        rb.AddForce(dir * launchForce + Vector3.up * launchUpForce, ForceMode.Impulse);

        // Tumble backwards (rotate around the axis perpendicular to the launch) + some randomness.
        Vector3 tumbleAxis = Vector3.Cross(Vector3.up, dir);
        rb.AddTorque((tumbleAxis + UnityEngine.Random.insideUnitSphere * 0.5f) * launchTorque, ForceMode.Impulse);
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, layer);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Current path (play mode)
        if (!hasPath) return;
        Gizmos.color = Color.green;
        for (int i = Mathf.Max(1, cornerIndex); i < cornerCount; i++)
            Gizmos.DrawLine(corners[i - 1], corners[i]);
    }
}
