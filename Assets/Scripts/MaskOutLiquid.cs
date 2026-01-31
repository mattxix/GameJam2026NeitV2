using UnityEngine;

public class MaskOutLiquid : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
         
    }

    private void Update()
    {
        if(GetComponent<MeshRenderer>().material != null)
        {
             GetComponent<MeshRenderer>().material.renderQueue = 3002;
        }
    }


}
