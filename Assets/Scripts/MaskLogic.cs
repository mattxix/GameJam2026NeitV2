using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random; // Alias Random to UnityEngine.Random


public class MaskLogic : MonoBehaviour
{
    [Header("Guest & Mask Data")]
    public GameObject[] guestPrefabs;         // Character prefabs (guests)
    //public GameObject[] maskTypeModels;       // Different mask prefabs/models
    public Material[] maskMaterials;          // Materials/colors for masks
    //public GameObject[] maskAccessoryModels;  // Accessory prefabs (excluding 'None')
    public Transform GuestSpawnPoint;         // Spawn point for guests

    [Header("Spawning")]
    [Tooltip("How many guests can be at the bar at once. Must not exceed the number of napkins/chairs.")]
    [SerializeField, Min(1)] private int maxGuests = 5;
    [SerializeField] private float minSpawnDelay = 5f;
    [SerializeField] private float maxSpawnDelay = 15f;
    [Tooltip("Percent chance a spawning guest is mafia, when no mafia is present.")]
    [SerializeField, Range(0, 100)] private int mafiaSpawnChance = 20;
    [Tooltip("After this many civilians in a row with no mafia at the bar, the next guest is guaranteed mafia.")]
    [SerializeField, Min(1)] private int guaranteeMafiaAfter = 4;

    // Cap on target rerolls. With ~60 combinations and at most a few guests, it's never reached in practice.
    private const int ProfileRollLimit = 64;

    private int evilMaskBase;
    private int evilAccessory;
    private int evilColor;
    private bool hasProfile;

    // Civilians spawned back to back while no target was at the bar.
    private int civiliansSinceMafia;

    public Transform viewportHolder;

    public int curGuest = 0;
    public int numGuests = 0;

    // Which seat indices are occupied. Frees correctly when any guest leaves.
    private readonly HashSet<int> occupiedSlots = new HashSet<int>();

    private Transform guestsHolder;

    // Colors available for masks
    public enum MaskColor
    {
        Black,
        Burgundy,
        ForestGreen,
        Gold,
        Navy
    }


    void Start()
    {
        GameObject holder = GameObject.Find("GuestsHolder");
        if (holder == null)
        {
            Debug.LogError("MaskLogic: no GuestsHolder in the scene.", this);
            enabled = false;
            return;
        }
        guestsHolder = holder.transform;

        CreateEnemyProfile();
        SpawnGuestWithMask();
        StartCoroutine(NPCSpawning());

    }

