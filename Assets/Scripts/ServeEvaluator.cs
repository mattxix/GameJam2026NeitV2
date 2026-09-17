using UnityEngine;

// Shared serve validation for both PC and VR. Single source of truth for
// what counts as a correct drink so the two versions can't drift apart.
public enum ServeResult
{
    Correct,            // civilian, right drink, clean
    MafiaSpared,        // mafia, right drink, clean - walks away alive
    MafiaPoisoned,      // mafia, right drink, poisoned - a kill
    CivilianPoisoned,   // innocent killed - instant loss
    WrongOrder          // they refuse it - a strike
}

public static class ServeEvaluator
{
    // Order accuracy decides whether they drink at all; poison resolves after.
    public static ServeResult Evaluate(DrinkProperties drink, NPCData guest)
    {
        if (drink == null || guest == null) return ServeResult.WrongOrder;

        bool orderMatches = drink.drinkFlavor == guest.desiredFlavor
                         && drink.hasIce == guest.wantsIce
                         && drink.topping == guest.desiredTopping;

        // A wrong drink is never consumed, so poison in it never lands.
        if (!orderMatches) return ServeResult.WrongOrder;

        if (drink.hasPoison)
            return guest.isEvil ? ServeResult.MafiaPoisoned : ServeResult.CivilianPoisoned;

        return guest.isEvil ? ServeResult.MafiaSpared : ServeResult.Correct;
    }

    // Only a refused drink costs a strike.
    public static bool IsStrike(ServeResult result)
    {
        return result == ServeResult.WrongOrder;
    }

    // Killing an innocent ends the run immediately.
    public static bool IsInstantLoss(ServeResult result)
    {
        return result == ServeResult.CivilianPoisoned;
    }

    // A mafia member removed from the board.
    public static bool IsKill(ServeResult result)
    {
        return result == ServeResult.MafiaPoisoned;
    }

    // A drink is only servable once it has been fully made.
    public static bool IsComplete(DrinkProperties drink)
    {
        return drink != null && drink.drinkFlavor != null && drink.topping != null;
    }
}