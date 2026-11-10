using UnityEngine;
using UnityEngine.InputSystem;

public class Player_InputsReciver : MonoBehaviour
{ 
    public Vector2 moveInput {  get; private set; }
    public Vector2 lookAt {  get; private set; }
    public bool hasJumped {  get; private set; }
    public bool hasDashed { get; private set; }
    public bool hasInteracted { get; private set; }
    public bool hasShoot {  get; private set; }

    private void LateUpdate()
    {
        hasJumped = false;
        hasDashed = false;
        hasInteracted = false;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookAt = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started) hasJumped = true;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started) hasDashed = true;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started) hasInteracted = true;
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            hasShoot = true;
        }

        if (context.canceled)
        {
            hasShoot = false;
        }
    }
}
