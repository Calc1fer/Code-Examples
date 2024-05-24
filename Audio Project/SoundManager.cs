using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/*This script is one component of a two part system which will manage the sound in response to events that
 happen in game. This will communicate with audioSources and manage the playing of sounds.*/

[System.Serializable]
public class CustomVolume
{
    public bool boostVolume;
    public float volumeIncrease; //between 0 and 100
}

public class SoundManager : MonoBehaviour
{
    //Implement singleton pattern
    private static SoundManager instance;

    public static SoundManager Instance
    {
        get
        {
            return instance;
        }
    }
    
    /*Use this section for controlling the default volume properties of each sound
    clip controlled by the sound manager.
    Volume will be between 0 and 100 (divide by 100 since volume is between 0 and 1)*/
    [Header("Object References")] 
    [SerializeField] private GameObject Level;
    [SerializeField] private GameObject objective;
    
    [Header("Volume Properties")] 
    [SerializeField] private float relocateVol = 10f;
    [SerializeField] private float whispersVol = 20f;
    [SerializeField] private float playerRespawnVol = 100f;
    [SerializeField] private float playerWhistleVol = 42f;
    [SerializeField] private float whistleResponseVol = 75f;
    [SerializeField] private float playerLandVol = 50f;
    [SerializeField] private float playerJumpVol = 50f;
    [SerializeField] private float playerDropGuideVol = 50f;
    [SerializeField] private float playerGuideSoundVol = 20f;
    [SerializeField] private float playerWarningsVol = 10f; //Increase as player gets closer to the edge
    [SerializeField] private float relocateObjectiveVol = 50f;
    
    [Header("Sound Clips (for events)")] 
    [SerializeField] private AudioClip darkAmbience;
    [SerializeField] private AudioClip whispers;
    [SerializeField] private AudioClip playerRespawn;
    [SerializeField] private AudioClip playerWhistle;
    [SerializeField] private AudioClip whistleResponse;
    [SerializeField] private List<AudioClip> playerLand;
    [SerializeField] private AudioClip playerJump;
    [SerializeField] private AudioClip playerDropGuide;
    [SerializeField] private AudioClip playerGuideSound;
    [SerializeField] private List<AudioClip> playerWarnings;
    [SerializeField] private AudioClip relocateObjective;
    
    [Header("Delays")] 
    [SerializeField] private float whistleResponseDelay = 1f;

    [Header("Custom Volume Properties")] 
    [SerializeField] private CustomVolume playerRespawnClip;
    

    private float volumeScale = 100f;
    private float whispersVolIncrement = 5f;
    private float warningsVolIncrement = 10f;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    private void OnEnable()
    {
        //Subscribe to the events when the SoundManager is enabled
        EventHandler.Instance.OnPlayerReachedObjective += PlayerReachedObjective;
        EventHandler.Instance.OnPlayerRespawn += RespawnPlayer;
        EventHandler.Instance.OnPlayerWhistle += PlayerWhistle;
        EventHandler.Instance.OnPlayerLand += PlayerLand;
        EventHandler.Instance.OnPlayerJump += PlayerJump;
        EventHandler.Instance.OnPlayerDropGuide += PlayerDropGuide;
        EventHandler.Instance.OnPlayerWarning += PlayerWarnings;
        EventHandler.Instance.OnChangePlayerWarningVol += PlayerWarningVolChange;
    }

    private void OnDisable()
    {
        //Unsubscribe to the events when the SoundManager is disabled to avoid memory leaks
        EventHandler.Instance.OnPlayerReachedObjective -= PlayerReachedObjective;
        EventHandler.Instance.OnPlayerRespawn -= RespawnPlayer;
        EventHandler.Instance.OnPlayerWhistle -= PlayerWhistle;
        EventHandler.Instance.OnPlayerLand -= PlayerLand;
        EventHandler.Instance.OnPlayerJump -= PlayerJump;
        EventHandler.Instance.OnPlayerDropGuide -= PlayerDropGuide;
        EventHandler.Instance.OnPlayerWarning -= PlayerWarnings;
        EventHandler.Instance.OnChangePlayerWarningVol -= PlayerWarningVolChange;
    }
    
    //All sound control functions and response functions go here
    /*This function will play a sound indicating the player has reached the objective object and interacted with it*/
    private void PlayerReachedObjective(PlayerMovement playerMovement, GameObject objective, bool combine)
    {
        //Handle sound for the player reaching the objective
        AudioSource objectiveAudioSource = objective.transform.GetChild(1).GetComponent<AudioSource>();
        objectiveAudioSource.pitch = 1f;
        objectiveAudioSource.volume = relocateObjectiveVol / volumeScale;
        objectiveAudioSource.PlayOneShot(relocateObjective, relocateObjectiveVol / volumeScale);
        Debug.Log(relocateObjectiveVol / volumeScale);

        //Move the objective to a new location
        objective.GetComponent<RelocateObjective>().Relocate();
        
        if (combine)
        {
            //Trigger the whispering sounds
            Level.GetComponent<CombineSounds>().CombineSoundClips(whispers, whispersVol / volumeScale);
            
            //Increase volume for the next objective
            whispersVol += whispersVolIncrement;
        }

        if (!combine)
        {
            Level.GetComponent<AudioSource>().clip = darkAmbience;
            Level.GetComponent<AudioSource>().volume = 0.75f;
            Level.GetComponent<AudioSource>().Play();
        }
    }

