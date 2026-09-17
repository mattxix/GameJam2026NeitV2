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
        if (hasServed) return;

        var drink = args.interactableObject.transform.GetComponent<DrinkProperties>();
        if (!ServeEvaluator.IsComplete(drink)) return;

        NPCData guest = FindGuest();
        if (guest == null) return;

        hasServed = true;
        Resolve(ServeEvaluator.Evaluate(drink, guest), guest, args.interactableObject as XRGrabInteractable);
    }

    // Napkins are named to match guest objects, same convention as the PC build.
    private NPCData FindGuest()
    {
        GameObject holder = GameObject.Find(guestsHolderName);
        if (holder == null) return null;

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
}