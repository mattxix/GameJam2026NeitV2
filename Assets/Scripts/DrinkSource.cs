using UnityEngine;

/// <summary>
/// Put this on the tap (or directly on its particle system) so a GlassCatcher
/// knows which drink is landing in the glass.
/// </summary>
public class DrinkSource : MonoBehaviour
{
    public LiquidGlass.Drink drink = LiquidGlass.Drink.OldHouseGin;

    [Tooltip("The stream. Left empty, it looks for one on this object or its children.")]
    public ParticleSystem stream;

    public bool IsPouring { get; private set; }

    void Awake()
    {
        
        SetPouring(false);
    }

    public void SetPouring(bool on)
    {
        IsPouring = on;
        if (stream == null) return;

        if (on) stream.Play();
        else stream.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    /// <summary>Hook this to the tap handle's XRI Activated / Select event.</summary>
    public void Toggle() => SetPouring(!IsPouring);
}