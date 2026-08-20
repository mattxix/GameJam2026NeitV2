using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
public class SimpleHapticFeedback : MonoBehaviour
{
    HapticImpulsePlayer controller;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<HapticImpulsePlayer>();
    }

    public void PlayHapticFeedback(float amplitude, float duration)
    {
        if (controller != null)
        {

            controller.SendHapticImpulse(amplitude, duration);
        }
    }
}