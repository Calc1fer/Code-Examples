using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SpatialAudio : MonoBehaviour
{
    /*This script component will allow for basic spatial localisation based on
     an object's position and the player main camera position.*/
    [SerializeField] private float minChannelValue = 0.4f;
    
    private Vector3 listenerPos; //The position of the player i.e. the main camera since it is in first person
    private Vector3 listenerForward;
    private Vector3 listenerRight;
    private AudioSource audioSource;
    private Vector3 objectPosition;
    
    //testing variables
    private float left;
    private float right;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        objectPosition = transform.position;
        listenerPos = Camera.main.transform.position;
        listenerForward = Camera.main.transform.forward;
        listenerRight = Camera.main.transform.right;
    }

    private void Update()
    {
        listenerPos = Camera.main.transform.position;
        listenerForward = Camera.main.transform.forward;
        listenerRight = Camera.main.transform.right;
        objectPosition = transform.position;
        //Debug.Log($"left: {left}, right: {right}");
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        // Calculate pan based on the position of the sound source (this object)
        float panValue = CalculatePanValue(listenerPos);

        // Apply spatialization to the audio data
        ApplySpatialisation(data, channels, panValue);
    }

    float CalculatePanValue(Vector3 listenerPos)
    {
        // Calculate the direction from the listener to the sound source
        Vector3 toSource = (objectPosition - listenerPos).normalized;

        // Calculate the dot product between the direction to the source and the listener's right vector
        float panValue = Vector3.Dot(toSource, listenerRight);

        return panValue;
    }

    private void ApplySpatialisation(float[] data, int channels, float panValue)
    {
        // Apply panning to each channel separately
        for (int i = 0; i < data.Length; i += channels)
        {
            data[i] *= Mathf.Clamp(1f - panValue, minChannelValue, 1);       // Left channel
            data[i + 1] *= Mathf.Clamp(1f + panValue, minChannelValue, 1f);   // Right channel

            left = data[i];
            right = data[i + 1];
        }
    }
}