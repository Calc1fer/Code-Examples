using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*This class will allow objects to respond to the player when they whistle*/
public class WhistleResponse : MonoBehaviour
{
    private SimpleAttenuation simpleAttenuation;
    private AudioSource audioSource;
    private PlayerMovement playerRef;
    private InputManager inputManager;

    private bool canPlay = true;
    private float maxDistance;  //Maximum distance the player can be from the object for it to still play a sound

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        inputManager = InputManager.Instance;
        playerRef = PlayerMovement.Instance;
        simpleAttenuation = GetComponent<SimpleAttenuation>();
    }
    
    public void Response(AudioClip responseSound, float responseDelay, float volume)
    {
        simpleAttenuation.SetExternalVolume(volume);
        //audioSource.clip = responseSound;
        
        float distance = Vector3.Distance(transform.position, playerRef.transform.position);
        
        if (canPlay && distance <= simpleAttenuation.GetDistanceThreshold())
        {
            StartCoroutine(DelayResponse(responseSound, responseDelay, volume));
            canPlay = false;
        }
    }

    private IEnumerator DelayResponse(AudioClip responseSound, float responseDelay, float volume)
    {
        yield return new WaitForSeconds(responseDelay);
        
        if (responseSound != null)
        {
            float lowerPitch = 0.25f;
            audioSource.pitch = lowerPitch;
            audioSource.PlayOneShot(responseSound, volume);
        }

        canPlay = true;
    }
}
