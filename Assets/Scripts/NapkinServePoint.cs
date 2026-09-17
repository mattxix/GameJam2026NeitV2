using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// VR replacement for DrinkLogic.PlaceDrink. The player socketing a glass here
// serves it to the guest whose name matches this napkin.
[RequireComponent(typeof(XRSocketInteractor))]
public class NapkinServePoint : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private MaskLogic maskLogic;
    [SerializeField] private PopupService popupService;
    [SerializeField] private SFXService soundService;

    [Header("Lookup")]
    [SerializeField] private string guestsHolderName = "GuestsHolder";

    [Header("Scoring")]
    [SerializeField] private int strikes;
    public int Strikes => strikes;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    private XRSocketInteractor socket;
    private bool hasServed;

    private void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
    }

    private void OnEnable()
    {
        socket.selectEntered.AddListener(OnDrinkPlaced);
    }

    private void OnDisable()
    {
        socket.selectEntered.RemoveListener(OnDrinkPlaced);
    }

    private void OnDrinkPlaced(SelectEnterEventArgs args)
    {
        Log("Socket fired on napkin " + name);

        if (hasServed)
        {
            Log("Bailed: already serving.");
            return;
        }

        GameObject placed = args.interactableObject.transform.gameObject;
        DrinkProperties drink = FindDrink(placed);

        if (drink == null)
        {
            Log("Bailed: no DrinkProperties found on " + placed.name);
            return;
        }

        Log("Drink found. flavor=" + (drink.drinkFlavor ?? "NULL")
            + " topping=" + (drink.topping ?? "NULL")
            + " ice=" + drink.hasIce
            + " poison=" + drink.hasPoison);

        if (!ServeEvaluator.IsComplete(drink))
        {
            Log("Bailed: drink incomplete (needs both flavor and topping).");
            return;
        }

        NPCData guest = FindGuest();
        if (guest == null)
        {
            Log("Bailed: no guest named '" + name + "' under " + guestsHolderName);
            return;
        }

        Log("Guest found. wants flavor=" + guest.desiredFlavor
            + " topping=" + guest.desiredTopping
            + " ice=" + guest.wantsIce
            + " evil=" + guest.isEvil);

        hasServed = true;
        ServeResult result = ServeEvaluator.Evaluate(drink, guest);
        Log("Result: " + result);

        Resolve(result, guest, args.interactableObject as XRGrabInteractable);
    }

    // Glass meshes vary, so check the object, its children, then its parents.
    private DrinkProperties FindDrink(GameObject placed)
    {
        DrinkProperties drink = placed.GetComponent<DrinkProperties>();
        if (drink == null) drink = placed.GetComponentInChildren<DrinkProperties>();
        if (drink == null) drink = placed.GetComponentInParent<DrinkProperties>();
        return drink;
    }

    // Napkins are named to match guest objects, same convention as the PC build.
    private NPCData FindGuest()
    {
        GameObject holder = GameObject.Find(guestsHolderName);
        if (holder == null)
        {
            Log("Bailed: could not find object named " + guestsHolderName);
            return null;
        }

        Transform guest = holder.transform.Find(name);
        return guest == null ? null : guest.GetComponent<NPCData>();
    }

    private void Resolve(ServeResult result, NPCData guest, XRGrabInteractable drinkObject)
    {
        switch (result)
        {
            case ServeResult.Correct:
                soundService.Glass();
                break;
            case ServeResult.EvilPoisoned:
                soundService.PoisonedGuest(2f);
                maskLogic.CreateEnemyProfile();
                break;
            case ServeResult.EvilNotPoisoned:
                Fail();
                maskLogic.CreateEnemyProfile();
                break;
            default:
                Fail();
                break;
        }

        if (ServeEvaluator.IsStrike(result)) strikes++;

        int guestIndex;
        if (int.TryParse(name, out guestIndex)) maskLogic.curGuest = guestIndex;
        maskLogic.numGuests--;

        Destroy(guest.gameObject);
        if (drinkObject != null) Destroy(drinkObject.gameObject);

        hasServed = false;
    }

    private void Fail()
    {
        soundService.Fail(2f);
        StartCoroutine(popupService.PopupMenu("TEMP"));
    }

    private void Log(string message)
    {
        if (verboseLogging) Debug.Log("[NapkinServePoint] " + message, this);
    }
}