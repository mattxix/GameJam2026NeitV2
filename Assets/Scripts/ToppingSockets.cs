using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;



/// One topping per drink, but three sockets so each garnish sits in its own
/// correct spot (rim wedge, in-drink cherry, umbrella, etc).
///
/// Fill any socket and the other two shut off, so a second garnish can't be
/// added. Pull it back out and all three open up again.
///
/// Put this on the glass root and drag the three sockets in.
public class ToppingSockets : MonoBehaviour
{
    [Tooltip("The garnish sockets. Only one may be filled at a time.")]
    public XRSocketInteractor[] sockets;

    [Tooltip("Optional: the drink this garnishes. Toppings are refused until it has liquid.")]
    public LiquidGlass liquid;

    /// <summary>Fires when a topping goes in or comes out. Null when removed.</summary>
    public event System.Action<GameObject> OnToppingChanged;

    /// <summary>The garnish currently on the drink, or null.</summary>
    public GameObject CurrentTopping { get; private set; }

    public bool HasTopping => CurrentTopping != null;

    void Awake()
    {
        foreach (var s in sockets)
        {
            if (s == null) continue;
            s.selectEntered.AddListener(OnSocketFilled);
            s.selectExited.AddListener(OnSocketEmptied);
        }
    }

    void OnDestroy()
    {
        foreach (var s in sockets)
        {
            if (s == null) continue;
            s.selectEntered.RemoveListener(OnSocketFilled);
            s.selectExited.RemoveListener(OnSocketEmptied);
        }
    }

    void Start() => Refresh();

    void OnSocketFilled(SelectEnterEventArgs args)
    {
        CurrentTopping = args.interactableObject.transform.gameObject;
        Refresh();
        OnToppingChanged?.Invoke(CurrentTopping);

        // Tutorial: a garnish on the glass finishes "Add the Topping" (Part 1)
        // and "Make their drink" (Part 2). Only the one currently showing counts.
        TutorialManager.Instance?.Complete(TutorialStep.AddTopping);
        TutorialManager.Instance?.Complete(TutorialStep.MakeDrink);
    }

    void OnSocketEmptied(SelectExitEventArgs args)
    {
        CurrentTopping = null;
        Refresh();
        OnToppingChanged?.Invoke(null);
    }

    /// <summary>Open every empty socket, or close them all if one is filled.</summary>
    void Refresh()
    {
        // An empty glass takes no garnish.
        bool allowed = liquid == null || !liquid.IsEmpty;

        foreach (var s in sockets)
        {
            if (s == null) continue;
            s.socketActive = allowed && (!HasTopping || s.hasSelection);
        }
    }

    void Update()
    {
        // The glass can empty at any time, so keep the sockets in step.
        if (liquid != null) Refresh();
    }

    /// <summary>Name of the garnish on the drink, or null. For order checking.</summary>
    public string ToppingName => CurrentTopping != null ? CurrentTopping.name : null;

    /// <summary>Drops the garnish and re-opens the sockets. Hook to your dump action.</summary>
    public void ClearTopping()
    {
        foreach (var s in sockets)
        {
            if (s == null || !s.hasSelection) continue;
            s.interactionManager.SelectExit(s, s.firstInteractableSelected);
        }
    }
}