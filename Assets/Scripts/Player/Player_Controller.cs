using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Isometric top-down controller.
/// - WASD moves relative to the camera (W = "up" on screen).
/// - The player always faces the mouse cursor projected on the ground plane.
/// - Shift dashes in the move direction (or aim direction if standing still).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Player_Controller : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float rotationSpeed = 1080f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 22f;
    [SerializeField] private float dashDuration = 0.15f;
    [Tooltip("Cooldown in seconds, counted from the END of the dash.")]
    [SerializeField] private float dashCD = 1f;
    [Tooltip("Extra invulnerability after the dash ends (forgiveness window).")]
    [SerializeField] private float dashExtraInvulnerability = 0.05f;
    [Tooltip("Pass through enemies while dashing (still collides with walls).")]
    [SerializeField] private bool dashThroughEnemies = true;
    [SerializeField] private LayerMask enemyLayer = 1 << 15;
    private float dashTimer;
    public bool IsDashing { get; private set; }
    /// <summary>0 = ready, 1 = just used. For a cooldown UI.</summary>
    public float DashCooldownPercent => dashCD > 0 ? Mathf.Clamp01(dashTimer / dashCD) : 0f;
    public bool CanDash => dashTimer <= 0 && !IsDashing;
    public event System.Action OnDash;

    [Header("Aim")]
    [Tooltip("Cinemachine camera following the player. Used to find the real Unity Camera (via its CinemachineBrain) for mouse aiming and camera-relative movement.")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    private Camera cam;

    /// <summary>World point under the mouse cursor, at the player's height.</summary>
    public Vector3 AimPoint { get; private set; }
    /// <summary>Flat (y = 0) normalized direction from the player to the cursor.</summary>
    public Vector3 AimDirection { get; private set; } = Vector3.forward;

    private Player_Health health;
    private Player_InputsReciver inputsReciver;
    private Rigidbody rb;
    private Animator anim;

    private Vector3 moveDirection;

    private void Awake()
    {
        health = GetComponent<Player_Health>();
        inputsReciver = GetComponent<Player_InputsReciver>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Start()
    {
        // Top-down shooter: cursor must be visible to aim.
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (health != null && health.IsDead) return;
        if (Pause_Menu.IsPaused) return;

        if (cam == null) cam = ResolveOutputCamera();

        moveDirection = GetCameraRelativeInput();
        UpdateAim();

        if (dashTimer > 0) dashTimer -= Time.deltaTime;
        if (inputsReciver.hasDashed && CanDash)
            StartCoroutine(Dash());
    }

    private void FixedUpdate()
    {
        if (health != null && health.IsDead)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        if (!IsDashing) Movement();
        Rotate();
    }

    /// <summary>
    /// A CinemachineCamera is not a real camera: the Unity Camera with the CinemachineBrain renders it.
    /// That output camera is what we need for ScreenPointToRay.
    /// </summary>
    private Camera ResolveOutputCamera()
    {
        if (cinemachineCamera != null)
        {
            CinemachineBrain brain = CinemachineCore.FindPotentialTargetBrain(cinemachineCamera);
            if (brain != null && brain.OutputCamera != null) return brain.OutputCamera;
        }

        return Camera.main;
    }

    private Vector3 GetCameraRelativeInput()
    {
        Vector2 input = inputsReciver.moveInput;
        if (cam == null) return new Vector3(input.x, 0, input.y);

        // Flatten camera axes so the isometric tilt doesn't affect speed.
        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;

        return Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
    }

    private void Movement()
    {
        Vector3 targetVelocity = moveDirection * moveSpeed;
        Vector3 currentFlat = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 newFlat = Vector3.MoveTowards(currentFlat, targetVelocity, acceleration * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(newFlat.x, rb.linearVelocity.y, newFlat.z);
    }

    private void UpdateAim()
    {
        if (cam == null) return;

        // Intersect the mouse ray with a horizontal plane at the player's height.
        Ray ray = cam.ScreenPointToRay(inputsReciver.lookAt);
        Plane ground = new Plane(Vector3.up, transform.position);

        if (ground.Raycast(ray, out float enter))
        {
            AimPoint = ray.GetPoint(enter);

            Vector3 dir = AimPoint - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f) AimDirection = dir.normalized;
        }
    }

    private void Rotate()
    {
        Quaternion target = Quaternion.LookRotation(AimDirection, Vector3.up);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target, rotationSpeed * Time.fixedDeltaTime));
    }

    private IEnumerator Dash()
    {
        IsDashing = true;
        OnDash?.Invoke();

        // Invulnerable for the whole dash (+ a tiny buffer).
        if (health != null) health.SetInvulnerable(dashDuration + dashExtraInvulnerability);

        LayerMask previousExclude = rb.excludeLayers;
        if (dashThroughEnemies) rb.excludeLayers = previousExclude | enemyLayer;

        Vector3 dir = moveDirection.sqrMagnitude > 0.01f ? moveDirection.normalized : AimDirection;
        float t = 0f;

        while (t < dashDuration)
        {
            rb.linearVelocity = new Vector3(dir.x * dashSpeed, rb.linearVelocity.y, dir.z * dashSpeed);
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Keep a bit of momentum instead of stopping dead; Movement() eases it back to normal speed.
        rb.linearVelocity = new Vector3(dir.x * moveSpeed, rb.linearVelocity.y, dir.z * moveSpeed);

        rb.excludeLayers = previousExclude;
        IsDashing = false;
        dashTimer = dashCD;
    }
}
