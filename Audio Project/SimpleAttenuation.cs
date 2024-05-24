using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*This is a simple attenuation script for increasing or decreasing the volume of sound/music/ambience
 depending on the player proximity to an objective or specific objects*/
public class SimpleAttenuation : MonoBehaviour
{
    [SerializeField] private float distanceThreshold = 10f;
    
    private PlayerMovement playerRef;
    private AudioSource audioSource;
    private float externalVol;
    
    private void Start()
    {
        playerRef = PlayerMovement.Instance;
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        float distance = Vector3.Distance(playerRef.transform.position ,transform.position);

        if (distance > distanceThreshold) return;
        
        float volume = Mathf.Clamp01(1 - (distance / distanceThreshold));
        audioSource.volume = Mathf.Min(volume, externalVol);
    }
    
    /*Getters*/
    public float GetDistanceThreshold()
    {
        return distanceThreshold;
    }

    public void SetExternalVolume(float val)
    {
        externalVol = val;
    }
}
