using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Holds a freshly racked glass frozen in place until the player first picks it up,
// so a full rack can't knock itself over on spawn.
[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class RackedGlass : MonoBehaviour
{
    private Rigidbody body;
    private XRGrabInteractable grab;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        body.isKinematic = true;
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDestroy()
    {
        if (grab != null) grab.selectExited.RemoveListener(OnReleased);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // Still held by another interactor, e.g. passed hand to hand.
        if (grab.isSelected) return;

        // XRI restores the grab-time kinematic state on drop, which would leave
        // the glass frozen in midair, so hand it back to physics here.
        body.isKinematic = false;
        body.useGravity = true;

        // Only the first release matters; after that it's a normal glass.
        Destroy(this);
    }
}