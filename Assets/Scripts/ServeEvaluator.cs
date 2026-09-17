using UnityEngine;

// Shared serve validation for both PC and VR. Single source of truth for
// what counts as a correct drink so the two versions can't drift apart.
public enum ServeResult { Correct, WrongOrder, EvilNotPoisoned, EvilPoisoned }

public static class ServeEvaluator
{
    // Compares a finished drink against a guest's order.
    public static ServeResult Evaluate(DrinkProperties drink, NPCData guest)
    {
        if (drink == null || guest == null) return ServeResult.WrongOrder;

        bool orderMatches = drink.drinkFlavor == guest.desiredFlavor
                         && drink.hasIce == guest.wantsIce
                         && drink.topping == guest.desiredTopping;

        if (!orderMatches) return ServeResult.WrongOrder;
        if (!guest.isEvil) return ServeResult.Correct;

        return drink.hasPoison ? ServeResult.EvilPoisoned : ServeResult.EvilNotPoisoned;
    }

    // True when the serve should cost the player a strike.
    public static bool IsStrike(ServeResult result)
    {
        return result == ServeResult.WrongOrder || result == ServeResult.EvilNotPoisoned;
    }

    // A drink is only servable once it has been fully made.
    public static bool IsComplete(DrinkProperties drink)
    {
        return drink != null && drink.drinkFlavor != null && drink.topping != null;
    }
}