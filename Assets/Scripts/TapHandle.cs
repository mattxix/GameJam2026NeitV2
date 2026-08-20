using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


public class TapHandle : XRBaseInteractable
{
    [System.Serializable] public class FloatEvent : UnityEvent<float> { }

    [Header("Hinge")]
    [Tooltip("Transform that actually rotates. Leave empty to rotate this object.")]
    [SerializeField] private Transform pivot;
    [Tooltip("Rotation axis in the pivot's local space. X = pull toward/away from you.")]
    [SerializeField] private Vector3 localAxis = Vector3.right;
    [SerializeField] private float minAngle = 0f;
    [SerializeField] private float maxAngle = 60f;

    [Header("Release")]
    [SerializeField] private bool springBack = true;
    [Tooltip("Degrees per second the handle snaps back when let go.")]
    [SerializeField] private float springSpeed = 240f;

    [Header("Pour")]
    [Range(0f, 1f)][SerializeField] private float pourThreshold = 0.5f;
    [Tooltip("Must drop below this to stop. Keep it under pourThreshold to avoid flickering at the edge.")]
    [Range(0f, 1f)][SerializeField] private float stopThreshold = 0.4f;

    [Header("Haptics")]
    [SerializeField] private bool hapticsEnabled = true;
    [Tooltip("Rumble strength when the handle is barely open.")]
    [Range(0f, 1f)][SerializeField] private float minAmplitude = 0.04f;
    [Tooltip("Rumble strength when the handle is fully open.")]
    [Range(0f, 1f)][SerializeField] private float maxAmplitude = 0.55f;
    [Tooltip("Shapes how openness maps to strength. Ease-in feels heavier than linear.")]
    [SerializeField] private AnimationCurve rumbleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Seconds between rumble pulses. Below ~0.04 most headsets start dropping them.")]
    [SerializeField] private float pulseInterval = 0.06f;
    [Tooltip("Distinct click when the valve opens or closes.")]
    [Range(0f, 1f)][SerializeField] private float pourClickAmplitude = 0.7f;
    [Tooltip("Distinct click when the handle bottoms out or snaps shut.")]
    [Range(0f, 1f)][SerializeField] private float limitClickAmplitude = 0.9f;
    [SerializeField] private float clickDuration = 0.05f;

    public UnityEvent onPourStart;
    public UnityEvent onPourStop;
    [Tooltip("Fires with 0-1 openness every time the handle moves. Drive flow rate with this.")]
    public FloatEvent onValueChanged;

    /// <summary>0 = closed, 1 = fully open.</summary>
    public float Value => Mathf.InverseLerp(minAngle, maxAngle, angle);
    public bool IsPouring => pouring;

    private Quaternion restRotation;
    private float angle;
    private bool pouring;
    private Transform hand;
    private Vector3 lastPlanar;

    private HapticImpulsePlayer hapticPlayer;
    private float nextPulseTime;
    private bool wasAtMax;
    private bool wasAtMin = true;

    protected override void Awake()
    {
        base.Awake();
        if (pivot == null) pivot = transform;
        restRotation = pivot.localRotation;
        angle = minAngle;
        ApplyRotation();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        hand = args.interactorObject.transform;
        lastPlanar = PlanarToHand();

        // Grab whichever hand actually took the handle, not a cached one.
        hapticPlayer = args.interactorObject.transform.GetComponentInParent<HapticImpulsePlayer>();
        nextPulseTime = 0f;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        hand = null;
        // Keep hapticPlayer so the spring-back thunk still lands in the right hand.
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase phase)
    {
        base.ProcessInteractable(phase);
        if (phase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) return;

        float previous = angle;

        if (hand != null)
        {
            Vector3 planar = PlanarToHand();
            // Accumulate frame-to-frame delta instead of measuring from the grab point.
            // Clamping then feels right: overshoot stops at the limit and reverses instantly.
            if (planar.sqrMagnitude > 0.0001f && lastPlanar.sqrMagnitude > 0.0001f)
            {
                float delta = Vector3.SignedAngle(lastPlanar, planar, AxisWorld());
                angle = Mathf.Clamp(angle + delta, minAngle, maxAngle);
                lastPlanar = planar;
            }
        }
        else if (springBack)
        {
            angle = Mathf.MoveTowards(angle, minAngle, springSpeed * Time.deltaTime);
        }

        if (!Mathf.Approximately(angle, previous))
        {
            ApplyRotation();
            onValueChanged?.Invoke(Value);
        }

        EvaluatePour();
        EvaluateLimits();
        UpdateRumble();
    }

    private void EvaluatePour()
    {
        float v = Value;
        if (!pouring && v >= pourThreshold)
        {
            pouring = true;
            Click(pourClickAmplitude);
            onPourStart?.Invoke();
        }
        else if (pouring && v <= stopThreshold)
        {
            pouring = false;
            Click(pourClickAmplitude);
            onPourStop?.Invoke();
        }
    }

    private void EvaluateLimits()
    {
        bool atMax = angle >= maxAngle - 0.01f;
        bool atMin = angle <= minAngle + 0.01f;

        // Bottoming out only reads as an impact if you drove it there yourself.
        if (atMax && !wasAtMax && hand != null) Click(limitClickAmplitude);
        // Snapping shut reads as an impact either way.
        if (atMin && !wasAtMin) Click(limitClickAmplitude * 0.6f);

        wasAtMax = atMax;
        wasAtMin = atMin;
    }

    private void UpdateRumble()
    {
        if (!hapticsEnabled || hapticPlayer == null || hand == null) return;
        if (Time.time < nextPulseTime) return;

        float amp = Mathf.Lerp(minAmplitude, maxAmplitude, rumbleCurve.Evaluate(Value));
        nextPulseTime = Time.time + pulseInterval;

        if (amp < 0.01f) return;
        // Duration overruns the interval so consecutive pulses blur into a rumble
        // instead of reading as a stutter.
        hapticPlayer.SendHapticImpulse(amp, pulseInterval * 1.5f);
    }

    private void Click(float amplitude)
    {
        if (!hapticsEnabled || hapticPlayer == null) return;
        hapticPlayer.SendHapticImpulse(Mathf.Clamp01(amplitude), clickDuration);
        // Hold off the rumble so it doesn't cut the click short.
        nextPulseTime = Time.time + clickDuration;
    }

    private void ApplyRotation()
    {
        pivot.localRotation = restRotation * Quaternion.AngleAxis(angle, localAxis);
    }

    private Vector3 AxisWorld()
    {
        Quaternion parentRot = pivot.parent != null ? pivot.parent.rotation : Quaternion.identity;
        return (parentRot * restRotation * localAxis).normalized;
    }

    private Vector3 PlanarToHand()
    {
        return Vector3.ProjectOnPlane(hand.position - pivot.position, AxisWorld());
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform p = pivot != null ? pivot : transform;
        Quaternion parentRot = p.parent != null ? p.parent.rotation : Quaternion.identity;
        Quaternion rest = Application.isPlaying ? restRotation : p.localRotation;
        Vector3 axis = (parentRot * rest * localAxis).normalized;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p.position - axis * 0.1f, p.position + axis * 0.1f);

        Vector3 reference = Vector3.ProjectOnPlane(p.up, axis).normalized * 0.08f;
        Gizmos.color = Color.green;
        Gizmos.DrawRay(p.position, Quaternion.AngleAxis(minAngle, axis) * reference);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(p.position, Quaternion.AngleAxis(maxAngle, axis) * reference);
    }
    #endif
}