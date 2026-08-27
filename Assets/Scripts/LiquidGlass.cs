using System;
using UnityEngine;

/// <summary>
/// Drives the Custom/DrinkLiquid shader. Put this on the liquid mesh
/// (a cylinder sitting inside the glass), not on the glass itself.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class LiquidGlass : MonoBehaviour
{
    public enum Drink
    {
        OldHouseGin = 0,
        MidnightTonic = 1,
        CrimsonHighball = 2,
        EmeraldFizz = 3,
        StillCrystal = 4
    }

    [Serializable]
    public struct DrinkStyle
    {
        public string name;
        [ColorUsage(true, true)] public Color liquid;
        [ColorUsage(true, true)] public Color surface;
        [ColorUsage(true, true)] public Color foam;
    }

    /// <summary>Parses "RRGGBBAA" into a Color. Used only for the default presets below.</summary>
    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    /// <summary>Lightens a colour toward white and makes it opaque. Used for surface / foam tints.</summary>
    static Color Lighten(Color c, float t)
    {
        Color r = Color.Lerp(c, Color.white, t);
        r.a = 1f;
        return r;
    }

    [Header("Drink presets — index must match the Drink enum order")]
    public DrinkStyle[] styles = new DrinkStyle[]    
    {
        new DrinkStyle { name = "OldHouseGin",     liquid = Hex("AF72397F"), surface = Lighten(Hex("AF72397F"), 0.30f), foam = Lighten(Hex("AF72397F"), 0.65f) },
        new DrinkStyle { name = "MidnightTonic",   liquid = Hex("003AFF7F"), surface = Lighten(Hex("003AFF7F"), 0.30f), foam = Lighten(Hex("003AFF7F"), 0.65f) },
        new DrinkStyle { name = "CrimsonHighball", liquid = Hex("FF00007F"), surface = Lighten(Hex("FF00007F"), 0.30f), foam = Lighten(Hex("FF00007F"), 0.65f) },
        new DrinkStyle { name = "EmeraldFizz",     liquid = Hex("00FF157F"), surface = Lighten(Hex("00FF157F"), 0.30f), foam = Lighten(Hex("00FF157F"), 0.65f) },
        new DrinkStyle { name = "StillCrystal",    liquid = Hex("9CB6CC7F"), surface = Lighten(Hex("9CB6CC7F"), 0.30f), foam = Lighten(Hex("9CB6CC7F"), 0.65f) },
    };

    [Header("State")]
    [SerializeField] Drink currentDrink = Drink.OldHouseGin;
    [Range(0f, 1f)][SerializeField] float fill = 0f;
    [Tooltip("Stop short of the very top so the mesh's top cap never shows.")]
    [Range(0.8f, 1f)] public float maxFill = 0.95f;

    [Header("Wobble")]
    [Tooltip("How far the surface can tilt when sloshing.")]
    public float maxWobble = 0.05f;
    [Tooltip("Oscillations per second.")]
    public float wobbleSpeed = 1.2f;
    [Tooltip("How fast the slosh settles down.")]
    public float recovery = 1.6f;

    [Header("Pouring")]
    public bool autoPourWhenTipped = true;
    [Tooltip("Fill fraction drained per second at a full tip.")]
    public float pourRate = 0.9f;
    [Tooltip("Radius of the glass rim. Auto-measured on Awake if left at 0.")]
    public float rimRadius = 0f;

    /// <summary>Fires every frame liquid leaves the glass. (drink, amount of fill lost)</summary>
    public event Action<Drink, float> OnPour;
    /// <summary>Fires once when the glass runs dry.</summary>
    public event Action OnEmptied;

    static readonly int FillID = Shader.PropertyToID("_FillAmount");
    static readonly int WobbleXID = Shader.PropertyToID("_WobbleX");
    static readonly int WobbleZID = Shader.PropertyToID("_WobbleZ");
    static readonly int TintID = Shader.PropertyToID("_Tint");
    static readonly int TopID = Shader.PropertyToID("_TopColor");
    static readonly int FoamID = Shader.PropertyToID("_FoamColor");

    MeshRenderer rend;
    MaterialPropertyBlock mpb;

    // Mesh extents, in world units, measured from the pivot.
    float bottomOffset, topOffset, meshTopLocalY;

    // Runtime colours (separate from the presets so drinks can be mixed).
    Color liquidColor, surfaceColor, foamColor;

    Vector3 lastPos;
    Quaternion lastRot;
    float wobbleTargetX, wobbleTargetZ, wobbleTime;
    float CurrentWobbleX, CurrentWobbleZ;

    public float Fill
    {
        get => fill;
        set
        {
            bool wasFull = fill > 0f;
            fill = Mathf.Clamp(value, 0f, maxFill);
            Apply();
            if (wasFull && fill <= 0f) OnEmptied?.Invoke();
        }
    }

    public Drink CurrentDrink => currentDrink;
    public bool IsEmpty => fill <= 0.001f;
    public bool IsFull => fill >= maxFill - 0.001f;

    void Awake()
    {
        rend = GetComponent<MeshRenderer>();
        mpb = new MaterialPropertyBlock();
        Measure();
        SetDrink(currentDrink);
        lastPos = transform.position;
        lastRot = transform.rotation;
        Apply();
    }

    void Measure()
    {
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null) return;

        Bounds b = mesh.bounds;                    // local space
        Vector3 scale = transform.lossyScale;
        meshTopLocalY = b.max.y;
        bottomOffset = b.min.y * scale.y;
        topOffset = b.max.y * scale.y;

        if (rimRadius <= 0f)
            rimRadius = b.extents.x * Mathf.Max(scale.x, scale.z);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        UpdateWobble(dt);
        if (autoPourWhenTipped) UpdatePour(dt);
        Apply();

        lastPos = transform.position;
        lastRot = transform.rotation;
    }

    void UpdateWobble(float dt)
    {
        wobbleTime += dt;

        // Decay whatever slosh energy is left.
        wobbleTargetX = Mathf.Lerp(wobbleTargetX, 0f, dt * recovery);
        wobbleTargetZ = Mathf.Lerp(wobbleTargetZ, 0f, dt * recovery);

        Vector3 velocity = (transform.position - lastPos) / dt;

        // Stable angular velocity (no 0/360 wrap-around glitches).
        Quaternion delta = transform.rotation * Quaternion.Inverse(lastRot);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        if (float.IsNaN(axis.x)) axis = Vector3.zero;
        Vector3 angularVelocity = axis * (angle * Mathf.Deg2Rad / dt);

        // Linear motion along an axis, plus rotation about the perpendicular axis,
        // both push the surface the same direction.
        wobbleTargetX += Mathf.Clamp((velocity.x + angularVelocity.z * 0.2f) * maxWobble, -maxWobble, maxWobble);
        wobbleTargetZ += Mathf.Clamp((velocity.z - angularVelocity.x * 0.2f) * maxWobble, -maxWobble, maxWobble);
        wobbleTargetX = Mathf.Clamp(wobbleTargetX, -maxWobble, maxWobble);
        wobbleTargetZ = Mathf.Clamp(wobbleTargetZ, -maxWobble, maxWobble);

        float pulse = 2f * Mathf.PI * wobbleSpeed;
        CurrentWobbleX = wobbleTargetX * Mathf.Sin(pulse * wobbleTime);
        CurrentWobbleZ = wobbleTargetZ * Mathf.Cos(pulse * wobbleTime);
    }

    void UpdatePour(float dt)
    {
        if (IsEmpty) return;

        // Lowest point of the rim, in world space.
        float tilt = Vector3.Angle(transform.up, Vector3.up) * Mathf.Deg2Rad;
        Vector3 rimCenter = transform.TransformPoint(new Vector3(0f, meshTopLocalY, 0f));
        float lowestRimY = rimCenter.y - rimRadius * Mathf.Sin(tilt);

        // Height of the liquid surface, in world space.
        float surfaceY = transform.position.y + Mathf.Lerp(bottomOffset, topOffset, fill);

        float overflow = surfaceY - lowestRimY;
        if (overflow <= 0f) return;

        float height = Mathf.Max(topOffset - bottomOffset, 1e-4f);
        float severity = Mathf.Clamp01(overflow / height * 3f);
        float amount = Mathf.Min(fill, pourRate * severity * dt);

        fill -= amount;
        OnPour?.Invoke(currentDrink, amount);
        if (fill <= 0f) { fill = 0f; OnEmptied?.Invoke(); }
    }

    /// <summary>
    /// Colours set through a MaterialPropertyBlock are handed to the GPU untouched,
    /// unlike colours assigned in the material inspector. In a Linear-space project
    /// we have to do the sRGB conversion ourselves or everything renders washed out.
    /// </summary>
    static Color ToShaderColor(Color c)
    {
        if (QualitySettings.activeColorSpace != ColorSpace.Linear) return c;
        Color linear = c.linear;
        linear.a = c.a;   // only RGB is gamma-encoded; alpha passes through
        return linear;
    }

    void Apply()
    {
        if (rend == null) return;

        rend.enabled = !IsEmpty;
        if (!rend.enabled) return;

        rend.GetPropertyBlock(mpb);
        mpb.SetFloat(FillID, Mathf.Lerp(bottomOffset, topOffset, fill));
        mpb.SetFloat(WobbleXID, CurrentWobbleX);
        mpb.SetFloat(WobbleZID, CurrentWobbleZ);
        mpb.SetColor(TintID, ToShaderColor(liquidColor));
        mpb.SetColor(TopID, ToShaderColor(surfaceColor));
        mpb.SetColor(FoamID, ToShaderColor(foamColor));
        rend.SetPropertyBlock(mpb);
    }

    // ---------- Public API ----------

    public void SetDrink(Drink drink)
    {
        currentDrink = drink;
        int i = Mathf.Clamp((int)drink, 0, styles.Length - 1);
        liquidColor = styles[i].liquid;
        surfaceColor = styles[i].surface;
        foamColor = styles[i].foam;
        Apply();
    }

    /// <summary>Call this from a tap. Adopts the drink if empty, blends colours if mixing.</summary>
    public void FillFrom(Drink drink, float amount)
    {
        if (amount <= 0f) return;

        if (IsEmpty)
        {
            SetDrink(drink);
        }
        else if (drink != currentDrink)
        {
            int i = Mathf.Clamp((int)drink, 0, styles.Length - 1);
            float t = Mathf.Clamp01(amount / Mathf.Max(fill + amount, 1e-4f));
            liquidColor = Color.Lerp(liquidColor, styles[i].liquid, t);
            surfaceColor = Color.Lerp(surfaceColor, styles[i].surface, t);
            foamColor = Color.Lerp(foamColor, styles[i].foam, t);
        }

        Fill = fill + amount;
    }

    public void Empty()
    {
        Fill = 0f;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            rend = GetComponent<MeshRenderer>();
            if (mpb == null) mpb = new MaterialPropertyBlock();
            Measure();
            SetDrink(currentDrink);
            Apply();
        }
    }
#endif
}