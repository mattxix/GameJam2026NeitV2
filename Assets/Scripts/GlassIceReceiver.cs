using System.Collections.Generic;
using UnityEngine;

// Tracks how much ice a glass holds and renders pooled cubes at fixed slots.
[DisallowMultipleComponent]
public class GlassIceReceiver : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject iceCubePrefab;
    [SerializeField] private Transform iceAnchor;
    [SerializeField] private float cubeSpacing = 0.03f;
    [SerializeField] private float fillRadius = 0.022f;

    [Header("Capacity")]
    [SerializeField, Min(1)] private int capacity = 4;

    [Header("Spilling")]
    [SerializeField, Range(90f, 180f)] private float spillAngle = 120f;
    [SerializeField, Min(0.02f)] private float spillInterval = 0.12f;

    private readonly List<GameObject> pool = new List<GameObject>();
    private int currentIce;
    private float nextSpillTime;

    public int CurrentIce => currentIce;
    public int Capacity => capacity;
    public bool IsFull => currentIce >= capacity;

    private void Awake()
    {
        if (iceAnchor == null) iceAnchor = transform;
        BuildPool();
        RefreshVisuals();
    }

    private void BuildPool()
    {
        if (iceCubePrefab == null)
        {
            Debug.LogError("GlassIceReceiver: iceCubePrefab not assigned.", this);
            return;
        }

        for (int i = 0; i < capacity; i++)
        {
            GameObject cube = Instantiate(iceCubePrefab, iceAnchor);
            cube.transform.localPosition = SlotPosition(i);
            cube.transform.localRotation = Random.rotation;
            cube.SetActive(false);
            pool.Add(cube);
        }
    }

    // Fills upward in a loose spiral so the glass looks progressively packed.
    private Vector3 SlotPosition(int index)
    {
        float angle = index * 2.399963f; // golden angle
        float radius = fillRadius * Mathf.Sqrt((float)index / Mathf.Max(1, capacity));
        return new Vector3(Mathf.Cos(angle) * radius, index * cubeSpacing, Mathf.Sin(angle) * radius);
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            bool shouldShow = i < currentIce;
            if (pool[i].activeSelf != shouldShow) pool[i].SetActive(shouldShow);
        }
    }

    // Drops one cube when the glass is tipped far past horizontal.
    private void Update()
    {
        if (currentIce <= 0) return;
        if (Time.time < nextSpillTime) return;
        if (Vector3.Angle(transform.up, Vector3.up) < spillAngle) return;

        TryRemoveOne();
        nextSpillTime = Time.time + spillInterval;
    }

    public bool TryAddOne()
    {
        if (IsFull) return false;
        currentIce++;
        RefreshVisuals();
        return true;
    }

    public bool TryRemoveOne()
    {
        if (currentIce <= 0) return false;
        currentIce--;
        RefreshVisuals();
        return true;
    }

    public void Clear()
    {
        if (currentIce == 0) return;
        currentIce = 0;
        RefreshVisuals();
    }
}