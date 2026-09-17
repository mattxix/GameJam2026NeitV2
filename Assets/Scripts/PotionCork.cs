using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// Cork starts parented to the bottle (kinematic, rides along).
/// First grab unparents it and turns its Rigidbody on.
[RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
public class PotionCork : MonoBehaviour
{
    XRGrabInteractable grab;
    Rigidbody rb;
    bool detached;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        grab.retainTransformParent = false; // don't re-parent to the bottle on drop

        // Seated: ride along with the bottle, no physics.
        rb.isKinematic = true;
        rb.useGravity = false;

        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (detached) return;
        detached = true;

        transform.SetParent(null, true);
        EnablePhysics();
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // XRI restores the kinematic state it saw at grab time (true on the
        // first grab), so re-enable physics here as well.
        if (!grab.isSelected) EnablePhysics();
    }

    void EnablePhysics()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
    }
}