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

public class GlassCatcher : MonoBehaviour
{
    [Tooltip("The liquid mesh this catcher feeds. Auto-found in parents if left empty.")]
    public LiquidGlass liquid;

    [Tooltip("Fill fraction added per particle caught. " +
             "Rough calibration: 1 / (emission rate * seconds to fill).")]
    public float fillPerParticle = 0.005f;

    [Tooltip("Fill fraction added per second while the stream is landing. " +
            "Used when the per-particle event list comes back empty.")]
    public float fillRatePerSecond = 0.25f;

    [Tooltip("Safety cap so a burst of particles in one frame can't jump the level.")]
    public float maxFillPerFrame = 0.08f;

    /// <summary>Fires when a particle lands. (drink, amount added) — hook splash FX here.</summary>
    public event System.Action<LiquidGlass.Drink, float> OnCaught;

    readonly List<ParticleCollisionEvent> events = new List<ParticleCollisionEvent>();

    void Awake()
    {
        if (liquid == null) liquid = GetComponentInChildren<LiquidGlass>();

        if (liquid == null)
            Debug.LogError($"{name}: no LiquidGlass found — assign it manually.", this);
    }

    void OnParticleCollision(GameObject other)
    {
        
        if (liquid == null || liquid.IsFull) return;

        var ps = other.GetComponent<ParticleSystem>();
        if (ps == null) return;

        // How many particles from this system hit us this frame.
        int count = ParticlePhysicsExtensions.GetCollisionEvents(ps, gameObject, events);
        //Debug.Log($"[Catcher] count={count} fill={liquid.Fill:F3}");

        var source = other.GetComponentInParent<DrinkSource>();
        LiquidGlass.Drink drink = source != null ? source.drink : liquid.CurrentDrink;

        float amount = count > 0
            ? Mathf.Min(count * fillPerParticle, maxFillPerFrame)
            : fillRatePerSecond * Time.deltaTime;

        //Debug
        //for (int i = 0; i < count; i++)
        //{
        //    Vector3 p = events[i].intersection;
        //    Collider hit = events[i].colliderComponent as Collider;
        //    Debug.Log($"[Catcher] hit '{(hit ? hit.name : "null")}' " +
        //              $"layer '{(hit ? LayerMask.LayerToName(hit.gameObject.layer) : "?")}' " +
        //              $"at y={p.y:F3}  (surface y={liquid.SurfaceWorldY:F3}, " +
        //              $"bottom={liquid.BottomWorldY:F3}, top={liquid.TopWorldY:F3})");
        //    Debug.DrawRay(p, Vector3.up * 0.05f, Color.red, 2f);
        //}
        liquid.FillFrom(drink, amount);
        OnCaught?.Invoke(drink, amount);
    }
}
    