    IEnumerator NPCSpawning()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));

            if (GameState.Instance != null && GameState.Instance.IsGameOver) yield break;

            if (occupiedSlots.Count < maxGuests)
            {
                SpawnGuestWithMask();
            }
        }
    }



    private int RandomMask()
    {
        int index = Random.Range(0, 4);
        return index;
    }

    private int RandomAccessory()
    {
        int index = Random.Range(0, 3);
        return index;
    }

    private int RandomMaskColor()
    {
        int index = Random.Range(0, maskMaterials.Length);
        return index;
    }

    // Lowest free seat, so a guest leaving mid-run frees their slot for reuse.
    private int NextFreeSlot()
    {
        for (int i = 0; i < maxGuests; i++)
            if (!occupiedSlots.Contains(i)) return i;
        return -1;
    }

    // Read from the guests actually present, so every exit path clears it:
    // poisoned, spared, refused a wrong drink, or removed any other way.
    private bool MafiaPresent()
    {
        for (int i = 0; i < guestsHolder.childCount; i++)
        {
            var npc = guestsHolder.GetChild(i).GetComponent<NPCData>();
            if (npc != null && npc.isEvil) return true;
        }
        return false;
    }

    // True if anyone at the bar wears exactly this mask, color, and accessory.
    private bool MatchesGuestAtBar(int mask, int color, int accessory)
    {
        if (guestsHolder == null) return false;
        for (int i = 0; i < guestsHolder.childCount; i++)
        {
            var npc = guestsHolder.GetChild(i).GetComponent<NPCData>();
            if (npc != null && npc.maskType == mask && npc.maskColor == color && npc.accessory == accessory)
                return true;
        }
        return false;
    }

    // Called when a guest is removed for any reason - served, poisoned, or timed out.
    public void ReleaseGuest(int slot)
    {
        occupiedSlots.Remove(slot);
        numGuests = occupiedSlots.Count;
        curGuest = slot;
    }

    // A mafia member left the bar, so a fresh target goes on the sheet.
    public void RetireCurrentTarget()
    {
        CreateEnemyProfile();
    }


    public void CreateEnemyProfile()
    {
        int oldMask = evilMaskBase;
        int oldColor = evilColor;
        int oldAccessory = evilAccessory;

        // Reroll until the target differs from the last one and matches nobody
        // already at the bar, so the sheet never points at an innocent.
        for (int attempt = 0; attempt < ProfileRollLimit; attempt++)
        {
            evilMaskBase = RandomMask();
            evilColor = RandomMaskColor();
            evilAccessory = RandomAccessory();

            bool sameAsOld = hasProfile
                && evilMaskBase == oldMask && evilColor == oldColor && evilAccessory == oldAccessory;

            if (!sameAsOld && !MatchesGuestAtBar(evilMaskBase, evilColor, evilAccessory)) break;
        }
        hasProfile = true;

        foreach (Transform _maskTransform in viewportHolder.Find("Masks").transform)
        {
            GameObject _mask = _maskTransform.gameObject;
            if (_mask.name == evilMaskBase.ToString())
            {
                Renderer renderer = _mask.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = maskMaterials[evilColor];
                }
                _mask.SetActive(true);
            }
            else
            {
                _mask.SetActive(false);
            }
        }

        foreach (Transform _accTransform in viewportHolder.Find("Accessories").transform)
        {
            GameObject _acc = _accTransform.gameObject;
            if (_acc.name == evilAccessory.ToString())
            {
                _acc.SetActive(true);
            }
            else
            {
                _acc.SetActive(false);
            }
        }
    }

    // Instantiate a guest and equip it with either the target or civilian mask
    void SpawnGuestWithMask()
    {
        int slot = NextFreeSlot();
        if (slot < 0) return;

        // Strict < so the Inspector value is the real percentage.
        bool mafiaAtBar = MafiaPresent();
        bool spawnMafia = !mafiaAtBar
            && (Random.Range(0, 100) < mafiaSpawnChance || civiliansSinceMafia >= guaranteeMafiaAfter);

        int guestIndex = Random.Range(0, guestPrefabs.Length);
        GameObject guest = Instantiate(guestPrefabs[guestIndex], GuestSpawnPoint.position, GuestSpawnPoint.rotation, guestsHolder);
        guest.name = slot.ToString();

        if (spawnMafia)
        {
            civiliansSinceMafia = 0;

            guest.GetComponent<NPCData>().isEvil = true;
            guest.GetComponent<NPCData>().maskType = evilMaskBase;
            guest.GetComponent<NPCData>().maskColor = evilColor;
            guest.GetComponent<NPCData>().accessory = evilAccessory;

            GameObject mask = guest.transform.Find("Masks").Find(evilMaskBase.ToString()).gameObject;
            mask.SetActive(true);

            Renderer renderer = mask.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = maskMaterials[evilColor];
            }

            GameObject acc = guest.transform.Find("Accessories").Find(evilAccessory.ToString()).gameObject;
            acc.SetActive(true);

        }
        else
        {
            // Only a dry spell with no target at the bar counts toward the guarantee.
            if (!mafiaAtBar) civiliansSinceMafia++;

            // Civilians may share any one trait with the target, never all three,
            // so the player has to read the whole mask instead of one giveaway.
            int maskIndex, mat, accIndex;
            do
            {
                maskIndex = RandomMask();
                mat = RandomMaskColor();
                accIndex = RandomAccessory();
            }
            while (maskIndex == evilMaskBase && mat == evilColor && accIndex == evilAccessory);

            GameObject mask = guest.transform.Find("Masks").Find(maskIndex.ToString()).gameObject;
            mask.SetActive(true);
            guest.GetComponent<NPCData>().maskType = maskIndex;

            Renderer renderer = mask.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = maskMaterials[mat];
            }
            guest.GetComponent<NPCData>().maskColor = mat;//int.Parse(mat.name);

            GameObject acc = guest.transform.Find("Accessories").Find(accIndex.ToString()).gameObject;
            acc.SetActive(true);
            guest.GetComponent<NPCData>().accessory = accIndex;

        }

        guest.GetComponent<WalkToPoints>().walkingDirection = 1;
        guest.GetComponent<WalkToPoints>()._guestIndex = slot;

        occupiedSlots.Add(slot);
        numGuests = occupiedSlots.Count;
        curGuest = slot;
    }

    // Attach mask and accessory to the guest's mask anchor
    //void AttachMask(Transform maskAnchor, MaskType type, MaskColor color, MaskAccessory accessory)
    //{
    // int typeIndex = (int)type;

    // Validate mask type index
    //  if (typeIndex < 0 || typeIndex >= maskTypeModels.Length)
    //   {
    //       Debug.LogError("Invalid mask type index.");
    //        return;
    //   }

    // Instantiate the mask prefab as a child of maskAnchor
    //  GameObject mask = Instantiate(maskTypeModels[typeIndex], transform.position, transform.rotation, maskAnchor);
    //  mask.tag = "Mask";

    // Reset transform so it aligns properly
    //mask.transform.position = new Vector3(0,0,.185f);
    //mask.transform.localRotation = Quaternion.identity;
    //mask.transform.localScale = Vector3.one;

    // Apply the selected color/material to the mask
    //  Renderer renderer = mask.GetComponent<Renderer>();
    //  if (renderer != null && (int)color < maskMaterials.Length)
    //  {
    //      renderer.material = maskMaterials[(int)color];
    //  }


    //}

}