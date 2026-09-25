using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Keeps a grid rack of glasses. Once every slot has been empty for a moment,
// stray glasses are cleared and a full rack respawns with a sparkle burst.
public class GlassRack : MonoBehaviour
{
    [Header("Glass")]
    [SerializeField] private GameObject glassPrefab;

    [Header("Grid")]
    [SerializeField, Min(1)] private int columns = 4;
    [SerializeField, Min(1)] private int rows = 3;
    [Tooltip("Distance between slots along this object's right (X) and forward (Z) axes.")]
    [SerializeField] private Vector2 spacing = new Vector2(0.12f, 0.12f);

    [Header("Detection")]
    [Tooltip("A glass within this distance of a slot counts as racked.")]
    [SerializeField, Min(0.01f)] private float slotRadius = 0.06f;
    [Tooltip("Seconds between rack checks. Refills are rare, so this needn't run every frame.")]
    [SerializeField, Min(0.05f)] private float checkInterval = 0.25f;
    [Tooltip("The rack must stay empty this long before refilling, so returning a glass cancels it.")]
    [SerializeField, Min(0f)] private float refillDelay = 1f;
    [Tooltip("Minimum seconds between refills, so no fault can put the rack into a respawn loop.")]
    [SerializeField, Min(0f)] private float refillCooldown = 3f;

    [Header("Cleanup")]
    [Tooltip("Glasses held in a hand or seated in a socket survive the refill, so in-progress drinks aren't lost.")]
    [SerializeField] private bool spareHeldGlasses = true;

    [Header("Effect")]
    [Tooltip("Additive URP particle material using the sparkle texture.")]
    [SerializeField] private Material effectMaterial;
    [SerializeField] private Color effectColor = new Color(1f, 0.82f, 0.4f, 1f);
    [SerializeField, Min(0)] private int spawnBurst = 30;
    [SerializeField, Min(0)] private int despawnBurst = 12;

    private readonly List<XRGrabInteractable> glasses = new List<XRGrabInteractable>();
    private ParticleSystem effect;
    private float nextCheck;
    private float emptySince = -1f;
    private float lastRefill = float.NegativeInfinity;

    private int SlotCount => columns * rows;

    private void Start()
    {
        if (glassPrefab == null)
        {
            Debug.LogError("GlassRack: glassPrefab not assigned.", this);
            enabled = false;
            return;
        }

        BuildEffect();
        AdoptSceneGlasses();

        // Opening rack: any hand-placed glasses are cleared and the grid is filled cleanly.
        Refill(false);
    }

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + checkInterval;

        if (AnySlotOccupied())
        {
            emptySince = -1f;
            return;
        }

        if (emptySince < 0f) emptySince = Time.time;
        if (Time.time - emptySince < refillDelay) return;
        if (Time.time - lastRefill < refillCooldown) return;

        emptySince = -1f;
        Refill(true);
    }

    // Slots are centred on this object, so the rack can be positioned by its middle.
    private Vector3 SlotPosition(int index)
    {
        int col = index % columns;
        int row = index / columns;
        float x = (col - (columns - 1) * 0.5f) * spacing.x;
        float z = (row - (rows - 1) * 0.5f) * spacing.y;
        return transform.position + transform.right * x + transform.forward * z;
    }

    // Glasses placed by hand in the scene get tracked too, so strays never escape cleanup.
    private void AdoptSceneGlasses()
    {
        var existing = FindObjectsByType<DrinkProperties>(FindObjectsSortMode.None);
        for (int i = 0; i < existing.Length; i++)
        {
            var grab = existing[i].GetComponent<XRGrabInteractable>();
            if (grab != null && !glasses.Contains(grab)) glasses.Add(grab);
        }
    }

    private bool AnySlotOccupied()
    {
        // Served glasses are destroyed elsewhere, so drop dead references first.
        glasses.RemoveAll(g => g == null);

        float r2 = slotRadius * slotRadius;
        for (int s = 0; s < SlotCount; s++)
        {
            Vector3 slot = SlotPosition(s);
            for (int g = 0; g < glasses.Count; g++)
            {
                if ((glasses[g].transform.position - slot).sqrMagnitude <= r2) return true;
            }
        }
        return false;
    }

    private void Refill(bool withEffect)
    {
        lastRefill = Time.time;

        for (int i = glasses.Count - 1; i >= 0; i--)
        {
            XRGrabInteractable glass = glasses[i];
            if (glass == null)
            {
                glasses.RemoveAt(i);
                continue;
            }

            if (spareHeldGlasses && glass.isSelected) continue;

            if (withEffect) Burst(glass.transform.position, despawnBurst);
            Destroy(glass.gameObject);
            glasses.RemoveAt(i);
        }

        Quaternion rotation = transform.rotation * glassPrefab.transform.localRotation;
        for (int s = 0; s < SlotCount; s++)
        {
            Vector3 position = SlotPosition(s);
            GameObject spawned = Instantiate(glassPrefab, position, rotation);

            // Frozen until first grabbed, so a fresh rack can't knock itself over.
            spawned.AddComponent<RackedGlass>();

            var grab = spawned.GetComponent<XRGrabInteractable>();
            if (grab != null) glasses.Add(grab);

            if (withEffect) Burst(position, spawnBurst);
        }
    }

    // One pooled system emits at any position, so a 12-glass refill costs no allocations.
    private void Burst(Vector3 position, int count)
    {
        if (effect == null || count <= 0) return;
        var emit = new ParticleSystem.EmitParams
        {
            position = position,
            applyShapeToPosition = true
        };
        effect.Emit(emit, count);
    }

    private void BuildEffect()
    {
        var go = new GameObject("RackRespawnEffect");
        go.transform.SetParent(transform, false);

        effect = go.AddComponent<ParticleSystem>();
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = effect.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = effectColor;
        main.gravityModifier = -0.15f; // sparkles drift upward
        main.maxParticles = 1000;

        // Bursts are emitted manually, so the automatic emitter stays off.
        var emission = effect.emission;
        emission.enabled = false;

        // Flat ring around each slot, so sparkles spread outward then rise.
        var shape = effect.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        var drag = effect.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.drag = 2f;

        var color = effect.colorOverLifetime;
        color.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;

        var size = effect.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var spin = effect.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (effectMaterial != null) renderer.sharedMaterial = effectMaterial;
        else Debug.LogWarning("GlassRack: effectMaterial not assigned, sparkles will render pink.", this);

        effect.Play();
    }

    // Shows the slots in the Scene view so the grid can be lined up before playing.
    private void OnDrawGizmos()
    {
        if (columns < 1 || rows < 1) return;
        Gizmos.color = new Color(1f, 0.82f, 0.4f, 0.9f);
        for (int s = 0; s < columns * rows; s++)
            Gizmos.DrawWireSphere(SlotPosition(s), slotRadius);
    }
}