    private void RespawnPlayer(GameObject player)
    {
        //Handle the oneshot sound for the player
        if (playerRespawnClip.boostVolume)
        {
            AudioClip respawnClip = IncreaseAmplitude(playerRespawn, playerRespawnClip.volumeIncrease);
            player.GetComponent<AudioSource>().PlayOneShot(respawnClip, playerRespawnVol / volumeScale);
        }
        else
        {
            player.GetComponent<AudioSource>().PlayOneShot(playerRespawn, playerRespawnVol / volumeScale);
        }
    }

    private void PlayerWhistle(GameObject player)
    {
        //Play the player whistle
        AudioSource audioSource = player.GetComponent<AudioSource>();
        float prevVolume = audioSource.volume;
        audioSource.volume = playerWhistleVol / volumeScale;
        audioSource.PlayOneShot(playerWhistle);
        
        //Execute the whistle response from the objective
        objective.GetComponent<WhistleResponse>().Response(whistleResponse, whistleResponseDelay, whistleResponseVol / volumeScale);
    }

    private void PlayerLand(GameObject player)
    {
        //Play a random clip for the player landing
        int randIdx = Random.Range(0, playerLand.Count);
        
        AudioSource audioSource = player.GetComponent<AudioSource>();
        float prevVolume = audioSource.volume;
        audioSource.volume = playerLandVol / volumeScale;
        audioSource.PlayOneShot(playerLand[randIdx]);
        
    }

    private void PlayerJump(GameObject player)
    {
        AudioSource audioSource = player.GetComponent<AudioSource>();
        float prevVolume = audioSource.volume;
        audioSource.volume = playerJumpVol / volumeScale;
        audioSource.PlayOneShot(playerJump);
    }

    private void PlayerDropGuide(SpawnGuide guide, Vector3 playerPos)
    {
        float volumeDrop = playerDropGuideVol / volumeScale;
        float volumeGuide = playerGuideSoundVol / volumeScale;

        AudioClip guideSound = IncreaseAmplitude(playerGuideSound, -50f);
        
        guide.SpawnGuideObject(playerPos, playerDropGuide, guideSound, volumeDrop, volumeGuide);
    }

    private void PlayerWarnings(GameObject player, bool onInvisibleGround)
    {
        /*Call player child objects Left and Right to play disembodied whispers
         warning them of being closer to the edge*/
        AudioSource[] audioSources = player.GetComponentsInChildren<AudioSource>();
        
        Debug.Log("Called");
        //Left is 0, Right is 1
        if (onInvisibleGround)
        {
            //Set the clip
            audioSources[1].clip = playerWarnings[Random.Range(2, playerWarnings.Count)];
            audioSources[2].clip = playerWarnings[Random.Range(0, playerWarnings.Count)];

            //Set the volume
            audioSources[1].volume = playerWarningsVol / volumeScale;
            audioSources[2].volume = playerWarningsVol / volumeScale;
            
            //Play the clip (Loop)
            audioSources[1].loop = true;
            audioSources[2].loop = true;

            audioSources[1].Play();
            audioSources[2].Play();
        }

        if (!onInvisibleGround)
        {
            audioSources[1].Stop();
            audioSources[2].Stop();
        }
    }

    private void PlayerWarningVolChange(GameObject player)
    {
        float raysHitOrMiss = player.GetComponent<PlayerMovement>().GetInvisibleRayHits();

        float volIncrement = warningsVolIncrement * raysHitOrMiss;
        AudioSource[] audioSources = player.GetComponentsInChildren<AudioSource>();
        
        float newVol = (playerWarningsVol + volIncrement) / volumeScale;
        Debug.Log(newVol);
        audioSources[1].volume = newVol;
        audioSources[2].volume = newVol;
        
        Debug.Log(audioSources[1].volume);
        Debug.Log(audioSources[2].volume);
    }
    
    private AudioClip IncreaseAmplitude(AudioClip clip, float volIncrease)
    {
        // Create a new AudioClip to avoid modifying the original
        AudioClip newClip = AudioClip.Create("playerRespawn", clip.samples, clip.channels, clip.frequency, false);

        // Get the data from the original clip
        float[] clipData = new float[clip.samples * clip.channels];
        clip.GetData(clipData, 0);

        // Get the new volume
        float volume = 1f + (volIncrease / volumeScale);

        // Apply the volume increase and set the data to the new AudioClip
        for (int i = 0; i < clipData.Length; i++)
        {
            clipData[i] *= volume;
        }
        newClip.SetData(clipData, 0);

        return newClip;
    }
}
