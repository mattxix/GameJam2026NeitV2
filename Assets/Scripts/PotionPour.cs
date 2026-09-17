using UnityEngine;

/// Sits on the potion bottle. Listens to the LiquidGlass on the bottle's liquid
/// mesh and runs a particle stream whenever liquid is leaving, so the potion
/// visibly trickles out when tipped. Forces a very slow pour rate.
///
/// Setup:
///   - LiquidGlass goes on the bottle's liquid mesh, as usual.
///   - This goes on the bottle root; drag the liquid mesh into Liquid.
///   - Stream is a particle system parked at the bottle's lip, pointing down,
///     Play On Awake OFF, Looping ON.
public class PotionPour : MonoBehaviour
{
    [Tooltip("The bottle's liquid. Auto-found in children if left empty.")]
    public LiquidGlass liquid;

    [Tooltip("The trickle at the bottle's lip. Play On Awake off, Looping on.")]
    public ParticleSystem stream;

    [Tooltip("Fill fraction drained per second at a full tip. The drink taps use " +
             "around 1.2 — keep this well under that for a slow trickle.")]
    [Range(0.01f, 0.5f)] public float pourRate = 0.06f;

    [Tooltip("Tilt below this is ignored, so the bottle doesn't dribble in your hand.")]
    public float tiltDeadzoneDeg = 25f;

    [Tooltip("Optional: the cork. While it's still a child of the bottle, nothing pours.")]
    public Transform cork;

    [Header("Stream look")]
    [Tooltip("Particles per second while pouring.")]
    public float emissionRate = 25f;

    [Tooltip("Seconds of no pouring before the stream shuts off. " +
             "A little slack keeps it from stuttering at the tipping point.")]
    public float streamLinger = 0.15f;

    float lastPourTime = -999f;
    bool streaming;

    void Awake()
    {
        if (liquid == null) liquid = GetComponentInChildren<LiquidGlass>();
        if (liquid == null)
        {
            Debug.LogError($"{name}: no LiquidGlass found — assign it.", this);
            enabled = false;
            return;
        }

        liquid.pourRate = pourRate;
        liquid.tiltDeadzoneDeg = tiltDeadzoneDeg;
        liquid.autoPourWhenTipped = false; // gated on the cork below

        liquid.OnPour += HandlePour;

        if (stream != null) stream.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void OnDestroy()
    {
        if (liquid != null) liquid.OnPour -= HandlePour;
    }

    void Update()
    {
        // Corked bottles don't pour. Cork is "out" once it's no longer our child.
        liquid.autoPourWhenTipped = IsUncorked;

        bool shouldStream = Time.time - lastPourTime < streamLinger;
        if (shouldStream != streaming) SetStream(shouldStream);
    }

    bool IsUncorked => cork == null || !cork.IsChildOf(transform);

    void HandlePour(LiquidGlass.Drink drink, float amount) => lastPourTime = Time.time;

    void SetStream(bool on)
    {
        streaming = on;
        if (stream == null) return;

        var emission = stream.emission;
        emission.rateOverTime = emissionRate;

        if (on) stream.Play();
        else stream.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}