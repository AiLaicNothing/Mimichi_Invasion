using UnityEngine;

/// <summary>
/// Anything the player can use with E (weapons on the ground, crates, doors, shops...).
/// Needs a Collider (trigger or solid) so Player_Interaction can detect it.
/// </summary>
public interface IInteractable
{
    /// <summary>Text for the on-screen prompt, e.g. "Buy weapon (500)". Empty = no prompt.</summary>
    string GetPrompt(GameObject interactor);

    /// <summary>Whether it can be used right now (already equipped, crate busy...).</summary>
    bool CanInteract(GameObject interactor);

    void Interact(GameObject interactor);
}
