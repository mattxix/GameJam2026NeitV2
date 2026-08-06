using UnityEngine;

public class SnapDrink : MonoBehaviour
{
    public string objTag;
    public string requiredName;

    [Header("Snapping Settings")]
    public Transform snapPoint;
    public float snapRadius = 0.5f;

    [Header("Access Granted Sound")]
    public AudioClip accessGrantedClip;
    [Range(0f, 1f)]
    public float accessGrantedVolume = 1f;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(objTag)) return;

        if (other.name.StartsWith(requiredName))
        {


            Debug.Log("placed");
        }
    }

    private void TrySnap(GameObject Drink)
    {
        if (snapPoint == null) return;

        if (Drink.transform.parent != null)
            return;

        float distance = Vector3.Distance(Drink.transform.position, snapPoint.position);

        if (distance <= snapRadius)
        {
            Drink.transform.position = snapPoint.position;
            Drink.transform.rotation = snapPoint.rotation;



            Rigidbody rb = Drink.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }


            Debug.Log("Medallion snapped to snap point");
        }
    }

  
}
