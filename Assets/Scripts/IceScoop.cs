using System.Collections.Generic;
using UnityEngine;

// Holds an integer ice count and shows pooled visual cubes.
// Fills when dipped in the bin, pours into a tilted glass.
[DisallowMultipleComponent]
public class IceScoop : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject iceCubePrefab;
    [SerializeField] private Transform iceAnchor;
    [SerializeField] private float cubeSpacing = 0.035f;

    [Header("Capacity")]
    [SerializeField, Min(1)] private int capacity = 3;

    [Header("Pouring")]
    [SerializeField, Range(10f, 90f)] private float pourAngle = 55f;
    [SerializeField, Min(0.05f)] private float pourInterval = 0.25f;

    [Header("Detection")]
    [SerializeField] private string iceBinTag = "IceBin";

    private readonly List<GameObject> pool = new List<GameObject>();
    private readonly List<GlassIceReceiver> nearbyGlasses = new List<GlassIceReceiver>();
    private int currentIce;
    private float nextPourTime;

    public int CurrentIce => currentIce;
    public int Capacity => capacity;

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
            Debug.LogError("IceScoop: iceCubePrefab not assigned.", this);
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

    // Stacks cubes in a shallow spiral so they read as a pile.
    private Vector3 SlotPosition(int index)
    {
        float angle = index * 2.399963f; // golden angle
        float radius = cubeSpacing * 0.5f * Mathf.Sqrt(index);
        return new Vector3(Mathf.Cos(angle) * radius, index * cubeSpacing * 0.35f, Mathf.Sin(angle) * radius);
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            bool shouldShow = i < currentIce;
            if (pool[i].activeSelf != shouldShow) pool[i].SetActive(shouldShow);
        }
    }

    public void Fill()
    {
        if (currentIce == capacity) return;
        currentIce = capacity;
        RefreshVisuals();
    }

    public bool TryRemoveOne()
    {
        if (currentIce <= 0) return false;
        currentIce--;
        RefreshVisuals();
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(iceBinTag))
        {
            Fill();
            return;
        }

        if (other.TryGetComponent(out GlassIceReceiver glass) && !nearbyGlasses.Contains(glass))
            nearbyGlasses.Add(glass);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out GlassIceReceiver glass))
            nearbyGlasses.Remove(glass);
    }

    private void Update()
    {
        if (currentIce <= 0 || nearbyGlasses.Count == 0) return;
        if (Time.time < nextPourTime) return;
        if (Vector3.Angle(transform.up, Vector3.up) < pourAngle) return;

        for (int i = nearbyGlasses.Count - 1; i >= 0; i--)
        {
            GlassIceReceiver glass = nearbyGlasses[i];
            if (glass == null)
            {
                nearbyGlasses.RemoveAt(i);
                continue;
            }

            if (glass.TryAddOne() && TryRemoveOne())
            {
                nextPourTime = Time.time + pourInterval;
                return;
            }
        }
    }
}