using UnityEngine;
using UnityEngine.InputSystem;


public class MouseLook : MonoBehaviour
{
    public enum RotationAxis
    {
        mouseXandMouseY = 0,
        mouseX = 1,
        mouseY = 2
    }

    public RotationAxis axis = RotationAxis.mouseXandMouseY;
    public float sensX = 10.0f;
    public float sensY = 10.0f;
    public float maxVerticalRotation = 45.0f;
    public float minVerticalRotation = -45.0f;

    float mouseX;
    float mouseY;
    float verticalRotation = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       Cursor.visible = false;
       Cursor.lockState = CursorLockMode.Locked;
        Rigidbody rb = GetComponent<Rigidbody>();
        if(rb != null)
        {
            rb.freezeRotation = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(axis == RotationAxis.mouseX)
        {
            transform.Rotate(0, mouseX * sensX * Time.deltaTime, 0);
        }
        else if(axis == RotationAxis.mouseY)
        {
            verticalRotation -= mouseY * sensY * Time.deltaTime;
            verticalRotation = Mathf.Clamp(verticalRotation, minVerticalRotation, maxVerticalRotation);
            float horizontalRotation = transform.localEulerAngles.y;
            transform.localEulerAngles = new Vector3(verticalRotation, horizontalRotation, 0);

        }
        else
        {
            verticalRotation -= mouseY * sensY * Time.deltaTime;
            verticalRotation = Mathf.Clamp(verticalRotation, minVerticalRotation, maxVerticalRotation);

            float deltaRotation = mouseX * sensX * Time.deltaTime;
            float horizontalRotation = transform.localEulerAngles.y + deltaRotation;
            transform.localEulerAngles = new Vector3(verticalRotation, horizontalRotation, 0);

        }
    }

    public void LookValues(InputAction.CallbackContext ctx)
    {
        //Debug.Log(ctx.ReadValue<Vector2>());
        mouseX = ctx.ReadValue<Vector2>().x;
        mouseY = ctx.ReadValue<Vector2>().y;

    }
}
