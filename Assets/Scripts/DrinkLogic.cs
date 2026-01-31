using UnityEngine;

public class DrinkLogic : MonoBehaviour
{
    public GameObject currentGlass;
    public GameObject defaultGlass;
    public Camera mainCamera;

    public void PickupNewGlass()
    {
        if (currentGlass == null)
        {
            currentGlass = Object.Instantiate(defaultGlass, mainCamera.transform);
        }
    }

    public void DeleteDrink()
    {
        if (currentGlass != null)
        {
            Object.Destroy(currentGlass);
        }
    }

    public void PourDrink(string drinkLiquid)
    {
        if (currentGlass != null)
        {
            currentGlass.gameObject.GetComponent<DrinkProperties>().PourDrink(drinkLiquid);
        }
    }

    public void AddTopping(string topping)
    {
        if (currentGlass != null)
        {
            currentGlass.gameObject.GetComponent<DrinkProperties>().AddTopping(topping);
        }
    }

    public void DumpDrink()
    {
        if (currentGlass != null)
        {
            currentGlass.gameObject.GetComponent<DrinkProperties>().DumpDrink();
        }
    }

    public void AddIce()
    {
        if (currentGlass != null)
        {
            if (currentGlass != null)
            {
                currentGlass.gameObject.GetComponent<DrinkProperties>().AddIce();
            }
        }
    }

    public void AddPoison()
    {
        if (currentGlass != null)
        {
            if (currentGlass != null)
            {
                currentGlass.gameObject.GetComponent<DrinkProperties>().AddPoison();
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
