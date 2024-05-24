using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*This script will be used to combine sounds so we can change the
 ambience depending on game events*/
public class CombineSounds : MonoBehaviour
{
    [SerializeField] private AudioClip ambientBase;

    private AudioSource audioSource;
    private AudioClip combinedClip;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    /*Call from external script and pass in the sound file to be combined with the
     ambient atmosphere*/
    public void CombineSoundClips(AudioClip secondaryClip, float volume)
    {
        //Reduce the amplitude of the secondaryClip
        float[] secondaryVolData = new float[secondaryClip.samples * secondaryClip.channels];
        secondaryClip.GetData(secondaryVolData, 0);
        
        for (int i = 0; i < secondaryVolData.Length; i++)
        {
            secondaryVolData[i] *= volume;
        }
        
        // Combine the clip data together
        //int length = Mathf.Max(ambientBase.samples, secondaryClip.samples);
        int length = ambientBase.samples * ambientBase.channels;
        float[] data = new float[length * ambientBase.channels];
        
        float[] ambientData = new float[length];
        ambientBase.GetData(ambientData, 0);
        
        // Combine the audio data by summing corresponding samples
        for (int i = 0; i < length; i++)
        {
            data[i] = ambientData[i] + secondaryVolData[i];
        
        }
        
        combinedClip = AudioClip.Create("CombinedClip", length / ambientBase.channels, ambientBase.channels, ambientBase.frequency, false);
        
        // Set the combined data to the new AudioClip
        combinedClip.SetData(data, 0);
        
        // Play the new sound
        audioSource.clip = combinedClip;
        audioSource.Play();
    }

    private void Update()
    {
    
    }
}
