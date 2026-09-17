using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Temporary diagnostic: reports what the socket can actually see.
public class SocketProbe : MonoBehaviour
{
    private XRSocketInteractor socket;
    // Reports who currently has the glass selected.
    private void OnTriggerStay(Collider other)
    {
        if (Time.frameCount % 120 != 0) return;

        var grab = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null) return;

        Debug.Log("[Probe] " + grab.name
            + " isSelected=" + grab.isSelected
            + " selectingCount=" + grab.interactorsSelecting.Count, this);

        foreach (var interactor in grab.interactorsSelecting)
            Debug.Log("[Probe]   held by: " + interactor.transform.name, this);
    }
    private void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
        Debug.Log("[Probe] socket found: " + (socket != null), this);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[Probe] trigger entered by: " + other.name + " on layer " + LayerMask.LayerToName(other.gameObject.layer), this);
    }

    private void Update()
    {
        if (socket == null) return;
        if (Time.frameCount % 60 != 0) return;

        var targets = new System.Collections.Generic.List<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable>();
        socket.GetValidTargets(targets);
        if (targets.Count > 0) Debug.Log("[Probe] valid targets: " + targets.Count, this);
    }
}