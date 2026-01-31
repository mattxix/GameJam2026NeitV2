using UnityEngine;
using UnityEngine.InputSystem;

public class RaycastFromPlayer : MonoBehaviour
{
    public float raycastDistance = 5.0f;
    bool holdingItem = false;
    GameObject heldObject;
    MeshRenderer hitObj;

    public Camera mainCamera;
    public LayerMask interactableLayer;
    public InputActionReference interactionAction;
    private GameObject hoveredObject;
    private GameObject lastHovered;
    public DrinkLogic drinkLogic;

    private void OnEnable()
    {
        interactionAction.action.Enable();
        interactionAction.action.performed += OnInteract;
    }

    private void OnDisable()
    {
        interactionAction.action.performed -= OnInteract;
        interactionAction.action.Disable();
    }

    // Update is called once per frame

    private void Start()
    {
    }
    private void Update()
    {
        DetectHover();
    }

    private void DetectHover()
    {
        hoveredObject = null;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableLayer))
        {
            hoveredObject = hit.collider.gameObject;
            if (lastHovered != null && lastHovered != hoveredObject)
            {
                lastHovered.GetComponent<Outline>().OutlineWidth = 0f;

            }
            hoveredObject.GetComponent<Outline>().OutlineWidth = 10f;
        }
        else
        {
            if(lastHovered != null)
            {
                lastHovered.GetComponent<Outline>().OutlineWidth = 0f;

            }
        }
        lastHovered = hoveredObject;

    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (hoveredObject == null)
            return;

        if (hoveredObject.name == "Glasses")
        {
            drinkLogic.PickupNewGlass();
        }
        if (hoveredObject.name == "Sink")
        {
            drinkLogic.DumpDrink();
        }
        if (hoveredObject.name == "Ice")
        {
            drinkLogic.AddIce();
        }
        if (hoveredObject.name == "Poison")
        {
            drinkLogic.AddPoison();
        }
        if (hoveredObject.CompareTag("DrinkPour"))
        {
            drinkLogic.PourDrink(hoveredObject.name);
        }
        if (hoveredObject.CompareTag("DrinkTopping"))   
        {
            drinkLogic.AddTopping(hoveredObject.name);
        }


        // Example: call interaction on hovered object
        // if (hoveredObject.TryGetComponent<IInteractable>(out var interactable))
        //  {
        //      interactable.Interact();
        //  }
    }
}
