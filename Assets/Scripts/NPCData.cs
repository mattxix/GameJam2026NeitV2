using Unity.VisualScripting;
using UnityEngine;

public class NPCData : MonoBehaviour
{

    public string desiredFlavor;
    public string desiredTopping;
    public bool wantsIce;
    public bool isEvil;
    public int maskType;
    public int accessory;
    public int maskColor;
    public DrinkProperties drinkProperties;
    public Canvas speechBubble;

    string RandomFlavor()
    {
        return drinkProperties.drinkMaterials[Random.Range(0, drinkProperties.drinkMaterials.Length)].name;
    }

    bool RandomIce()
    {
       return (Random.value > 0.5f);
    }

    string RandomTopping()
    {
        return drinkProperties.drinkToppings[Random.Range(0, drinkProperties.drinkToppings.Length)].gameObject.name;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        desiredFlavor = RandomFlavor();
        wantsIce = RandomIce();
        desiredTopping = RandomTopping();

        speechBubble.transform.Find(desiredFlavor).gameObject.SetActive(true);
        speechBubble.transform.Find(desiredTopping).gameObject.SetActive(true);
        speechBubble.transform.Find("Ice").gameObject.SetActive(wantsIce);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
