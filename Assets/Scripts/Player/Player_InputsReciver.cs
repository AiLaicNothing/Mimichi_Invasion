using UnityEngine;
using UnityEngine.InputSystem;

public class Player_InputsReciver : MonoBehaviour
{
    public Vector2 moveInput { get; private set; }
    /// <summary>Mouse position in screen pixels.</summary>
    public Vector2 lookAt { get; private set; }
    public bool hasJumped { get; private set; }
    public bool hasDashed { get; private set; }
    public bool hasInteracted { get; private set; }
    public bool hasReloaded { get; private set; }
    public bool hasPaused { get; private set; }
    /// <summary>True while the shoot button is held (automatic weapons).</summary>
    public bool hasShoot { get; private set; }
    /// <summary>True only on the frame the shoot button was pressed (semi-auto weapons).</summary>
    public bool hasShootPressed { get; private set; }

    // One-frame flags are cleared after every script has read them in Update.
    private void LateUpdate()
    {
        hasJumped = false;
        hasDashed = false;
        hasInteracted = false;
        hasReloaded = false;
        hasPaused = false;
        hasShootPressed = false;
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

    public void OnReload(InputAction.CallbackContext context)
    {
        if (context.started) hasReloaded = true;
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.started) hasPaused = true;
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            hasShoot = true;
            hasShootPressed = true;
        }

        if (context.canceled) hasShoot = false;
    }
}
