using UnityEngine;

public class DrinkRandomizer : MonoBehaviour
{
    // Drink Color Array
    public enum DrinkColor { Orange, Blue, Red, Green, White }

    // Drink Toppings Array
    public enum DrinkToppings { Lemon, Lime, Umbrella, Cherry, Orange }

    // Ice or No Ice Array
    public enum IceNoIce { Ice, NoIce }

    public DrinkColor[] drinkColors = { DrinkColor.Orange, DrinkColor.Blue, DrinkColor.Red, DrinkColor.Green, DrinkColor.White };
    public DrinkToppings[] drinkToppings = { DrinkToppings.Lemon, DrinkToppings.Lime, DrinkToppings.Umbrella, DrinkToppings.Cherry, DrinkToppings.Orange };
    public IceNoIce[] iceNoIce = { IceNoIce.Ice, IceNoIce.NoIce };

    // Selected drink attributes
    private DrinkColor selectedDrinkColor;
    private DrinkToppings selectedDrinkToppings;
    private IceNoIce selectedIceNoIce;

    void Start()
    {
        RandomizeDrink();
        ApplyDrinkAttributes(selectedDrinkColor, selectedDrinkToppings, selectedIceNoIce);
    }

    void Update()
    {
        // Add update logic if needed
    }

    void RandomizeDrink()
    {
        selectedDrinkColor = drinkColors[Random.Range(0, drinkColors.Length)];
        selectedDrinkToppings = drinkToppings[Random.Range(0, drinkToppings.Length)];
        selectedIceNoIce = iceNoIce[Random.Range(0, iceNoIce.Length)];
    }

    void ApplyDrinkAttributes(DrinkColor color, DrinkToppings topping, IceNoIce ice)
    {
        // Logic to apply the selected attributes to the drink
        // For example, update visuals or properties in your game
        Debug.Log($"Drink: Color={color}, Topping={topping}, Ice={ice}");
    }
}
