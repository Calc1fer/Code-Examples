using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerMovement : MonoBehaviour
{
    private static PlayerMovement instance;
    public static PlayerMovement Instance
    {
        get
        {
            return instance;
        }
    }
    
    [Header("Player Movement Properties")]
    [SerializeField] private float walkVel;
    [SerializeField] private float runVel;
    [SerializeField] private float jumpForce = 50f;
    [SerializeField] private float jumpDelay = 1f;
    [SerializeField] private float whistleDelay = 3f;
    [SerializeField] private Transform orientation;
    [SerializeField] private int guidePieces = 100;
    
    [Header("Player Collision Properties")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float rayRadius = 0.2f;
    [SerializeField] private float rayLength = 0.2f;
    [SerializeField] private float invisibleRayRadius = 0.5f;

    [Header("Cheats objects")] 
    [SerializeField] private GameObject objective;
    
    private Vector2 moveInput;
    private Vector3 moveDir;
    private Rigidbody rb;
    
    private InputManager inputManager;
    private EventHandler eventHandler;
    private UIManager uiManager;
    
    private bool grounded = false;
    private bool onInvisibleGround = false; //Will be true if player is on an invisible pathway
    private bool invisibleEvent = false;
    private bool canJump = true;
    private bool canWhistle = true;
    private bool checkLanded = false;
    private bool metObjectives = false;
    
    private int invisibleRayHits = 0;
    private bool[] invisibleRayArr;
    private bool[] prevInvisibleRayArr;
    private int totalRayChanges = 0;
    private bool canCallVolChange = false;
    private bool toggleInstructions = true;

    private int objectiveCount = 0;
    
    private float muffleFactor = 1000f; //depending on how many rays miss the invisible pathway, this value will increase or decrease.

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        inputManager = InputManager.Instance;
        eventHandler = EventHandler.Instance;
        uiManager = UIManager.Instance;
        invisibleRayArr = new bool[12];
        prevInvisibleRayArr = new bool[invisibleRayArr.Length];

        for (int i = 0; i < invisibleRayArr.Length; i++)
        {
            invisibleRayArr[i] = false;
        }
        
        Array.Copy(invisibleRayArr, prevInvisibleRayArr, invisibleRayArr.Length);
    }

    private void Update()
    {
        moveInput = inputManager.GetPlayerMoveInput();

        if (grounded)
        {
            Whistle();
            
            if (inputManager.GetIsPlayerJumping() && canJump)
            {
                Jump();
            }

            if (inputManager.GetTurnOnTheBrightLights())
            {
                //Turn on the lights if the player so desires
                eventHandler.TurnOnLights();
            }

            if (inputManager.GetIsGuidePressed() && guidePieces > 0)
            {
                //Call event handler to spawn a new guide so the player knows
                //where they have been
                eventHandler.PlayerDropGuide(gameObject);
                guidePieces--;
            }
        }

        if (!grounded && !checkLanded)
        {
            //Variables for checking when the player has landed.
            checkLanded = true;
            CheckHasLanded();
        }

        if (!metObjectives && objectiveCount == 3)
        {
            metObjectives = true;
            uiManager.EnableInteract(false);
        }

        if (inputManager.GetInstructions())
        {
            toggleInstructions = !toggleInstructions;
            
            if(toggleInstructions) uiManager.ShowInstructions(true);
            if(!toggleInstructions) uiManager.ShowInstructions(false); 
        }

        if (inputManager.GetRelocate())
        {
            Vector3 newPos = objective.transform.position;
            gameObject.transform.position = new Vector3(newPos.x, newPos.y, newPos.z - 1f);
        }
    }

    private void FixedUpdate()
    {
        GroundCheck();
        CheckForInvisibleGround();
        PlayerMove();

        if (grounded)
        {
            ControlPlayerVel();
        }
        
        //If the player has fallen off the edge of the level respawn them
        if(rb.velocity.y < -10f) eventHandler.RespawnPlayer(gameObject);
    }

    private void Jump()
    {
        //prevent the player from repeatedly jumping
        canJump = false;
        rb.AddForce(rb.velocity.x, jumpForce * 10f, rb.velocity.z, ForceMode.Force);
        
        //Call event for jumping
        eventHandler.PlayerJump(gameObject);
        
        StartCoroutine(DelayJump());
    }

    private void CheckHasLanded()
    {
        StartCoroutine(DelayLandCheck());
    }
    
    private void PlayerMove()
    {
        //Calculate movement - walk in direction player is looking
        moveDir = orientation.forward * moveInput.y + orientation.right * moveInput.x;
        
        if (grounded && !inputManager.GetIsPlayerRunning())
        {
            rb.AddForce(moveDir.normalized * walkVel * 10f, ForceMode.Force);
            
            if(rb.velocity.y > 0) rb.AddForce(Vector3.down * 100f, ForceMode.Force); 
        }

        if (grounded && inputManager.GetIsPlayerRunning())
        {
            rb.AddForce(moveDir.normalized * runVel * 10f, ForceMode.Force);
            
            if(rb.velocity.y > 0) rb.AddForce(Vector3.down * 250f, ForceMode.Force); 
        }
    }

    public void Whistle()
    {
        if (objectiveCount == 3) return;
        //Call event in handler
        if (inputManager.GetIsPlayerWhistling() && canWhistle)
        {
            eventHandler.PlayerWhistle(gameObject);

            StartCoroutine(DelayWhistle());
            canWhistle = false;
        }
    }
    
    private void ControlPlayerVel()
    {
        Vector3 currentVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        Vector3 limitVel = Vector3.zero;
        float velocityLerpSpeed = 1f;
        
        //Limit the velocity if above the threshold
        if (currentVel.magnitude > walkVel)
        {
            limitVel = currentVel.normalized * walkVel;
        }

        if (currentVel.magnitude > runVel)
        {
            limitVel = currentVel.normalized * runVel;
        }
        
        // Gradually adjust the velocity towards the limited value
        rb.velocity = Vector3.Lerp(rb.velocity, new Vector3(limitVel.x, rb.velocity.y, limitVel.z), Time.fixedDeltaTime * velocityLerpSpeed);
    }
    
    private void GroundCheck()
    {
        int numRays = 12;
        float angleInterval = 360 / numRays;
        Vector3[] rayOrigins = new Vector3[numRays];
        Vector3 centreRay_Pos = groundCheck.transform.position;

        bool anyHit = false;

        for(int i = 1; i < numRays; i++)
        {   
            //Get angle for this ray
            float angle = i * angleInterval;

            if(angle == 0)
            {
                //Calculate the direction for this ray based on the angle
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                //Calculate the origin of new ray based on centre ray
                Vector3 origin = centreRay_Pos + 0 * dir;

                rayOrigins[i] = origin;
            }
            else
            {
                //Calculate the direction for this ray based on the angle
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                //Calculate the origin of new ray based on centre ray
                Vector3 origin = centreRay_Pos + rayRadius * dir;

                rayOrigins[i] = origin;
                // Debug.DrawRay(origin, Vector3.down * ray_length, Color.green);

            }
        }

        foreach (Vector3 origin in rayOrigins)
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, Vector3.down, out hit, rayLength))
            {
                // if any ray hits the ground, the player is considered grounded
                if(hit.collider.gameObject.tag != "Player")
                {
                    grounded = true;
                    anyHit = true;
                    Debug.DrawRay(origin, Vector3.down * rayLength, Color.green);
                }
            }
            else
            {
                Debug.DrawRay(origin, Vector3.down * rayLength, Color.red);
            }
        }

        if(!anyHit)
        {
            grounded = false;
        }
    }
    
    public void CheckForInvisibleGround()
    {
        int numRays = 12;
        float angleInterval = 360 / numRays;
        Vector3[] rayOrigins = new Vector3[numRays];
        Vector3 centreRay_Pos = groundCheck.transform.position;
        string invisiblePathTag = "InvisibleGround";

        float muffleIncrement = 100f;
        bool anyHit = false;
        bool rayMiss = false; //If any ray misses then change the value of the muffle factor

        for(int i = 1; i < numRays; i++)
        {   
            //Get angle for this ray
            float angle = i * angleInterval;

            if(angle == 0)
            {
                //Calculate the direction for this ray based on the angle
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                //Calculate the origin of new ray based on centre ray
                Vector3 origin = centreRay_Pos + 0 * dir;
                rayOrigins[i] = origin;
            }
            else
            {
                //Calculate the direction for this ray based on the angle
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                //Calculate the origin of new ray based on centre ray
                Vector3 origin = centreRay_Pos + invisibleRayRadius * dir;
                rayOrigins[i] = origin;

            }
        }

        int index = -1; //for keeping track of the rays that hit or miss
        
        foreach (Vector3 origin in rayOrigins)
        {
            index++;
            
            RaycastHit hit;
            if (Physics.Raycast(origin, Vector3.down, out hit, rayLength))
            {
                // if any ray hits the invisible surface then set to true
                if(hit.collider.gameObject.CompareTag(invisiblePathTag))
                {
                    anyHit = true;
                    Debug.DrawRay(origin, Vector3.down * rayLength, Color.green);
                    
                    onInvisibleGround = true;

                    //If the ray is colliding with invisible floor then increase the muffleFactor (essentially reducing the filter effect)
                    if (muffleFactor is >= 100f and < 1000f) muffleFactor += muffleIncrement;
                    if (muffleFactor < 100f) muffleFactor = 100f;
                    if (muffleFactor > 1000f) muffleFactor = 1000f;

                    invisibleRayArr[index] = true; //keep track of the rays
                }
            }
            else
            {
                Debug.DrawRay(origin, Vector3.down * rayLength, Color.red);
                
                //Reduce the factor, essentially increasing the effect
                if (muffleFactor is <= 1000f and > 100f) muffleFactor -= muffleIncrement;
                
                if (muffleFactor < 100f) muffleFactor = 100f;
                if (muffleFactor > 1000f) muffleFactor = 1000f;

                invisibleRayArr[index] = false;
            }
        }
        
        //Check for the event
        if (onInvisibleGround && !invisibleEvent)
        {
            invisibleEvent = true;
            eventHandler.PlayerWarning(gameObject, onInvisibleGround);
        }
        else if (!onInvisibleGround && invisibleEvent)
        {
            invisibleEvent = false;
            eventHandler.PlayerWarning(gameObject, onInvisibleGround);
        }
        
        if(!anyHit)
        {
            onInvisibleGround = false;
            muffleFactor = 0f;
        }
        
        CheckArrays();
        
        //If there has been a change, then we can call the event once
        if (canCallVolChange)
        {
            eventHandler.PlayerWarningVolChange(gameObject);
            canCallVolChange = false;
        }
    }
    
    /*Function to compare array indexes for the invisible rays*/
    private void CheckArrays()
    {
        for(int i = 0; i < invisibleRayArr.Length; i++)
        {
            //Compare each value. If there is a change then increment or decrement
            //depending on whether value is true or false
            if (invisibleRayArr[i] != prevInvisibleRayArr[i])
            {
                if (invisibleRayArr[i])
                {
                    totalRayChanges--;
                    canCallVolChange = true;
                }

                if (!invisibleRayArr[i])
                {
                    totalRayChanges++;
                    canCallVolChange = true;
                }

                if (totalRayChanges < 0) totalRayChanges = 0;
            }
        }

        Array.Copy(invisibleRayArr, prevInvisibleRayArr, invisibleRayArr.Length);
    }
    
    //Collisions   
    private void OnTriggerStay(Collider other)
    {
        //Activate the text prompt
        uiManager.EnableInteract(true);
        
        //If the player is interacting with the object when in the 
        //trigger box then call reached objective event
        if (!inputManager.GetIsPlayerInteract()) return;
        
        if (other.gameObject.CompareTag("Objective"))
        {
            objectiveCount ++;
            bool combine = false;
            if (objectiveCount == 3) combine = true;
            eventHandler.PlayerReachedObjective(this, other.gameObject, combine);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.CompareTag("Objective")) uiManager.EnableInteract(false);
    }

    //Timers
    private IEnumerator DelayJump()
    {
        yield return new WaitForSeconds(jumpDelay);
        
        canJump = true;
    }

    private IEnumerator DelayLandCheck()
    {
        yield return new WaitWhile(GetIsGrounded);
        yield return new WaitUntil(GetIsGrounded);
        
        //Call the event manager for player landing
        checkLanded = false;
        eventHandler.PlayerLand(gameObject);
    }
    
    private IEnumerator DelayWhistle()
    {
        yield return new WaitForSeconds(whistleDelay);

        canWhistle = true;
    }
     public bool GetIsGrounded()
     {
         return grounded;
     }

     public bool GetOnInvisibleGround()
     {
         return onInvisibleGround;
     }

     public float GetMuffleFactor()
     {
         return muffleFactor;
     }

     public int GetInvisibleRayHits()
     {
         return totalRayChanges;
     }
}
