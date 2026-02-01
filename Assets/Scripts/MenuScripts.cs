using UnityEngine;

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
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SetActiveCamera(cam1);
    }

    public void StartGame()
    {
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
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SetActiveCamera(cam1);
        menuCanvas.gameObject.SetActive(true);

    }

    public void Instructions()
    {
        Cursor.visible = true;
        SetActiveCamera(cam2);

    }

}


