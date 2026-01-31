using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public class DrinkProperties : MonoBehaviour
{

    public string drinkFlavor;
    public bool hasIce;
    public bool hasPoison;
    public string topping;
    public Material[] drinkMaterials;
    public GameObject[] drinkToppings;
    public GameObject liquid;
    public GameObject ice;
    public GameObject poison;
    private float fillProgress = 0.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        drinkFlavor = null;
        topping = null;
        hasIce = false;
        hasPoison = false;
    }

    // Update is called once per frame
    public Material FindFlavor(string nameToFind)
    {
        foreach (Material go in drinkMaterials)
        {
            if (go != null && go.name == nameToFind)
            {
                // Return the first object found with that name
                return go;
            }
        }

        // Return null if no object with the name is found
        Debug.LogWarning("Could not find object with name: " + nameToFind);
        return null;
    }

    public GameObject FindTopping(string nameToFind)
    {
        foreach (GameObject go in drinkToppings)
        {
            if (go != null && go.name == nameToFind)
            {
                // Return the first object found with that name
                return go;
            }
        }

        // Return null if no object with the name is found
        Debug.LogWarning("Could not find object with name: " + nameToFind);
        return null;
    }


    void Update()
    {
        if (drinkFlavor == null)
        {
            liquid.SetActive(false);
        }
        else
        {
            liquid.SetActive(true);
            // Debug.Log(drinkFlavor);
            liquid.transform.localScale = Vector3.one * fillProgress;
            liquid.transform.localPosition = new Vector3(0, Mathf.Lerp(.5f, 0, fillProgress), 0);
            liquid.gameObject.GetComponent<MeshRenderer>().material = FindFlavor(drinkFlavor);
        }

        if (topping != null)
        {
            GameObject currentToppingObject = FindTopping(topping);
            if (currentToppingObject != null)
            {
                currentToppingObject.SetActive(true);
            }
        }
        else
        {
            foreach (GameObject go in drinkToppings)
            {
                go.SetActive(false);
            }
        }

        ice.SetActive(hasIce);
        poison.SetActive(hasPoison);
    }

    public void DumpDrink()
    {
        drinkFlavor = null;
        hasIce = false;
        topping = null;
        hasPoison = false;
    }

    public void AddIce()
    {
        hasIce = true;
    }
    public void AddPoison()
    {
        if(topping != null)
        {
            hasPoison = true;
        }
    }

    public void PourDrink(string drinkLiquid)
    {
        if (drinkFlavor == null)
        {
            drinkFlavor = drinkLiquid;
            StartCoroutine(FillLiquidAnim());
        }
    }

    public void AddTopping(string addedTopping)
    {
        if (drinkFlavor != null && topping == null)
        {
            topping = addedTopping;
        }
    }

    IEnumerator FillLiquidAnim()
    {
        float frames = 100;
        float currentFrame = 0;
        for (float i = 0; i < frames; i++)
        {
            currentFrame++;
            fillProgress = currentFrame / frames;
            Debug.Log(fillProgress);
            yield return new WaitForEndOfFrame();
        }
    }
}
