using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class DrinkLogic : MonoBehaviour
{
    public GameObject currentGlass;
    public GameObject defaultGlass;
    public Camera mainCamera;
    public GameObject player;
    public MaskLogic maskLogic;
    public PopupService popupService;

    public Transform target;
    public float smoothTime = 0.1f;
    public int strikes = 0;

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
        if (currentGlass != null && currentGlass.GetComponent<DrinkProperties>().topping != null)
        {

            //currentGlass.GetComponent<DrinkProperties>().drinkFlavor = null;
            //currentGlass.GetComponent<DrinkProperties>().topping = null;
            //currentGlass.GetComponent<DrinkProperties>().hasIce = false;
            //currentGlass.GetComponent<DrinkProperties>().hasPoison = false;
            //currentGlass = null;
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

    public void PlaceDrink(Transform placePos)
    {
        //convert string to integer
        if (!placePos.Find("Drink") && currentGlass != null  && currentGlass.GetComponent<DrinkProperties>().topping != null)
        {

            GameObject drinkCopy = Instantiate(currentGlass, placePos.position, placePos.rotation, placePos);
            drinkCopy.GetComponent<DrinkProperties>().enabled = false;

            var holder = GameObject.Find("GuestsHolder");
            Debug.Log("PUT ON "+ placePos.transform.parent.gameObject.name);
            var corrGuest = holder.transform.Find(placePos.parent.name);
            var guestNeeds = corrGuest.GetComponent<NPCData>();

            var flavor = currentGlass.gameObject.GetComponent<DrinkProperties>().drinkFlavor;
            var hasIce = currentGlass.gameObject.GetComponent<DrinkProperties>().hasIce;
            var topping = currentGlass.gameObject.GetComponent<DrinkProperties>().topping;
            var isPoisoned = currentGlass.gameObject.GetComponent<DrinkProperties>().hasPoison;
            if (flavor == guestNeeds.desiredFlavor && hasIce == guestNeeds.wantsIce && topping == guestNeeds.desiredTopping)
            {

                if (guestNeeds.isEvil)
                {
                    if (isPoisoned)
                    {
                        soundService.PoisonedGuest(2f);    
                    }
                    else
                    {
                        soundService.Fail(2f);
                        strikes++;
                        StartCoroutine(popupService.PopupMenu("TEMP"));
                    }
                    maskLogic.CreateEnemyProfile();
                }
                else
                {
                    soundService.Glass();
                }


              
                //   Debug.Log("DRINKS MATCH!");
            }
            else
            {
                soundService.Fail(2f);
                strikes++;
                StartCoroutine(popupService.PopupMenu("TEMP"));
            }

            Destroy(drinkCopy);
            Destroy(corrGuest.gameObject);
            maskLogic.numGuests--;
            maskLogic.curGuest = int.Parse(placePos.parent.name);
            //.Find(placePos.gameObject.name);

            DeleteDrink();

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
