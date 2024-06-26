using Cinemachine.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NIThirdPersonController : MonoBehaviour
{
    [SerializeField] private AnimationStateController animator_ref;
    private Animator animator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CapsuleCollider capsule;
    [SerializeField] private PlayerControls controls;
    [SerializeField] private ThirdPersonCam main_cam_ref;
    [SerializeField] private Transform hand_hold;
    private GameObject pop_obj;
    Keyboard kb;
    InputAction hide_cursor;
    InputAction show_cursor;
    InputAction movement;
    InputAction jumping;
    InputAction running;
    InputAction crouching;
    InputAction push_or_pull;

    [SerializeField] private Transform orientation;
    [SerializeField] private Transform feet_pos;
    [SerializeField] private Transform head_pos;
    [SerializeField] private Transform wall_pos;
    [SerializeField] private Transform push_pos;

    private Vector3 input;
    private Vector3 move_dir;
    private Vector2 move_input;

    [SerializeField] private LayerMask is_ground;
    [SerializeField] private LayerMask is_ceiling;
    [SerializeField] private LayerMask is_a_wall;
    [SerializeField] private LayerMask is_a_pushable;
    [SerializeField] private LayerMask is_everything;

    private bool grounded;
    private bool is_roof;
    private bool is_wall;
    private bool is_pushable;
    private bool push;
    private bool is_running = false;
    private bool is_crouching = false;
    private bool is_jumping = false;
    private bool is_run_jump_ready = true;
    private bool is_standing_jump = false;
    private bool is_stand_jump_ready = true;
    private bool input_disabled = false;
    private bool hard_landing = false;
    private bool allow_jump = true;
    private bool artefact_collected;
    private bool pushing = false;
    private bool currently_pushing = false;
    private bool ready_to_push = false;

    [Header("Movement")]
    [SerializeField] private float ground_drag;
    [SerializeField] private float walk_speed;
    [SerializeField] private float run_speed;
    [Header("Jumping")]
    [SerializeField] private float stand_jump_force;
    [SerializeField] private float moving_jump_force;
    [SerializeField] private float air_multiplier;
    [Header("Capsule Properties")]
    [SerializeField] private float capsule_radius;
    [SerializeField] private float capsule_height;
    [SerializeField] private float capsule_centre;
    [SerializeField] private bool isClimbing = false;
    [Header("Collision")]
    [SerializeField] private float ray_radius = 0.2f;
    [SerializeField] private float ray_length = 0.2f;
    [SerializeField] private PhysicMaterial physic_mat;

    private float stand_jump_delay = 0.5f;  //Change depending on the animation used
    private float run_jump_delay;
    private float disabled_input_delay;
    private int score = 0;
    private float prev_capsule_height;
    private bool grounded_this_frame = false;
    private float temp_ray_length;
    private float temp_ray_radius;

    void Awake() 
    {
        //Assign the new input controller
        controls = new PlayerControls();
        kb = InputSystem.GetDevice<Keyboard>();
        animator = GetComponent<Animator>();
    }
    private void Start()
    {
        //Initialise necessary variables here for player startup
        temp_ray_length = ray_length;
        temp_ray_radius = ray_radius;

        //Set the capsule collider to variables that will change when crouching
        capsule.height = capsule_height;
        capsule.radius = capsule_radius;
        capsule.center = new Vector3(0f, capsule_centre, 0f);

        prev_capsule_height = capsule_height;

        //Freeze the rotation of the player indefinitely.
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void OnEnable() 
    {
        //Initialise the controls and assign to input actions
        controls.Enable();

        //Assign input actions here
        hide_cursor = controls.Player.HideCursor;
        hide_cursor.Enable();

        show_cursor = controls.Player.ShowCursor;
        show_cursor.Enable();
        
        if(!isClimbing)
        {
            jumping = controls.Player.Jump;
            jumping.Enable();
            
            movement = controls.Player.Move;
            movement.Enable();

            running = controls.Player.ToggleRun;
            running.Enable();

            crouching = controls.Player.Crouch;
            crouching.Enable();

            push_or_pull = controls.Player.PushPull;
            push_or_pull.Enable();

            jumping.started += _ => Jump();

            running.started += _ => ToggleRunning(_);
            running.canceled += _ => ToggleRunning(_);

            crouching.started += _ => ToggleCrouch(_);
            crouching.canceled += _ => ToggleCrouch(_);
        }

        //Use input actions to call necessary functions for player functionality

        hide_cursor.started += _ => HideCursor();
        show_cursor.started += _ => ShowCursor();
    }

    private void OnDisable() 
    {
        controls.Disable();
        movement.Disable();
        jumping.Disable();
        running.Disable();
    }

    // Update is called once per frame
    private void Update()
    {
#if UNITY_EDITOR
    if(Input.GetKeyDown(KeyCode.F1))
    {
            walk_speed = 400;
            run_speed = 600;
            stand_jump_force = 600;
            moving_jump_force = 600;
    }
#endif
        if (!isClimbing)
        {
            PlayerInput();
            
            //Function to hide cursor when playing
            HideCursor();

            hard_landing = animator_ref.GetHardLanding();

            //Check to see if the player is on the ground
            //bool floor = Physics.CheckSphere(feet_pos.position, 0.3f, is_ground);
            bool roof = Physics.CheckSphere(head_pos.position, 0.1f, is_ceiling);
            bool wall = Physics.CheckSphere(wall_pos.position, 0.2f, is_a_wall);
            push = Physics.CheckSphere(push_pos.position, 0.2f, is_a_pushable);

            CheckFloor();

                if (push)
                {
                  is_pushable = true;
                  //Debug.Log(is_pushable);
                }
                else if (!push)
                {
                  is_pushable = false;
                  //Debug.Log(is_pushable);
                }

            //Player will still be on ground but crouched, so allow for movement, but prevent the player from standing again.
            if(roof)
            {
                //grounded = true;
                is_roof = true;
            }
            else if(!roof)
            {
                //grounded = false;
                is_roof = false;

                //Reset the capsule to it's usual parameters when player is standing
                if(is_crouching)
                {
                    bool force_stop_crouch = kb.leftCtrlKey.isPressed;
                    if(!force_stop_crouch)
                    {
                        //ResetCapsuleCollider();
                        Debug.Log("First freeze");
                    }
                }
            }
            
            if(wall)
            {
                //StopPlayer();
                is_wall = true;
                
            }
            else if(!wall)
            {
                is_wall = false;
            }
            
            //Handle drag
            if (grounded && !hard_landing)
            {
                ray_length = temp_ray_length;
                //If the player speed is above the maximum velocity then call this function
                ControlPlayerVel();
                //Prevent moving in the air by preventing player input updates during a jump
                rb.drag = ground_drag;
                //is_stand_jump_ready = true;
                
                //If player is no longer jumping, reset the collider
                capsule.height = prev_capsule_height;

                //Deal with pushing and pulling of objects only on the floor
                if(is_pushable)
                {
                    if(pushing)
                    {
                        if(push_or_pull.triggered)
                        {
                            pushing = false;
                            if(pop_obj != null)
                            {
                                //Destroy the joint when player is no longer pushing
                                currently_pushing = false;
                                pop_obj.GetComponent<ObjectController>().ToggleJoint(pushing, rb);
                            }
                        }
                    }
                    else
                    {

                        if(push_or_pull.triggered)
                        {
                            pushing = true;
                        }
                    }
                }

                if(currently_pushing)
                {
                    Debug.Log("Stop Jumping");
                    allow_jump = false;
                }
            }
            else
            {
                rb.drag = 0f;
                ray_length = 0.1f;
            }

            //If the player falls and has a hard landing, disable the input
            if(hard_landing)
            {
                allow_jump = false;
                float disable_input = (animator_ref.GetHardLandAnimTime() / 1.5f) - 0.4f;
                
                StartCoroutine(NoRotation(disable_input));

            }
            else
            {
                if(!currently_pushing)
                {
                    allow_jump = true;
                }
            }
        }
        else
        {
            Debug.Log("Cancelled climbing");
        }
    }

    private void OnDrawGizmos() 
    {
        Vector3 ray_pos = new Vector3(feet_pos.position.x, feet_pos.position.y - 0.15f, feet_pos.position.z);
        float ray_length = 0.2f;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(ray_pos, Vector3.forward * ray_length);
        Gizmos.DrawRay(ray_pos, -Vector3.forward * ray_length);
        Gizmos.DrawRay(ray_pos, -Vector3.right * ray_length);
        Gizmos.DrawRay(ray_pos, Vector3.right * ray_length);

    }

    private void FixedUpdate()
    {
        //Call player movement
        if(!input_disabled)
        {
            PlayerMovement();
        }
    }

    private bool CheckForObstacles()
    {
        RaycastHit hit;
        float ray_length = 0.2f;
        Vector3 ray_pos = new Vector3(feet_pos.position.x, feet_pos.position.y -0.15f, feet_pos.position.z);

        Ray front_ray = new Ray(ray_pos, transform.TransformDirection(Vector3.forward));
        Ray back_ray = new Ray(ray_pos, -transform.TransformDirection(Vector3.forward));
        Ray left_ray = new Ray(ray_pos, -transform.TransformDirection(Vector3.right));
        Ray right_ray = new Ray(ray_pos, transform.TransformDirection(Vector3.right));

        if(Physics.Raycast(front_ray, out hit, ray_length, is_everything))
        {
            if(hit.rigidbody.gameObject.tag == "PushOrPull" || hit.rigidbody.gameObject.tag == "Ground")
            {
                LandOrFall(transform.forward);
                return true;
            }
        }
        if(Physics.Raycast(back_ray, out hit, ray_length, is_everything) || Physics.Raycast(left_ray, out hit, ray_length, is_everything) || Physics.Raycast(right_ray, out hit, ray_length, is_everything))
        {
            if(hit.rigidbody.gameObject.tag == "PushOrPull" || hit.rigidbody.gameObject.tag == "Ground")
            {
                LandOrFall(hit.normal);
                return true;
            }
        }
        return false;
    }

    private void LandOrFall(Vector3 move_dir)
    {
        Vector3 new_dir = new Vector3(move_dir.x, 5f, move_dir.z);
        //rb.AddForce(((move_dir * 5) + Vector3.down) * Time.fixedDeltaTime);
        rb.AddForce(new_dir * 10f, ForceMode.Impulse);
        //Debug.Log("Pushing away from obstacle");
    }

    private void CheckFloor()
    {   
        int num_rays = 8;
        float angle_interval = 360 / num_rays;
        Vector3[] ray_origins = new Vector3[num_rays];
        Vector3 centre_ray_pos = feet_pos.transform.position;

        bool any_hit = false;

        for(int i = 0; i < num_rays; i++)
        {
            //Get angle for this ray
            float angle = i * angle_interval;

            if(angle == 0)
            {
                //Calculate the direction for this ray based on the angle
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                //Calculate the origin of new ray based on centre ray
                Vector3 origin = centre_ray_pos + 0 * dir;

                ray_origins[i] = origin;
            }
            else
            {
                //Calculate the direction for this ray based on the angle
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                //Calculate the origin of new ray based on centre ray
                Vector3 origin = centre_ray_pos + ray_radius * dir;

                ray_origins[i] = origin;

            }
        }

        foreach (Vector3 origin in ray_origins)
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, Vector3.down, out hit, ray_length))
            {
                // if any ray hits the ground, the player is considered grounded
                grounded = true;
                any_hit = true;
                Debug.DrawRay(origin, Vector3.down * ray_length, Color.green);
            }
            else
            {
                Debug.DrawRay(origin, Vector3.down * ray_length, Color.red);
            }
        }

        if(!any_hit)
        {
            grounded = false;
        }
    }

    private void OnCollisionEnter(Collision other) 
    {
        if(other.gameObject.tag == "Pickup")
        {
            //Choose the ID to set to being picked up
            GameManager.SetArtefactCollected(gameObject.GetComponent<PickedUp>().GetID(), true);
            GameManager.SetHUBTravel(true);
            Destroy(other.gameObject);
        }
    }

    private void OnCollisionExit(Collision other) 
    {
        if(other.gameObject.GetComponent<ObjectController>() != null && pushing)
        {
                currently_pushing = false;
                pushing = false;
                pop_obj = other.gameObject;
                pop_obj.GetComponent<ObjectController>().ToggleJoint(pushing, rb);
                Debug.Log("No longer joined");
        }

        if(other.gameObject.tag == "Slope")
        {
            physic_mat.dynamicFriction = 0f;
            ray_radius = temp_ray_radius;
        }
    }

    private void OnCollisionStay(Collision other)
    {
        if(other.gameObject.GetComponent<ObjectController>() != null && pushing)
        {
            currently_pushing = true;
            pop_obj = other.gameObject;
            pop_obj.GetComponent<ObjectController>().ToggleJoint(pushing, rb);
            Debug.Log("We are one!");
        }
        else if(other.gameObject.GetComponent<ObjectController>() != null && !pushing)
        {
            currently_pushing = false;
            pop_obj = other.gameObject;
            pop_obj.GetComponent<ObjectController>().ToggleJoint(pushing, rb);
        }

        //Stop the player sliding on the slopes
        if(other.gameObject.tag == "Slope")
        {
            physic_mat.dynamicFriction = 1f;

            //Change radius of rays
            ray_length = 0.1f;
        }
    }

    private void PlayerInput()
    {
        if(!input_disabled)
        {
            //Newer input system reading input values from the player
            move_input = movement.ReadValue<Vector2>();
        }
    }

    private void PlayerMovement()
    {
        //Calculate movement direction
        move_dir = orientation.forward * move_input.y + orientation.right * move_input.x;
        
        //Debug rays for making sure the player moves in the correct direction
        Debug.DrawRay(orientation.position, orientation.forward * 2, Color.blue);
        Debug.DrawRay(orientation.position, orientation.right * 2, Color.red);
        Debug.DrawRay(rb.position, move_dir, Color.green);

        if (grounded && !input_disabled && !currently_pushing)
        {
            //If the player is walking
            if (!is_running)
            {
                rb.AddForce(move_dir.normalized * walk_speed * 10, ForceMode.Force);
            }

            //If the player is running
            if (is_running && !is_crouching)
            {
                rb.AddForce(move_dir.normalized * run_speed * 10, ForceMode.Force);
            }

            //If the player is trying to run while crouching
            if((is_running && is_crouching))
            {
                rb.AddForce(move_dir.normalized * walk_speed * 10, ForceMode.Force);
            }
        }
        else if((!grounded && is_jumping) && !input_disabled)
        {
            rb.AddForce(move_dir.normalized * (walk_speed / 2) * 5, ForceMode.Force);
        }
        else if(grounded && currently_pushing)
        {
           //Player is pushing or pulling (Doesn't work with just if pushing and running for some reason)
           rb.AddForce(move_dir.normalized * walk_speed * 10, ForceMode.Force);
        }
    }

    private void ControlPlayerVel()
    {
        Vector3 current_vel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);


        //Limit the velocity if above the threshold 
        if(current_vel.magnitude > walk_speed)
        {
            Vector3 limit_vel = current_vel.normalized * walk_speed;
            rb.velocity = new Vector3(limit_vel.x, rb.velocity.y, limit_vel.z);
        }
        if (current_vel.magnitude > run_speed)
        {
            Vector3 limit_vel = current_vel.normalized * run_speed;
            rb.velocity = new Vector3(limit_vel.x, rb.velocity.y, limit_vel.z);
        }
    }

    //Stop the player rotating when standing jumping
    private void NoRotationDelay()
    {
        is_standing_jump = false;
    }

    //Function for jumping
    private void Jump()
    {
        if(!is_crouching)
        {
            rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
        }

        if(grounded && !is_crouching && allow_jump && !input_disabled)
        {
            is_jumping = true;
            //Get delay from the animator
            run_jump_delay = animator_ref.GetRunJumpAnimTime() - 0.2f;

            //Reset the player y-velocity to 0 so they player jumps to the same height every time
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            //Apply the force once using impulse
            rb.AddForce(0f, moving_jump_force, 0f, ForceMode.Impulse);

            animator.SetBool("IsJumping", true);
            animator_ref.SetJump(true);

            allow_jump = false;
            StartCoroutine(DelayRunJump(run_jump_delay));
        }
    }

    private IEnumerator NoRotation(float duration)
    {
        rb.velocity = Vector3.zero;
        input_disabled = true;
        main_cam_ref.setLockRotation(true);
        yield return new WaitForSeconds(duration);

        input_disabled = false;
        main_cam_ref.setLockRotation(false);
        animator_ref.SetHardLanding(false);
    }

    public void DisableJump()
    {
        allow_jump = false;
    }

    //Prevents the player jumping immediately again after landing
    private IEnumerator DelayRunJump(float duration)
    {
        yield return new WaitForSeconds(duration);
        allow_jump = true;
        is_jumping = false;
    }

    private void DelayedJump()
    {
        if(grounded)
        {
            rb.AddForce(transform.up * stand_jump_force, ForceMode.Impulse);
            is_stand_jump_ready = true;
        }
    }

    private IEnumerator DisableInput(float duration)
    {
        input_disabled = true;
        allow_jump = false;

        if (hard_landing)
        {
            animator_ref.SetHardLanding(false);
        }

        rb.velocity = Vector3.zero;;

        float prev_mag = rb.velocity.magnitude;

        yield return new WaitForSeconds(duration);

        while(rb.velocity.magnitude > 0.1f)
        {
            yield return null;
            if(rb.velocity.magnitude > prev_mag)
            {
                //Player is still moving so reset the timer
                prev_mag = rb.velocity.magnitude;
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                //Player is slowing down or is stationary
                prev_mag = rb.velocity.magnitude;
                yield return null;
            }

        }
        
        //Reset capsule properties after landing has finished
        if(hard_landing)
        {
            Debug.Log("Second Freeze");
        }
        input_disabled = false;
        allow_jump = true;
    }

    //Hide cursor function
    public void HideCursor()
    {
        if (!GameManager.GetPauseCursor())
        {
            //Hides cursor while playing
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    //Show the cursor
    public void ShowCursor()
    {
        if (GameManager.GetPauseCursor())
        {
            //Shows cursor while playing
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    //Function for determining whether the player wants to jump or not
    private void ToggleRunning(InputAction.CallbackContext run)
    {
        // if(grounded)
        // {
            //If the player has pressed the run button in addition to moving
            if(run.started)
            {
                is_running = true;
            }
            if(run.canceled)
            {
                is_running = false;
            }
        //}
    }

    //Function for determining whether the player wants to crouch or not
    private void ToggleCrouch(InputAction.CallbackContext crouch)
    {
        #region ToggleCrouchOG
        //if (grounded && !input_disabled)
        //{
        //    if(crouch.started)
        //    {
        //        is_crouching = true;
        //        //capsule.height = capsule_height / 2;
        //        //capsule.center = new Vector3(0f, capsule_centre / 2, 0f);
        //        //capsule.radius = capsule_radius * 2;
        //    }
        //    if(crouch.canceled && !is_roof)
        //    {
        //        is_crouching = false;
        //        //capsule.height = capsule_height;
        //        //capsule.center = new Vector3(0f, capsule_centre, 0f);
        //        //capsule.radius = capsule_radius;
        //    }
        //}
        #endregion

        if (grounded)
        {
            if (crouch.started)
            {
                is_crouching = true;
                //capsule.height = capsule_height / 2;
                //capsule.center = new Vector3(0f, capsule_centre / 2, 0f);
                //capsule.radius = capsule_radius * 2;
            }
            if (crouch.canceled && !is_roof)
            {
                is_crouching = false;
                //capsule.height = capsule_height;
                //capsule.center = new Vector3(0f, capsule_centre, 0f);
                //capsule.radius = capsule_radius;
            }
        }
    }

    private void ResetCapsuleCollider()
    {

        is_crouching = false;
        capsule.height = capsule_height;
        capsule.center = new Vector3(0f, capsule_centre, 0f);
        capsule.radius = capsule_radius;
        
        //Use bit mask to stop the player pinging into the air when moving the centre of gravity
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
    }

    private void HardLandCapsuleCollider()
    {
        capsule.height = capsule_height / 2;
        capsule.center = new Vector3(0f, capsule_centre / 2, 0f);
        capsule.radius = capsule_radius * 2;
    }

    //Getters relevant for passing variables into the animator controller
    public bool GetInput()
    {
        return input_disabled;
    }

    public Vector3 GetRBVelocity(Vector3 vel)
    {
        vel = rb.velocity;
        return vel;
    }

    public bool GetGrounded()
    {
        return grounded;
    }

    public bool GetIsStandingJump()
    {
        return is_standing_jump;
    }

    public bool GetRunningJumping()
    {
        if(!grounded)
        {
            return true;
        }

        return false;
    }

    public bool GetJumping()
    {
        return is_jumping;
    }

    public Vector2 GetPlayerInput()
    {
        return move_input;
    }

    public bool GetCrouching()
    {
        return is_crouching;
    }

    public bool GetPushOrPull()
    {
        return pushing;
    }

    public bool GetRunning()
    {
        return is_running;
    }

    public bool GetAllowJump()
    {
        return allow_jump;
    }

    public bool GetIsClimbing()
    {
        return isClimbing;
    }

    public bool GetArtefactCollected()
    {
        return artefact_collected;
    }

    public Transform GetHold()
    {
        return hand_hold;
    }

    //Setters
    public void SetArtefactCollected(bool val)
    {
        artefact_collected = val;
    }
    public void SetIsClimbing(bool change)
    {
        isClimbing = change;
    }

    public void SetInput(bool val)
    {
        input_disabled = val;
    }

    public void setPushing(bool val)
    {
        pushing = val;
    }

    public bool GetHardLanding()
    {
        return hard_landing;
    }
}
