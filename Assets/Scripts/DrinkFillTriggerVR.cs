using UnityEngine;

public class DrinkFillTriggerVR : MonoBehaviour
{
    public LiquidGlass.Drink drink;
    public float flowRate = 0.4f;   // fill fraction per second
    public bool isPouring;

    void OnTriggerStay(Collider other)
    {
        if (!isPouring) return;
        var glass = other.GetComponentInParent<LiquidGlass>();
        if (glass != null && !glass.IsFull)
            glass.FillFrom(drink, flowRate * Time.deltaTime);
    }
}
