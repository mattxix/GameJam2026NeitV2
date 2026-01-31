using JetBrains.Annotations;
using UnityEngine;

public class MaskLogic : MonoBehaviour
{
    // Guest Prefabs Array
    public GameObject[] guestPrefabs;

    // Mask Type Array (Models)
    public GameObject[] maskTypeModels;

    // Mask Color Array (Materials)
    public Material[] maskMaterials;

    // Mask Accessory Array (Models)
    public GameObject[] maskAccessoryModels;

    // Enums for selection logic
    public enum MaskType { M1, M2, M3, M4, M5 }
    public enum MaskColor { Red, Green, Blue, Yellow, Purple }
    public enum MaskAccessory { FlowerLeft, FlowerRight, None }

    // Selected attributes for Target and Civilian
    private MaskType tSelectedType;
    private MaskColor tSelectedColor;
    private MaskAccessory tSelectedAccessory;

    private MaskType cSelectedType;
    private MaskColor cSelectedColor;
    private MaskAccessory cSelectedAccessory;

    // Poisoned flag
    public bool targetPoisoned = false;

    // Parent objects for each mask (assign in Inspector)
    public Transform targetMaskRoot;
    public Transform civilianMaskRoot;

    void Start()
    {
        RandomizeTargetMask();
        RandomizeCivilianMask();
        EnsureMasksAreDifferent();
        SetupMask(targetMaskRoot, tSelectedType, tSelectedColor, tSelectedAccessory);
        SetupMask(civilianMaskRoot, cSelectedType, cSelectedColor, cSelectedAccessory);
        SpawnGuest();
    }


    //*------------------------Mask-Handling------------------------*//
    void RandomizeTargetMask()
    {
        tSelectedType = (MaskType)Random.Range(0, maskTypeModels.Length);
        tSelectedColor = (MaskColor)Random.Range(0, maskMaterials.Length);
        tSelectedAccessory = (MaskAccessory)Random.Range(0, maskAccessoryModels.Length);
    }

    void RandomizeCivilianMask()
    {
        cSelectedType = (MaskType)Random.Range(0, maskTypeModels.Length);
        cSelectedColor = (MaskColor)Random.Range(0, maskMaterials.Length);
        cSelectedAccessory = (MaskAccessory)Random.Range(0, maskAccessoryModels.Length);
    }

    void EnsureMasksAreDifferent()
    {
        // If all three attributes match, change civilian accessory
        if (tSelectedType == cSelectedType &&
            tSelectedColor == cSelectedColor &&
            tSelectedAccessory == cSelectedAccessory)
        {
            int accCount = maskAccessoryModels.Length;
            cSelectedAccessory = (MaskAccessory)(((int)cSelectedAccessory + 1) % accCount);
        }
    }

    void SetupMask(Transform maskRoot, MaskType type, MaskColor color, MaskAccessory accessory)
    {
        // Remove all children from maskRoot
        foreach (Transform child in maskRoot)
        {
            Destroy(child.gameObject);
        }

        // Instantiate and set up mask type (model)
        int typeIndex = (int)type;
        if (typeIndex >= 0 && typeIndex < maskTypeModels.Length)
        {
            GameObject maskModel = Instantiate(maskTypeModels[typeIndex], maskRoot);
            // Apply material
            var renderer = maskModel.GetComponent<Renderer>();
            if (renderer != null && (int)color < maskMaterials.Length)
            {
                renderer.material = maskMaterials[(int)color];
            }
            // Instantiate accessory if not None
            int accIndex = (int)accessory;
            if (accessory != MaskAccessory.None && accIndex >= 0 && accIndex < maskAccessoryModels.Length)
            {
                Instantiate(maskAccessoryModels[accIndex], maskModel.transform);
            }
        }
    }


    public void RemakeTargetIfPoisoned()
    {
        if (targetPoisoned)
        {
            // Remove all children from targetMaskRoot (mask and guest)
            foreach (Transform child in targetMaskRoot)
            {
                Destroy(child.gameObject);
            }

            // Randomize new target mask
            RandomizeTargetMask();
            EnsureMasksAreDifferent();
            SetupMask(targetMaskRoot, tSelectedType, tSelectedColor, tSelectedAccessory);
            SpawnGuest();

            // Reset poison flag
            targetPoisoned = false;
        }
    }

    //*------------------------Guest-Spawning------------------------*//
    void SpawnGuest()
    {
        if (guestPrefabs == null || guestPrefabs.Length == 0 || targetMaskRoot == null)
            return;

        if (UnityEditor.EditorUtility.IsPersistent(targetMaskRoot.gameObject))
        {
            Debug.LogError("targetMaskRoot must be a scene object, not a prefab asset.");
            return;
        }

        // Randomly select a type of guest
        int index = Random.Range(0, guestPrefabs.Length);
        GameObject prefab = guestPrefabs[index];

        // Instantiate as a child of targetMaskRoot
        GameObject guest = Instantiate(prefab, targetMaskRoot);

        // Optionally reset local position/rotation
        guest.transform.localPosition = Vector3.zero;
        guest.transform.localRotation = Quaternion.identity;
    }

}


