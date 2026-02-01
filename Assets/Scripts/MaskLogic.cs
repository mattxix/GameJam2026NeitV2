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

    private int evilMaskBase;
    private int evilAccessory;
    private int evilColor;
    private bool evilInScene;

    public Transform viewportHolder;

    public int curGuest = 0;
    public int numGuests = 0;

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
        CreateEnemyProfile();
        SpawnGuestWithMask();
        StartCoroutine(NPCSpawning());

    }

    IEnumerator NPCSpawning()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(1, 4));

            if (numGuests < 5)
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


   public void CreateEnemyProfile()
    {
        evilMaskBase = RandomMask();
        evilColor = RandomMaskColor();
        evilAccessory = RandomAccessory();

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

        int guestIndex = Random.Range(0, guestPrefabs.Length);
        GameObject guest = Instantiate(guestPrefabs[guestIndex], GuestSpawnPoint.position, GuestSpawnPoint.rotation, GameObject.Find("GuestsHolder").transform);
        guest.name = curGuest.ToString();

        if(Random.Range(0, 100) <= 20 && !evilInScene)
        {
            evilInScene = true;
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
            var maskIndex = RandomMask();
            GameObject mask = guest.transform.Find("Masks").Find(maskIndex.ToString()).gameObject;
            mask.SetActive(true);
            guest.GetComponent<NPCData>().maskType = maskIndex;


            var mat = RandomMaskColor();
            Renderer renderer = mask.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = maskMaterials[mat];
            }
            guest.GetComponent<NPCData>().maskColor = mat;//int.Parse(mat.name);

            int accIndex = RandomAccessory();

            do
            {
                accIndex = RandomAccessory();

            } while (accIndex == evilAccessory);

            GameObject acc = guest.transform.Find("Accessories").Find(accIndex.ToString()).gameObject;
            acc.SetActive(true);
            guest.GetComponent<NPCData>().accessory = accIndex;

        }

        guest.GetComponent<WalkToPoints>().walkingDirection = 1;
        guest.GetComponent<WalkToPoints>()._guestIndex = curGuest;

        numGuests++;
        curGuest = numGuests;



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

