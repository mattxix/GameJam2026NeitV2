using UnityEngine;

public class TapFlow : MonoBehaviour
{
    [SerializeField] private ParticleSystem stream;
    [SerializeField] private float maxRate = 10f;
    [SerializeField] private AnimationCurve flowCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.MainModule main;

    private void Awake()
    {
        emission = stream.emission;
    }

    public void Start()
    {
        emission.rateOverTime = 0;
    }

    // Wire this to onValueChanged in the Inspector.
    public void SetFlow(float value)
    {
        float rate = maxRate * flowCurve.Evaluate(value);
        emission.rateOverTime = rate;

        if (rate > 0.01f)
        {
            if (!stream.isPlaying) stream.Play();
        }
        else if (stream.isPlaying)
        {
            stream.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}