using UnityEngine;

public class DrinkLogic : MonoBehaviour
{
    public GameObject currentGlass;
    public GameObject defaultGlass;
    public Camera mainCamera;
    public GameObject player;

    public Transform target;
    public float smoothTime = 0.1f; 


    private Vector3 positionVelocity = Vector3.zero;
    private Vector3 rotationVelocity = Vector3.zero;

    public SFXService soundService;

    void LateUpdate()
    {
        if (target != null && currentGlass != null)
        {
            // Smoothly damp the position towards the target's position

            Vector3 targetPosition = target.position;
            //Debug.Log(targetPosition);
            currentGlass.transform.position = Vector3.SmoothDamp(currentGlass.transform.position, targetPosition, ref positionVelocity, smoothTime);

            // Smoothly damp the rotation towards the target's rotation
            Quaternion targetRotation = new Quaternion(0, target.rotation.y, 0, target.rotation.w);
            currentGlass.transform.rotation = Quaternion.Slerp(currentGlass.transform.rotation, targetRotation, Time.deltaTime * (1.0f / smoothTime) * .8f);
        }
    }

    public void PickupNewGlass()
    {
        if (currentGlass == null)
        {
            currentGlass = Object.Instantiate(defaultGlass, new Vector3(0, 0, 0), Quaternion.identity);
            soundService.Glass();
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
            if (currentGlass.gameObject.GetComponent<DrinkProperties>().drinkFlavor == null)
            {
                soundService.Pouring();
            }
            currentGlass.gameObject.GetComponent<DrinkProperties>().PourDrink(drinkLiquid);
            
        }
    }

    public void AddTopping(string topping)
    {
        if (currentGlass != null)
        {
            if (currentGlass.gameObject.GetComponent<DrinkProperties>().topping == null)
            {
                soundService.Grab(2.0f);
            }
            currentGlass.gameObject.GetComponent<DrinkProperties>().AddTopping(topping);
          
        }
    }

    public void DumpDrink()
    {
        if (currentGlass != null)
        {
            if (currentGlass.gameObject.GetComponent<DrinkProperties>().drinkFlavor != null)
            {
                soundService.Pouring();
            }
            currentGlass.gameObject.GetComponent<DrinkProperties>().DumpDrink();
           
        }
    }

    public void AddIce()
    {
        if (currentGlass != null)
        {
            if (currentGlass != null)
            {
                if (currentGlass.gameObject.GetComponent<DrinkProperties>().hasIce == false)
                {
                    soundService.Ice(1.5f);
                }
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
                if (currentGlass.gameObject.GetComponent<DrinkProperties>().hasPoison == false)
                {
                    soundService.Poison(2.0f);
                }
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
        if (currentGlass != null)
        {
            LateUpdate();
        }
    }
}
