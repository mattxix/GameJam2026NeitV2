using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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
    private GameObject timers;
    private Slider countdownSlider;
    private float timeRemaining;
    private GameObject sliderParent;

    float timer = 0f;
    float duration = 100f;
    float interval = 0.01f;

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
        //timeRemaining = 30.0f;
        desiredFlavor = RandomFlavor();
        wantsIce = RandomIce();
        desiredTopping = RandomTopping();

        speechBubble.transform.Find(desiredFlavor).gameObject.SetActive(true);
        speechBubble.transform.Find(desiredTopping).gameObject.SetActive(true);
        speechBubble.transform.Find("Ice").gameObject.SetActive(wantsIce);
        speechBubble.enabled = false;

        var timerP = GameObject.Find("Timers").transform.Find(transform.name);
       // sliderParent = timerP.gameObject;
        countdownSlider = timerP.transform.Find("Slider").GetComponent<Slider>();
        countdownSlider.value = 1;

        //sliderParent.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
       // Debug.Log(timeRemaining / 10f);
        //countdownSlider.GetComponent<Slider>().value = 1-(timeRemaining / 30f);
    }

    public void ReachedBar()
    {
        //sliderParent.SetActive(true);
        speechBubble.enabled = true;
        //timeRemaining = 10f;
      //  StartCoroutine(CountdownTillExit());
    }

    IEnumerator CountdownTillExit()
    {
      

        while (timer < duration)
        { 
            timer += interval;
            //timeRemaining = duration - timer;
            countdownSlider.GetComponent<Slider>().value = ((duration - timer)/duration);

            yield return new WaitForSeconds(interval);
        }

        var maskLogic = GameObject.Find("MaskManager").GetComponent<MaskLogic>();

        if (isEvil)
        {
            maskLogic.CreateEnemyProfile();
        }

        var oldName = gameObject.name;
        gameObject.name = "UNUSED";

        maskLogic.curGuest = int.Parse(oldName);
        maskLogic.numGuests--;
        Destroy(gameObject);


        //countdownSlider.transform.parent.gameObject.SetActive(false);


    }
}
