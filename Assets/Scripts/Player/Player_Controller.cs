using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Controller : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Jump")]
    [SerializeField] private float jumpForce;

    [Header("Dash")]
    [SerializeField] private float dashForce;
    [SerializeField] private float dashCD;
    private float dashTimer;

    [Header("Camera")]
    [SerializeField] private GameObject camHolder;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float maxClamp = 80f;

    private float cameraRotation;

    private Player_Health health;
    private Player_InputsReciver inputsReciver;
    private Rigidbody rb;
    private Animator anim;

    private void Awake()
    {
        health = GetComponent<Player_Health>();
        inputsReciver = GetComponent<Player_InputsReciver>();

        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        RotatePlayer();
        RotateCamera();
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Movement()
    {
        Vector3 direction = transform.right * inputsReciver.moveInput.x + transform.forward * inputsReciver.moveInput.y;

        direction = Vector3.ClampMagnitude(direction, 1f);

        rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);
    }

    private void RotatePlayer()
    {
        transform.Rotate(0, inputsReciver.lookAt.x * mouseSensitivity, 0);
    }

    private void RotateCamera()
    {
        cameraRotation -= inputsReciver.lookAt.y * mouseSensitivity;
        cameraRotation = Mathf.Clamp(cameraRotation, -maxClamp, maxClamp);

        camHolder.transform.localRotation = Quaternion.Euler(cameraRotation, 0, 0);
    }
}

