using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PopupService : MonoBehaviour
{

    public GameObject popup;
    public Button replay;
    public Button menu;
    public TMP_Text title;
    public TMP_Text content;
    public Volume volume;
    public GameObject player;
    private DepthOfField DOF;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        volume.profile.TryGet(out DOF);
    }

    // Update is called once per frame
    void Update()
    {
        float rot = Mathf.Sin(Time.time * .5f) * 2;
        float scale = Mathf.Sin(Time.time * .5f) * .08f;
        popup.transform.rotation = Quaternion.Euler(0, 0, rot);
        popup.transform.localScale = Vector3.one * (1 + scale);
    }

    public void RestartScene()
    {
  
     
    }

    public void GoToMenu()
    {

    }

    public IEnumerator PopupMenu(string condition)
    {
        player.GetComponent<FPMovement>().enabled = false;
        player.GetComponent<MouseLook>().enabled = false;
        player.transform.Find("Main Camera").GetComponent<MouseLook>().enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (condition == "PoisonedInnocent")
        {
            title.text = "YOU POISONED AN INNOCENT GUEST!";
            content.text = "An innocent party guest was poisoned and killed.";
        }
        if (condition == "DidNotPoison")
        {
            title.text = "YOU FAILED TO POISON THE TARGET!";
            content.text = "You let the target get away and failed your mission.";
        }
        if (condition == "LostJob")
        {
            title.text = "YOU LOST YOUR JOB!";
            content.text = "Too many guests became suspicious of your demeanor and your cover was blown.";
        }

        if (condition == "TEMP")
        {
            title.text = "YOU LOST YOUR JOB!";
            content.text = "YOU FAILED YOUR MISSION.";
        }

        for (float i = 0; i < 60; i++)
        {
            if (DOF != null)
            {
                Debug.Log(DOF.aperture.value);
                DOF.focalLength.value = Mathf.Lerp(1f, 56f, i / 60);
            }
               
            yield return new WaitForSeconds(0.02f);
        }

       // popup.SetActive(true);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);


    }

    public void ClickRetry()
    {
        Debug.Log("TETTTR");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
