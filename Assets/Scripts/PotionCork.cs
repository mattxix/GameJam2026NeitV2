using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// Cork starts parented to the bottle (kinematic, rides along).
/// First grab pops it off into world space; on release it becomes a normal physics object.
[RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
public class PotionCork : MonoBehaviour
{
    

    [Tooltip("Seconds after pulling the cork before it can collide with the bottle again.")]
    [SerializeField] float collisionReenableDelay = 0.5f;

    XRGrabInteractable grab;
    Rigidbody rb;
    Collider[] corkColliders;
    bool detached;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        corkColliders = GetComponentsInChildren<Collider>();

        // Don't snap back under the bottle when dropped.
        grab.retainTransformParent = false;

        // Seated: follow the bottle's transform, no physics.
        rb.isKinematic = true;
        rb.useGravity = false;
        SetBottleCollision(false);

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

        // XRI normally unparents on grab already; this just guarantees it.
        transform.SetParent(null, true);
        Invoke(nameof(RestoreBottleCollision), collisionReenableDelay);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (grab.isSelected) return; // still held by the other hand

        // XRI restores the kinematic state from grab time during Drop,
        // which runs before this event — so override it here.
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    void RestoreBottleCollision() => SetBottleCollision(true);

    void SetBottleCollision(bool enabled)
    {
       // foreach (var c in corkColliders)
            //foreach (var b in bottleColliders)
                //if (c && b) Physics.IgnoreCollision(c, b, !enabled);
    }
}