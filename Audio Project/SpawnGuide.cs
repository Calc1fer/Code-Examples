using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*This script will control the spawning of the guide pieces that the player
 can drop in order to see where they have been and will be helpful for navigating 
 the space. This is persistent so if the player falls the objects will still be there.*/
public class SpawnGuide : MonoBehaviour
{
    [SerializeField] private GameObject guidePiece;
    [SerializeField] private Material material;
    private AudioSource audioSource;
    private GameObject guide;
    private SimpleAttenuation simpleAttenuation;
    
    public void SpawnGuideObject(Vector3 playerPos, AudioClip playerDropGuide, AudioClip playerGuideSound,float volumeDrop, float volumeGuide)
    {
        Vector3 pos = new Vector3(playerPos.x, playerPos.y - 0.2f, playerPos.z);
        guide = Instantiate(guidePiece, pos, Quaternion.identity);
        audioSource = guide.GetComponentInChildren<AudioSource>();

        ParticleSystem particleSystem = guide.GetComponentInChildren<ParticleSystem>();
        
        // Set a random colour for the guide piece
        SetRandomColour(particleSystem);
        
        //Play the audio
        StartCoroutine(PlaySoundsInOrder(playerDropGuide, playerGuideSound, volumeDrop, volumeGuide));
    }

    private void SetRandomColour(ParticleSystem particleSystem)
    {
        float[] rgb = new float[3];
        for (int i = 0; i < rgb.Length; i++)
        {
            rgb[i] = Random.value;
        }
        
        Debug.Log($"first: {rgb[0]}, second: {rgb[1]}, third: {rgb[2]}");

        Material newMaterial = new Material(material);
        
        //Set the material colour
        Renderer renderer = guide.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            newMaterial.color = new Color(rgb[0] * 10, rgb[1] * 10, rgb[2] * 10);
            renderer.material = newMaterial;
            
            // Set the emission color
            newMaterial.EnableKeyword("_EMISSION");
            newMaterial.SetColor("_EmissionColor", new Color(rgb[0] * 100, rgb[1] * 100, rgb[2] * 100));
            
        }
        else
        {
            Debug.Log("Sadly this wont play nice!!");
        }
        
        // Get the main module component
        var mainModule = particleSystem.main;

        // Set a random colour for the guide piece
        mainModule.startColor = new Color(rgb[0], rgb[1], rgb[2]);
        
    }

    private IEnumerator PlaySoundsInOrder(AudioClip playerDropGuide, AudioClip playerGuideSound,float volumeDrop, float volumeGuide)
    {
        simpleAttenuation = guide.GetComponentInChildren<SimpleAttenuation>();
        simpleAttenuation.SetExternalVolume(volumeDrop);
        audioSource.PlayOneShot(playerDropGuide, volumeDrop);

        yield return new WaitForSeconds(0.5f);
        
        simpleAttenuation.SetExternalVolume(volumeGuide);
        audioSource.clip = playerGuideSound;
        audioSource.Play();
    }
    
    public GameObject GetGuide()
    {
        return guide;
    }
}
