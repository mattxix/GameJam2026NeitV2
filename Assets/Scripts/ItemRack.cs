using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// General-purpose rack for any grabbable prefab. Once every slot has been empty for
// a moment, strays of that prefab are cleared and a full set respawns with a sparkle.
public class ItemRack : MonoBehaviour
{
    [Header("Item")]
    [Tooltip("Grabbable prefab this rack holds. Loose copies are matched by this prefab's name.")]
    [SerializeField] private GameObject itemPrefab;

    [Header("Grid")]
    [SerializeField, Min(1)] private int columns = 3;
    [SerializeField, Min(1)] private int rows = 2;
    [Tooltip("Distance between slots along this object's right (X) and forward (Z) axes.")]
    [SerializeField] private Vector2 spacing = new Vector2(0.07f, 0.07f);

    [Header("Detection")]
    [Tooltip("An item within this distance of a slot counts as racked.")]
    [SerializeField, Min(0.005f)] private float slotRadius = 0.035f;
    [Tooltip("Seconds between rack checks. Refills are rare, so this needn't run every frame.")]
    [SerializeField, Min(0.05f)] private float checkInterval = 0.25f;
    [Tooltip("The rack must stay empty this long before refilling, so returning an item cancels it.")]
    [SerializeField, Min(0f)] private float refillDelay = 1f;
    [Tooltip("Minimum seconds between refills, so no fault can put the rack into a respawn loop.")]
    [SerializeField, Min(0f)] private float refillCooldown = 3f;

    [Header("Cleanup")]
    [Tooltip("Items held in a hand or seated in a socket survive the refill, so garnished drinks aren't stripped.")]
    [SerializeField] private bool spareHeldItems = true;

    [Header("Effect")]
    [Tooltip("Additive URP particle material using the sparkle texture.")]
    [SerializeField] private Material effectMaterial;
    [SerializeField] private Color effectColor = new Color(1f, 0.82f, 0.4f, 1f);
    [SerializeField, Min(0)] private int spawnBurst = 16;
    [SerializeField, Min(0)] private int despawnBurst = 8;

    private readonly List<XRGrabInteractable> items = new List<XRGrabInteractable>();
    private ParticleSystem effect;
    private string prefabName;
    private float nextCheck;
    private float emptySince = -1f;
    private float lastRefill = float.NegativeInfinity;

    private int SlotCount => columns * rows;

    private void Start()
    {
        if (itemPrefab == null)
        {
            Debug.LogError("ItemRack: itemPrefab not assigned.", this);
            enabled = false;
            return;
        }

        prefabName = itemPrefab.name;

        BuildEffect();
        AdoptSceneItems();

        // Opening rack: hand-placed copies are cleared and the grid is filled cleanly.
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

    // "Lime.VR (3)" and "Lime.VR(Clone)" both reduce to "Lime.VR".
    private static string BaseName(string raw)
    {
        string name = raw;

        int clone = name.IndexOf("(Clone)", StringComparison.Ordinal);
        if (clone >= 0) name = name.Substring(0, clone);
        name = name.TrimEnd();

        // Unity's duplicate suffix, e.g. " (3)".
        if (name.EndsWith(")", StringComparison.Ordinal))
        {
            int open = name.LastIndexOf(" (", StringComparison.Ordinal);
            if (open >= 0 && int.TryParse(name.Substring(open + 2, name.Length - open - 3), out _))
                name = name.Substring(0, open);
        }

        return name.Trim();
    }

    // Hand-placed copies get tracked too, so strays of this type never escape cleanup.
    private void AdoptSceneItems()
    {
        var all = FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (BaseName(all[i].gameObject.name) == prefabName && !items.Contains(all[i]))
                items.Add(all[i]);
        }
    }

    private bool AnySlotOccupied()
    {
        // Served items are destroyed elsewhere, so drop dead references first.
        items.RemoveAll(item => item == null);

        float r2 = slotRadius * slotRadius;
        for (int s = 0; s < SlotCount; s++)
        {
            Vector3 slot = SlotPosition(s);
            for (int i = 0; i < items.Count; i++)
            {
                if ((items[i].transform.position - slot).sqrMagnitude <= r2) return true;
            }
        }
        return false;
    }

    private void Refill(bool withEffect)
    {
        lastRefill = Time.time;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            XRGrabInteractable item = items[i];
            if (item == null)
            {
                items.RemoveAt(i);
                continue;
            }

            if (spareHeldItems && item.isSelected) continue;

            if (withEffect) Burst(item.transform.position, despawnBurst);
            Destroy(item.gameObject);
            items.RemoveAt(i);
        }

        Quaternion rotation = transform.rotation * itemPrefab.transform.localRotation;
        for (int s = 0; s < SlotCount; s++)
        {
            Vector3 position = SlotPosition(s);
            GameObject spawned = Instantiate(itemPrefab, position, rotation);

            // Frozen until first grabbed, so a fresh rack can't knock itself over.
            // RackedGlass works for any grabbable, despite the name.
            spawned.AddComponent<RackedGlass>();

            var grab = spawned.GetComponent<XRGrabInteractable>();
            if (grab != null) items.Add(grab);

            if (withEffect) Burst(position, spawnBurst);
        }
    }

    // One pooled system emits at any position, so a refill costs no allocations.
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
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = effectColor;
        main.gravityModifier = -0.15f; // sparkles drift upward
        main.maxParticles = 500;

        // Bursts are emitted manually, so the automatic emitter stays off.
        var emission = effect.emission;
        emission.enabled = false;

        // Small flat ring around each slot, sized for toppings rather than glasses.
        var shape = effect.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.03f;
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
        else Debug.LogWarning("ItemRack: effectMaterial not assigned, sparkles will render pink.", this);

        effect.Play();
    }

    // Shows the slots in the Scene view so the grid can be lined up before playing.
    private void OnDrawGizmos()
    {
        if (columns < 1 || rows < 1) return;
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.9f);
        for (int s = 0; s < columns * rows; s++)
            Gizmos.DrawWireSphere(SlotPosition(s), slotRadius);
    }
}