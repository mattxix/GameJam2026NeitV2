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
    [SerializeField] private SFXService soundService;

    [Header("Lookup")]
    [SerializeField] private string guestsHolderName = "GuestsHolder";

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

        if (hasServed) return;
        if (GameState.Instance != null && GameState.Instance.IsGameOver) return;

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
        if (soundService != null)
        {
            switch (result)
            {
                case ServeResult.MafiaPoisoned:
                    soundService.PoisonedGuest(2f);
                    break;
                case ServeResult.CivilianPoisoned:
                    soundService.PoisonedGuest(2f);
                    break;
                case ServeResult.WrongOrder:
                    soundService.Fail(2f);
                    break;
                default:
                    soundService.Glass();
                    break;
            }
        }

        // A spared target keeps their profile live so they can return later.
        if (result == ServeResult.MafiaSpared && maskLogic != null)
            maskLogic.TargetEscaped();

        // GameState owns strikes, score, and the lose conditions.
        if (GameState.Instance != null) GameState.Instance.ReportServe(result);

        int slot;
        if (int.TryParse(name, out slot) && maskLogic != null)
            maskLogic.ReleaseGuest(slot);

        Destroy(guest.gameObject);
        if (drinkObject != null) Destroy(drinkObject.gameObject);

        hasServed = false;
    }

    private void Log(string message)
    {
        if (verboseLogging) Debug.Log("[NapkinServePoint] " + message, this);
    }
}