using UnityEngine;

/// <summary>
/// Moves the catcher disc up and down inside the glass to follow the fill level,
/// so the drink stream dies at the liquid surface instead of at the rim.
///
/// Works entirely in LOCAL space, so the disc rides with the glass no matter how
/// the glass is moved, rotated, or reparented.
///
/// GlassCatcher still lives on the glass ROOT — Unity routes particle collision
/// messages to the Rigidbody, and moving this collider doesn't change that.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LiquidSurfaceCollider : MonoBehaviour
{
    [Tooltip("The liquid this tracks. Auto-found on the glass root if left empty.")]
    public LiquidGlass liquid;

    [Header("Travel (local Y, relative to this object's parent)")]
    [Tooltip("Where the disc sits when the glass is empty — the inside bottom.")]
    public float emptyLocalY = 0f;

    [Tooltip("Where the disc sits when the glass is full — just under the rim.")]
    public float fullLocalY = 0.15f;

    [Header("Tuning")]
    [Tooltip("Don't touch the transform until the target has moved this far. " +
             "Moving a collider inside a Rigidbody's compound shape costs PhysX " +
             "work, so this keeps it to a few updates per pour, not one per frame.")]
    public float minStep = 0.0015f;

    [Tooltip("0 = snap instantly. Higher values ease the disc toward the target, " +
             "which looks better if fill arrives in visible chunks.")]
    public float smoothing = 0f;

    float lastY = float.NaN;
    Vector3 restLocalPos;

    void Awake()
    {
        if (liquid == null)
        {
            Transform root = transform.parent != null ? transform.parent : transform;
            liquid = root.GetComponentInChildren<LiquidGlass>();
        }

        if (liquid == null)
            Debug.LogError($"{name}: no LiquidGlass found — assign it manually.", this);

        if (GetComponent<Collider>().isTrigger)
            Debug.LogWarning($"{name}: collider is a trigger. Particles ignore triggers " +
                             "in World collision mode — uncheck Is Trigger.", this);

        // X and Z are whatever you authored in the scene; only Y is driven.
        restLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (liquid == null) return;

        float targetY = Mathf.Lerp(emptyLocalY, fullLocalY, Mathf.Clamp01(liquid.Fill));

        if (smoothing > 0f && !float.IsNaN(lastY))
            targetY = Mathf.Lerp(lastY, targetY, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

        if (float.IsNaN(lastY) || Mathf.Abs(targetY - lastY) > minStep)
        {
            transform.localPosition = new Vector3(restLocalPos.x, targetY, restLocalPos.z);
            lastY = targetY;
        }
    }

    void OnDisable()
    {
        lastY = float.NaN;
    }

    // ---------- Editor helpers ----------

    [ContextMenu("Capture Current Height As Empty")]
    void CaptureEmpty()
    {
        emptyLocalY = transform.localPosition.y;
    }

    [ContextMenu("Capture Current Height As Full")]
    void CaptureFull()
    {
        fullLocalY = transform.localPosition.y;
    }

    void OnDrawGizmosSelected()
    {
        Transform parent = transform.parent != null ? transform.parent : transform;
        Vector3 basePos = Application.isPlaying ? restLocalPos : transform.localPosition;

        Vector3 low = parent.TransformPoint(new Vector3(basePos.x, emptyLocalY, basePos.z));
        Vector3 high = parent.TransformPoint(new Vector3(basePos.x, fullLocalY, basePos.z));

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(low, high);
        Gizmos.DrawWireSphere(low, 0.01f);
        Gizmos.DrawWireSphere(high, 0.01f);
    }
}