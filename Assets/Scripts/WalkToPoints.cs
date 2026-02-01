using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WalkToPoints : MonoBehaviour
{

    public float moveSpeed = 5f;
    public float stoppingDistance = 0.1f; // How close to the node before moving to the next

    private int currentNodeIndex = 0;

    public int walkingDirection;
    public int _guestIndex;
    private GameObject waypoints;
    private GameObject seats;
    public 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        waypoints = GameObject.Find("Waypoints");
        seats = GameObject.Find("Chairs");
        walkingDirection = 1;
    }

    // Update is called once per frame
    void Update()
    {
       FollowNodes();
    }



    void FollowNodes()
    {
     //  if (nodes == null || nodes.Count == 0)
      //  {
      //      return;
       // }

        // Get the target node's position
        Vector3 targetPosition = waypoints.transform.Find(currentNodeIndex.ToString()).position;

        // Move the current transform towards the target position
        if (currentNodeIndex == 3)
        {
            targetPosition = seats.transform.Find(_guestIndex.ToString()).position;
        }
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        
        transform.LookAt(targetPosition);
        // Check if the object is close enough to the current node
        if (Vector3.Distance(transform.position, targetPosition) < stoppingDistance)
        {
            // Move to the next node
            if(currentNodeIndex < 3)
            {
                currentNodeIndex += walkingDirection;
            }
            else
            {
               transform.rotation = seats.transform.Find(_guestIndex.ToString()).rotation;
                transform.GetComponent<NPCData>().ReachedBar();

            }


            Debug.Log(currentNodeIndex);


        }

    }

}
