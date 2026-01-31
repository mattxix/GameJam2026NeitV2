using UnityEngine;
using UnityEngine.InputSystem;

public class LadderScript : MonoBehaviour
{
    public Transform playerController;
    bool insideLadder = false;
    public float ladderSpeed = 8.0f;
    public FPMovement playerInput;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (insideLadder && Input.GetKey(KeyCode.W))
        {
            playerController.transform.position += Vector3.up / ladderSpeed;
        } 
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Ladder"))
        {   
            playerInput.enabled = false;
            insideLadder = !insideLadder;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Ladder"))
        {
            playerInput.enabled = true;
            insideLadder = !insideLadder;
        }
    }
}
