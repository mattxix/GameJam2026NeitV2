using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Mirrors the VR systems (LiquidGlass, GlassIceReceiver, topping socket) into
// DrinkProperties so ServeEvaluator reads real values instead of PC-era nulls.
[RequireComponent(typeof(DrinkProperties))]
public class VRDrinkBridge : MonoBehaviour
{
    // Maps a LiquidGlass drink to the material name NPCData orders by.
    [Serializable]
    public struct FlavorMap
    {
        public LiquidGlass.Drink drink;
        public string flavorName;
    }

    [Header("Sources")]
    [SerializeField] private LiquidGlass liquid;
    [SerializeField] private GlassIceReceiver iceReceiver;
    [SerializeField] private XRSocketInteractor toppingSocket;

    [Header("Flavor Mapping")]
    [Tooltip("Flavor names must match the material names in DrinkProperties.drinkMaterials.")]
    [SerializeField] private FlavorMap[] flavorMap;

    [Tooltip("Fill fraction required before the glass counts as poured.")]
    [SerializeField, Range(0.05f, 1f)] private float minFillToCount = 0.15f;

    private DrinkProperties drink;

    private void Awake()
    {
        drink = GetComponent<DrinkProperties>();

        if (liquid == null) liquid = GetComponentInChildren<LiquidGlass>();
        if (iceReceiver == null) iceReceiver = GetComponent<GlassIceReceiver>();
        if (toppingSocket == null) toppingSocket = GetComponentInChildren<XRSocketInteractor>();
    }

    private void OnEnable()
    {
        if (toppingSocket == null) return;
        toppingSocket.selectEntered.AddListener(OnToppingAttached);
        toppingSocket.selectExited.AddListener(OnToppingRemoved);
    }

    private void OnDisable()
    {
        if (toppingSocket == null) return;
        toppingSocket.selectEntered.RemoveListener(OnToppingAttached);
        toppingSocket.selectExited.RemoveListener(OnToppingRemoved);
    }

    private void Update()
    {
        SyncFlavor();
        SyncIce();
    }

    // LiquidGlass owns the pour, so flavor follows whatever is actually in the glass.
    private void SyncFlavor()
    {
        if (liquid == null) return;

        if (liquid.Fill < minFillToCount)
        {
            drink.drinkFlavor = null;
            return;
        }

        drink.drinkFlavor = LookupFlavor(liquid.CurrentDrink);
    }

    private string LookupFlavor(LiquidGlass.Drink value)
    {
        for (int i = 0; i < flavorMap.Length; i++)
            if (flavorMap[i].drink == value) return flavorMap[i].flavorName;

        // Unmapped drinks would silently serve as wrong orders, so say so.
        Debug.LogWarning("VRDrinkBridge: no flavor mapping for " + value, this);
        return null;
    }

    private void SyncIce()
    {
        if (iceReceiver == null) return;
        drink.hasIce = iceReceiver.CurrentIce > 0;
    }

    private void OnToppingAttached(SelectEnterEventArgs args)
    {
        drink.topping = CleanName(args.interactableObject.transform.gameObject.name);
    }

    private void OnToppingRemoved(SelectExitEventArgs args)
    {
        drink.topping = null;
    }

    // VR topping prefabs carry suffixes ("(Clone)", ".VR") that won't match NPCData.
    private static string CleanName(string raw)
    {
        string clean = raw;

        int clone = clean.IndexOf("(Clone)", StringComparison.Ordinal);
        if (clone >= 0) clean = clean.Substring(0, clone);

        int suffix = clean.IndexOf(".VR", StringComparison.OrdinalIgnoreCase);
        if (suffix >= 0) clean = clean.Substring(0, suffix);

        return clean.Trim();
    }

   
}