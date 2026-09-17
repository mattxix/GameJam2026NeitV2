using UnityEngine;

// Marks a particle stream as poison rather than a drink. GlassCatcher checks
// for this so poison doses the glass instead of adding fill.
public class PoisonSource : MonoBehaviour
{
    [Tooltip("The stream. Left empty, it looks for one on this object or its children.")]
    public ParticleSystem stream;

    static readonly System.Collections.Generic.Dictionary<ParticleSystem, PoisonSource> byStream = new();

    public static PoisonSource ForStream(ParticleSystem ps)
        => ps != null && byStream.TryGetValue(ps, out var s) ? s : null;

    void Awake()
    {
        if (stream == null) stream = GetComponentInChildren<ParticleSystem>();
        if (stream != null) byStream[stream] = this;
    }

    void OnDestroy()
    {
        if (stream != null) byStream.Remove(stream);
    }
}