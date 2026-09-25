using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Mirrors the VR systems (LiquidGlass, GlassIceReceiver, topping sockets) into
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

    // Stand-in for "more than one garnish" - never matches a real order.
    private const string MultipleToppings = "__MULTIPLE__";

    [Header("Sources")]
    [SerializeField] private LiquidGlass liquid;
    [SerializeField] private GlassIceReceiver iceReceiver;

    [Tooltip("Every topping socket on the glass. Left empty, all child sockets are found automatically.")]
    [SerializeField] private XRSocketInteractor[] toppingSockets;

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

        // Singular lookup only caught one socket, so the cherry and umbrella never registered.
        if (toppingSockets == null || toppingSockets.Length == 0)
            toppingSockets = GetComponentsInChildren<XRSocketInteractor>(true);
    }

    private void OnEnable()
    {
        for (int i = 0; i < toppingSockets.Length; i++)
        {
            if (toppingSockets[i] == null) continue;
            toppingSockets[i].selectEntered.AddListener(OnToppingChanged);
            toppingSockets[i].selectExited.AddListener(OnToppingRemoved);
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < toppingSockets.Length; i++)
        {
            if (toppingSockets[i] == null) continue;
            toppingSockets[i].selectEntered.RemoveListener(OnToppingChanged);
            toppingSockets[i].selectExited.RemoveListener(OnToppingRemoved);
        }
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

    private void OnToppingChanged(SelectEnterEventArgs args)
    {
        RecomputeTopping();
    }

    private void OnToppingRemoved(SelectExitEventArgs args)
    {
        RecomputeTopping();
    }

    // Reads every socket rather than trusting one event, so removals resolve correctly.
    private void RecomputeTopping()
    {
        string found = null;
        int count = 0;

        for (int i = 0; i < toppingSockets.Length; i++)
        {
            XRSocketInteractor socket = toppingSockets[i];
            if (socket == null || !socket.hasSelection) continue;

            IList<IXRSelectInteractable> held = socket.interactablesSelected;
            for (int j = 0; j < held.Count; j++)
            {
                count++;
                if (found == null) found = CleanName(held[j].transform.gameObject.name);
            }
        }

        if (count == 0) drink.topping = null;
        else if (count == 1) drink.topping = found;
        else drink.topping = MultipleToppings;
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