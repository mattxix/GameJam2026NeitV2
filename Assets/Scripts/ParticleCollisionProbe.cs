using UnityEngine;

public class ParticleCollisionProbe : MonoBehaviour
{
    void OnParticleCollision(GameObject other)
    {
        //Debug.Log($"[Probe] hit {other.name} on layer '{LayerMask.LayerToName(other.layer)}'");
    }
    void Start()
    {
        var col = GetComponent<ParticleSystem>().collision;
        //Debug.Log($"[Probe] enabled={col.enabled} type={col.type} quality={col.quality} " +
        //          $"sendMessages={col.sendCollisionMessages} dynamic={col.enableDynamicColliders} " +
        //          $"mask={col.collidesWith.value}");
    }
}