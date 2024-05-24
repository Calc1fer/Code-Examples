using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class FootstepController : MonoBehaviour
{
    /*This class will play a random footstep sound for each step as the player moves*/

    [SerializeField] private float volume = 10f; //Volume is between 0 and 1 (Allow user to specify between 1 and 100 for volume) 
    [SerializeField] private float runDelay = 0.2f;
    [SerializeField] private float walkDelay = 0.4f;
    [SerializeField] private AudioClip[] footstepClips;
    private AudioSource footstepAudio;

    private InputManager inputManager;
    private PlayerMovement playerMovement;

    private bool canPlay = true;
    private float muffleFactor = 0f;
    private int volumeScaler = 100; //Must be multiple factor above the volume
    
    private LowPassFilter lowPassFilter;

    private void Start()
    {
        inputManager = InputManager.Instance;
        playerMovement = GetComponent<PlayerMovement>();
        footstepAudio = GetComponent<AudioSource>();

        //Initialise the audio source with a clip so the OnAudioFilterRead function has data to play, otherwise no sound the filter
        //in the function will be played instead, meaning no sound in this case.
        footstepAudio.clip = footstepClips[0];
        
        //Initialise the low pass filter
        lowPassFilter = new LowPassFilter(100f, 1f, AudioSettings.outputSampleRate);
    }

    private void Update()
    {
        //If the player is moving
        if (inputManager.GetPlayerMoveInput() != Vector3.zero && playerMovement.GetIsGrounded())
        {
            if(inputManager.GetIsPlayerRunning()) PlayFootsteps(runDelay);
            if(!inputManager.GetIsPlayerRunning()) PlayFootsteps(walkDelay);
            
            muffleFactor = playerMovement.GetMuffleFactor();
        }
    }

    private void PlayFootsteps(float val)
    {
        //Delay the footstep clip based on whether running or walking
        if (canPlay)
        {
            canPlay = false;
            StartCoroutine(DelayFootstep(val));
        }
    }

    private IEnumerator DelayFootstep(float val)
    {
        yield return new WaitForSeconds(val);
        
        if (!footstepAudio.isPlaying)
        {
            //Select random clip from array
            AudioClip currentFootstep = footstepClips[Random.Range(0, footstepClips.Length)];
            
            //Assign to the AudioSource
            footstepAudio.clip = currentFootstep;
            footstepAudio.volume = volume / volumeScaler;
            Debug.Log(footstepAudio.volume);
            
            //Play the footstep
            footstepAudio.Play();
        }

        //Allows us to play the next footstep
        canPlay = true;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float[] filteredData = new float[data.Length];
        
        //Update the muffle factor
        lowPassFilter.SetCutoffFrequency(muffleFactor);

        if (playerMovement.GetOnInvisibleGround())
        {
            for (int i = 0; i < data.Length; i++)
            {
                filteredData[i] = lowPassFilter.Filter(data[i]);
            }
            
            Array.Copy(filteredData, data, data.Length);
        }
    }
}
