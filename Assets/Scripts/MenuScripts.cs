using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuScripts : MonoBehaviour
{
    public Camera cam1;
    public Camera cam2;
 
    public Canvas menuCanvas;

 
    private void SetActiveCamera(Camera activeCam)
    {
        if (cam1 != null)
        {
            cam1.gameObject.SetActive(cam1 == activeCam);
        }
        if (cam2 != null)
        {
            cam2.gameObject.SetActive(cam2 == activeCam);
        }
    }

    void Start()
    {
        SceneManager.LoadScene(0);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SetActiveCamera(cam1);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(1);
        if (menuCanvas != null)
        {
            menuCanvas.gameObject.SetActive(false);
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SetActiveCamera(cam1);
        menuCanvas.gameObject.SetActive(true);

    }

    public void Instructions()
    {
        SceneManager.LoadScene(2);
        Cursor.visible = true;
        SetActiveCamera(cam2);
    }

}


