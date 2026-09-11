using System;
using UnityEngine;

/// <summary>
/// Drives the Custom/DrinkLiquid shader. Put this on the liquid mesh
/// (a cylinder sitting inside the glass), not on the glass itself.
///
/// The liquid is modelled as a cylinder of radius rimRadius between
/// bottomOffset and topOffset along the glass axis. Each frame the horizontal
/// clip plane is solved so it encloses exactly fill * (full volume), whatever
/// the tilt — so tipping the glass never makes the drink appear to grow or
/// shrink. Pouring uses the same solve: liquid leaves when that plane sits
/// above the low edge of the rim.
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

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

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
    [Tooltip("Cap on how full the tap can make it. Lower this if a full glass " +
             "spills too easily when picked up — a real pint isn't filled to the brim.")]
    [Range(0.5f, 1f)] public float maxFill = 0.9f;

    [Header("Glass shape")]
    [Tooltip("INTERIOR radius of the glass in metres. This is real geometry now, " +
             "not a sensitivity dial — it drives both the volume calculation and " +
             "when tipping starts a pour. A pint glass is about 0.035.")]
    public float rimRadius = 0.035f;

    [Tooltip("Whose 'up' is the glass axis. Assign the glass ROOT. " +
             "Falls back to this object's up if empty.")]
    public Transform tiltReference;

    [Tooltip("Ignore the mesh bounds and use the two values below. Turn this on " +
             "when the liquid only appears across part of the 0-1 fill range.")]
    public bool useManualBounds = false;

    [Tooltip("Inside bottom of the glass, in metres along the axis from this object's pivot.")]
    public float manualBottomOffset = 0f;

    [Tooltip("Rim of the glass, in metres along the axis from this object's pivot.")]
    public float manualTopOffset = 0.175f;

    [Header("Wobble")]
    public float maxWobble = 0.05f;
    public float wobbleSpeed = 1.2f;
    public float recovery = 1.6f;

    [Header("Pouring")]
    public bool autoPourWhenTipped = true;
    [Tooltip("Fill fraction drained per second at a full tip.")]
    public float pourRate = 1.2f;
    [Tooltip("Ignore tilts smaller than this, so hand tremor doesn't dribble.")]
    public float tiltDeadzoneDeg = 3f;

    /// <summary>Fires every frame liquid leaves the glass. (drink, fill lost)</summary>
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

    // Axis extents from the pivot, in metres along the glass axis.
    float bottomOffset, topOffset;

    // Tilt of the glass axis: cos = dot(axis, world up), sin from that.
    float cosT = 1f, sinT = 0f;

    // Last solved clip plane, as a world-Y offset from the pivot.
    float planeHeight;

    Color liquidColor, surfaceColor, foamColor;

    Vector3 lastPos;
    Quaternion lastRot;
    float wobbleTargetX, wobbleTargetZ, wobbleTime;
    float wobbleX, wobbleZ;

    // ---------- Public API ----------

    public float Fill
    {
        get => fill;
        set
        {
            bool had = fill > 0f;
            fill = Mathf.Clamp(value, 0f, maxFill);
            Apply();
            if (had && fill <= 0f) OnEmptied?.Invoke();
        }
    }

    public Drink CurrentDrink => currentDrink;
    public bool IsEmpty => fill <= 0.001f;
    public bool IsFull => fill >= maxFill - 0.001f;

    /// <summary>World height of the liquid surface as last solved.</summary>
    public float SurfaceWorldY => transform.position.y + planeHeight;
    public float BottomWorldY => transform.position.y + bottomOffset * cosT;
    public float TopWorldY => transform.position.y + topOffset * cosT;

    public void SetDrink(Drink drink)
    {
        currentDrink = drink;
        int i = Mathf.Clamp((int)drink, 0, styles.Length - 1);
        liquidColor = styles[i].liquid;
        surfaceColor = styles[i].surface;
        foamColor = styles[i].foam;
        Apply();
    }

    /// <summary>Call from a tap. Adopts the drink if empty, blends colours if mixing.</summary>
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

    public void Empty() => Fill = 0f;

    // ---------- Lifecycle ----------

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
        var mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Bounds b = mf.sharedMesh.bounds;
            float s = transform.lossyScale.y;
            bottomOffset = b.min.y * s;
            topOffset = b.max.y * s;
        }

        if (useManualBounds)
        {
            bottomOffset = manualBottomOffset;
            topOffset = manualTopOffset;
        }

        if (topOffset <= bottomOffset)
            Debug.LogError($"{name}: topOffset ({topOffset:F4}) must be above bottomOffset " +
                           $"({bottomOffset:F4}). Fix the bounds.", this);
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

        wobbleTargetX = Mathf.Lerp(wobbleTargetX, 0f, dt * recovery);
        wobbleTargetZ = Mathf.Lerp(wobbleTargetZ, 0f, dt * recovery);

        Vector3 velocity = (transform.position - lastPos) / dt;

        Quaternion delta = transform.rotation * Quaternion.Inverse(lastRot);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        if (float.IsNaN(axis.x)) axis = Vector3.zero;
        Vector3 angularVelocity = axis * (angle * Mathf.Deg2Rad / dt);

        wobbleTargetX += Mathf.Clamp((velocity.x + angularVelocity.z * 0.2f) * maxWobble, -maxWobble, maxWobble);
        wobbleTargetZ += Mathf.Clamp((velocity.z - angularVelocity.x * 0.2f) * maxWobble, -maxWobble, maxWobble);
        wobbleTargetX = Mathf.Clamp(wobbleTargetX, -maxWobble, maxWobble);
        wobbleTargetZ = Mathf.Clamp(wobbleTargetZ, -maxWobble, maxWobble);

        float pulse = 2f * Mathf.PI * wobbleSpeed;
        wobbleX = wobbleTargetX * Mathf.Sin(pulse * wobbleTime);
        wobbleZ = wobbleTargetZ * Mathf.Cos(pulse * wobbleTime);
    }

    void UpdatePour(float dt)
    {
        if (IsEmpty) return;

        ComputeTilt();
        float tiltDeg = Mathf.Acos(Mathf.Clamp(cosT, -1f, 1f)) * Mathf.Rad2Deg;
        if (tiltDeg < tiltDeadzoneDeg) return;

        float plane = SolvePlaneHeight();

        // Lowest point of the rim circle, as a world-Y offset from the pivot.
        float lowestRim = topOffset * cosT - rimRadius * sinT;

        float overflow = plane - lowestRim;
        if (overflow <= 0f) return;

        // Floor the rate so the last dribble doesn't take forever.
        float severity = Mathf.Clamp(overflow / Mathf.Max(rimRadius, 1e-4f), 0.15f, 1f);
        float amount = Mathf.Min(fill, pourRate * severity * dt);

        fill -= amount;
        if (fill < 0.01f) fill = 0f;

        OnPour?.Invoke(currentDrink, amount);
        if (fill <= 0f) OnEmptied?.Invoke();
    }

    // ---------- Volume solve ----------

    void ComputeTilt()
    {
        Vector3 up = tiltReference != null ? tiltReference.up : transform.up;
        cosT = Mathf.Clamp(Vector3.Dot(up, Vector3.up), -1f, 1f);
        sinT = Mathf.Sqrt(Mathf.Max(0f, 1f - cosT * cosT));
    }

    /// <summary>Area of a disc of radius r on the side x &lt;= t.</summary>
    static float SegmentArea(float r, float t)
    {
        if (t <= -r) return 0f;
        if (t >= r) return Mathf.PI * r * r;
        return r * r * Mathf.Acos(-t / r) + t * Mathf.Sqrt(r * r - t * t);
    }

    /// <summary>Integral of SegmentArea from -r up to t.</summary>
    static float SegmentAreaIntegral(float r, float t)
    {
        if (t <= -r) return 0f;
        if (t >= r) return Mathf.PI * r * r * r + Mathf.PI * r * r * (t - r);
        float r2 = r * r;
        float q = r2 - t * t;
        float sq = Mathf.Sqrt(q);
        return r2 * t * Mathf.Acos(-t / r) + r2 * sq - q * sq / 3f;
    }

    /// <summary>
    /// Volume of the tilted cylinder that lies below a horizontal plane at
    /// world-Y offset h from the pivot. Exact for a cylinder; glasses that
    /// taper will be slightly off, which nobody will notice.
    /// </summary>
    float VolumeBelow(float h)
    {
        float H = topOffset - bottomOffset;
        float r = rimRadius;

        // Axis vertical (upright or inverted): plain height clamp.
        if (sinT < 1e-4f)
        {
            float len = cosT > 0f
                ? h / cosT - bottomOffset
                : H - (h / cosT - bottomOffset);
            return Mathf.PI * r * r * Mathf.Clamp(len, 0f, H);
        }

        // For a slice at axis position s, its centre sits at world-Y
        // (bottomOffset + s) * cosT. Points on that slice are below the plane
        // where their in-slice coordinate x <= t(s) = (h - centreY) / sinT.
        float t0 = (h - bottomOffset * cosT) / sinT;
        float t1 = (h - (bottomOffset + H) * cosT) / sinT;

        // Axis horizontal: every slice has the same chord.
        if (Mathf.Abs(cosT) < 1e-4f)
            return H * SegmentArea(r, t0);

        // t is linear in s with dt/ds = -cosT/sinT, so
        // V = ∫ A(t(s)) ds = (sinT / cosT) * (F(t0) - F(t1)).
        return (sinT / cosT) * (SegmentAreaIntegral(r, t0) - SegmentAreaIntegral(r, t1));
    }

    /// <summary>
    /// Finds the plane height enclosing fill * fullVolume at the current tilt.
    /// Volume is monotonic in h, so a bisection converges reliably.
    /// </summary>
    float SolvePlaneHeight()
    {
        float H = topOffset - bottomOffset;
        float r = rimRadius;
        float target = Mathf.Clamp01(fill) * Mathf.PI * r * r * H;

        float aLo = bottomOffset * cosT;
        float aHi = topOffset * cosT;
        float lo = Mathf.Min(aLo, aHi) - r * sinT;
        float hi = Mathf.Max(aLo, aHi) + r * sinT;

        for (int i = 0; i < 28; i++)
        {
            float mid = 0.5f * (lo + hi);
            if (VolumeBelow(mid) < target) lo = mid; else hi = mid;
        }
        return 0.5f * (lo + hi);
    }

    // ---------- Rendering ----------

    static Color ToShaderColor(Color c)
    {
        if (QualitySettings.activeColorSpace != ColorSpace.Linear) return c;
        Color l = c.linear;
        l.a = c.a;
        return l;
    }

    void Apply()
    {
        if (rend == null) return;

        rend.enabled = !IsEmpty;
        if (!rend.enabled) return;

        ComputeTilt();
        planeHeight = SolvePlaneHeight();

        rend.GetPropertyBlock(mpb);
        mpb.SetFloat(FillID, planeHeight);
        mpb.SetFloat(WobbleXID, wobbleX);
        mpb.SetFloat(WobbleZID, wobbleZ);
        mpb.SetColor(TintID, ToShaderColor(liquidColor));
        mpb.SetColor(TopID, ToShaderColor(surfaceColor));
        mpb.SetColor(FoamID, ToShaderColor(foamColor));
        rend.SetPropertyBlock(mpb);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;

        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            rend = GetComponent<MeshRenderer>();
            if (mpb == null) mpb = new MaterialPropertyBlock();
            Measure();
            SetDrink(currentDrink);
            Apply();
        };
    }
#endif
}