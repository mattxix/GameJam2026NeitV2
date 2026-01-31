using UnityEngine;

public class MaskLogic : MonoBehaviour
{
    [Header("Guest & Mask Data")]
    public GameObject[] guestPrefabs;         // Character prefabs (guests)
    public GameObject[] maskTypeModels;       // Different mask prefabs/models
    public Material[] maskMaterials;          // Materials/colors for masks
    public GameObject[] maskAccessoryModels;  // Accessory prefabs (excluding 'None')
    public Transform GuestSpawnPoint;         // Spawn point for guests

    public Transform[] StandPoints;


    //Different stand points for guests
    public enum StandPoint
    { 
        StandPoint1,
        StandPoint2,
        StandPoint3,
        StandPoint4,
        StandPoint5
    }

    // Types of masks available
    public enum MaskType
    {
        M1,
        M2,
        M3,
        M4,
        M5
    }

    // Colors available for masks
    public enum MaskColor
    {
        Black,
        Burgundy,
        ForestGreen,
        Gold,
        Navy
    }

    // Accessories that can be added to masks
    public enum MaskAccessory
    {
        FeatherLeft,
        FeatherRight,
        FlowerLeft,
        FlowerRight,
        None  // No accessory
    }

    // Target mask properties (special mask to find)
    private MaskType targetType;
    private MaskColor targetColor;
    private MaskAccessory targetAccessory;

    // Civilian mask properties (random masks)
    private MaskType civilianType;
    private MaskColor civilianColor;
    private MaskAccessory civilianAccessory;

    void Start()
    {
        // Randomize target mask and civilian mask at the start
        RandomizeTargetMask();
        RandomizeCivilianMask();

        // Spawn one guest with either target or civilian mask
        SpawnGuestWithMask();
    }

    // Choose random mask details for the target
    void RandomizeTargetMask()
    {
        targetType = (MaskType)Random.Range(0, maskTypeModels.Length);
        targetColor = (MaskColor)Random.Range(0, maskMaterials.Length);
        targetAccessory = (MaskAccessory)Random.Range(0, System.Enum.GetValues(typeof(MaskAccessory)).Length);
    }

    // Choose random mask details for a civilian
    void RandomizeCivilianMask()
    {
        civilianType = (MaskType)Random.Range(0, maskTypeModels.Length);
        civilianColor = (MaskColor)Random.Range(0, maskMaterials.Length);
        civilianAccessory = (MaskAccessory)Random.Range(0, System.Enum.GetValues(typeof(MaskAccessory)).Length);
    }

    // Instantiate a guest and equip it with either the target or civilian mask
    void SpawnGuestWithMask()
    {

        int guestIndex = Random.Range(0, guestPrefabs.Length);
        GameObject guest = Instantiate(guestPrefabs[guestIndex], GuestSpawnPoint.position, GuestSpawnPoint.rotation);

        // Find the MaskAnchor transform on the guest to attach mask and accessory
        Transform maskAnchor = guest.transform.Find("MaskAnchor");
        if (maskAnchor == null)
        {
            Debug.LogWarning("MaskAnchor not found on guest prefab.");
            return;
        }

        // Remove any existing masks/accessories from previous spawn
        foreach (Transform child in maskAnchor)
            Destroy(child.gameObject);

        // 20% chance to spawn the target mask, otherwise spawn civilian mask
        if (Random.value < 0.5f)
        {
            AttachMask(maskAnchor, targetType, targetColor, targetAccessory);
            Debug.Log("Spawned Target Mask");
        }
        else
        {
            AttachMask(maskAnchor, civilianType, civilianColor, civilianAccessory);
            Debug.Log("Spawned Civilian Mask");
        }
    }

    // Attach mask and accessory to the guest's mask anchor
    void AttachMask(Transform maskAnchor, MaskType type, MaskColor color, MaskAccessory accessory)
    {
        int typeIndex = (int)type;

        // Validate mask type index
        if (typeIndex < 0 || typeIndex >= maskTypeModels.Length)
        {
            Debug.LogError("Invalid mask type index.");
            return;
        }

        // Instantiate the mask prefab as a child of maskAnchor
        GameObject mask = Instantiate(maskTypeModels[typeIndex], maskAnchor);
        mask.tag = "Mask";

        // Reset transform so it aligns properly
        mask.transform.localPosition = Vector3.zero;
        mask.transform.localRotation = Quaternion.identity;
        mask.transform.localScale = Vector3.one;

        // Apply the selected color/material to the mask
        Renderer renderer = mask.GetComponent<Renderer>();
        if (renderer != null && (int)color < maskMaterials.Length)
        {
            renderer.material = maskMaterials[(int)color];
        }

        // If accessory is 'None', skip adding accessory
        if (accessory == MaskAccessory.None)
        {
            return;
        }
        // Find the accessory anchor on the mask prefab, or fallback to mask root
        Transform accessoryAnchor = mask.transform.Find("AccessoryAnchor");
        if (accessoryAnchor == null)
        {
            accessoryAnchor = mask.transform;
        }

        int accIndex = (int)accessory;

        // Validate accessory index against the accessory models array
        if (accIndex < 0 || accIndex >= maskAccessoryModels.Length)
        { 
            return;
        }
        // Instantiate accessory and attach it
        GameObject accessoryObj = Instantiate(maskAccessoryModels[accIndex], accessoryAnchor);
        accessoryObj.tag = "Accessory";

        // Reset transform to align correctly
        accessoryObj.transform.localPosition = Vector3.zero;
        accessoryObj.transform.localRotation = Quaternion.identity;
        accessoryObj.transform.localScale = Vector3.one;
    }
}
