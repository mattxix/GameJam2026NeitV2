using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandAnimation : MonoBehaviour
{
    public Animator anim;
    public InputActionReference gripInteraction;
    public InputActionReference triggerInteraction;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        float gripValue = gripInteraction.action.ReadValue<float>();
        float triggerValue = triggerInteraction.action.ReadValue<float>();

        anim.SetFloat("Grip", gripValue);
        anim.SetFloat("Trigger", triggerValue);
    }
}
