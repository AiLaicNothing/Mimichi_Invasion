using System;
using UnityEngine;

/// <summary>
/// Finds the closest IInteractable in range every frame (for the prompt UI)
/// and uses it when E (Interact) is pressed.
/// </summary>
public class Player_Interaction : MonoBehaviour
{
    [SerializeField] private float interactRadius = 2f;
    [Tooltip("Layers interactables are on (weapons, crates...).")]
    [SerializeField] private LayerMask interactMask = ~0;

    /// <summary>Closest usable interactable, or null.</summary>
    public IInteractable Current { get; private set; }

    /// <summary>Fired when the focused interactable changes (null = nothing in range).</summary>
    public event Action<IInteractable> OnFocusChanged;

    private readonly Collider[] buffer = new Collider[16];
    private Player_InputsReciver inputs;
    private Player_Health health;

    private void Awake()
    {
        inputs = GetComponent<Player_InputsReciver>();
        health = GetComponent<Player_Health>();
    }

    private void Update()
    {
        bool blocked = Pause_Menu.IsPaused || (health != null && health.IsDead);

        IInteractable nearest = blocked ? null : FindNearest();
        if (nearest != Current)
        {
            Current = nearest;
            OnFocusChanged?.Invoke(Current);
        }

        if (!blocked && Current != null && inputs.hasInteracted)
            Current.Interact(gameObject);
    }

    private IInteractable FindNearest()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, interactRadius, buffer, interactMask, QueryTriggerInteraction.Collide);

        IInteractable nearest = null;
        float best = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            IInteractable interactable = buffer[i].GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract(gameObject)) continue;

            float dist = (buffer[i].bounds.center - transform.position).sqrMagnitude;
            if (dist < best)
            {
                best = dist;
                nearest = interactable;
            }
        }

        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
