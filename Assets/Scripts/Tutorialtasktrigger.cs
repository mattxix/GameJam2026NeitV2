using UnityEngine;

/// <summary>
/// Completes a tutorial step when something happens in the world.
///
/// Two ways to use it:
///  1. Call Fire() from any UnityEvent in the Inspector (e.g. an
///     XRGrabInteractable's Select Entered) or from code.
///  2. Tick "Use Trigger Zone" and put it on an object with a trigger
///     collider. It fires when an object with the required tag enters.
///
/// It only counts when one of its steps is the panel currently showing,
/// so doing things early or out of order never skips ahead.
/// </summary>
public class TutorialTaskTrigger : MonoBehaviour
{
    [Tooltip("Which step(s) this completes. It only fires for whichever of these is currently showing.")]
    [SerializeField] private TutorialStep[] steps = { TutorialStep.GrabGlass };

    [Header("Trigger zone (optional)")]
    [Tooltip("Fire when a tagged object enters this object's trigger collider.")]
    [SerializeField] private bool useTriggerZone = false;

    [Tooltip("Tag the entering object (or its Rigidbody root) must have. Leave empty to accept anything.")]
    [SerializeField] private string requiredTag = "Glass";

    /// <summary>Completes the current step if it's one of this trigger's steps.</summary>
    public void Fire()
    {
        var tutorial = TutorialManager.Instance;
        if (tutorial == null || !tutorial.IsRunning) return;

        foreach (var step in steps)
        {
            if (tutorial.CurrentStep == step)
            {
                tutorial.Complete(step);
                return;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTriggerZone) return;
        if (!HasRequiredTag(other)) return;
        Fire();
    }

    private bool HasRequiredTag(Collider other)
    {
        if (string.IsNullOrEmpty(requiredTag)) return true;
        if (other.CompareTag(requiredTag)) return true;

        // Grabbable objects often have their colliders on child objects,
        // so also check the object that owns the Rigidbody.
        var body = other.attachedRigidbody;
        return body != null && body.CompareTag(requiredTag);
    }
}