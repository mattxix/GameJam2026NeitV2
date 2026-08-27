using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sits on a small non-trigger collider inside the mouth of the glass.
/// Every drink particle that lands here adds fill and is killed by the
/// particle system's Lifetime Loss setting.
///
/// Setup on the GLASS side:
///   - Child GameObject with a Box/Sphere collider spanning the glass mouth.
///   - Collider must NOT be a trigger (particles ignore triggers in World mode).
///   - Put it on a dedicated layer, e.g. "DrinkCatcher", and clear that layer's
///     row in Edit > Project Settings > Physics so it collides with nothing.
///
/// Setup on the TAP's particle system:
///   - Collision module ON, Type: World, Mode: 3D
///   - Collision Quality: High          (required for collision messages)
///   - Send Collision Messages: CHECKED (nothing works without this)
///   - Lifetime Loss: 1                 (particle dies on contact)
///   - Collides With: only the DrinkCatcher layer
/// </summary>
[RequireComponent(typeof(Collider))]
public class GlassCatcher : MonoBehaviour
{
    [Tooltip("The liquid mesh this catcher feeds. Auto-found in parents if left empty.")]
    public LiquidGlass liquid;

    [Tooltip("Fill fraction added per particle caught. " +
             "Rough calibration: 1 / (emission rate * seconds to fill).")]
    public float fillPerParticle = 0.005f;

    [Tooltip("Safety cap so a burst of particles in one frame can't jump the level.")]
    public float maxFillPerFrame = 0.08f;

    /// <summary>Fires when a particle lands. (drink, amount added) — hook splash FX here.</summary>
    public event System.Action<LiquidGlass.Drink, float> OnCaught;

    readonly List<ParticleCollisionEvent> events = new List<ParticleCollisionEvent>();

    void Awake()
    {
        if (liquid == null) liquid = GetComponentInParent<LiquidGlass>();

        var col = GetComponent<Collider>();
        if (col.isTrigger)
            Debug.LogWarning($"{name}: catcher collider is a trigger. " +
                             "Particles ignore triggers — uncheck Is Trigger.", this);
    }

    void OnParticleCollision(GameObject other)
    {
        if (liquid == null || liquid.IsFull) return;

        var ps = other.GetComponent<ParticleSystem>();
        if (ps == null) return;

        // How many particles from this system hit us this frame.
        int count = ParticlePhysicsExtensions.GetCollisionEvents(ps, gameObject, events);
        if (count <= 0) return;

        // The tap tells us which drink it is; fall back to whatever is in the glass.
        var source = other.GetComponentInParent<DrinkSource>();
        LiquidGlass.Drink drink = source != null ? source.drink : liquid.CurrentDrink;

        float amount = Mathf.Min(count * fillPerParticle, maxFillPerFrame);
        liquid.FillFrom(drink, amount);
        OnCaught?.Invoke(drink, amount);
    }
}
