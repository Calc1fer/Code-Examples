using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Controller : MonoBehaviour
{
    [SerializeField] private Transform target_transform;
    [SerializeField] private Material win_material;
    [SerializeField] private Material lose_material;
    [SerializeField] private MeshRenderer floor_meshrenderer;
    [SerializeField] private ParameterManager params_ref;
    public Transform respawn_point;
    public bool respawn = false;
    public InputMaster controls;
    public Transform orientation;
    public LayerMask is_ground;
    public Transform ground_pos;
    private bool grounded;
    public float speed;
    public float air_multiplyer;
    public float jump_force;
    private Vector2 move_input;
    public Rigidbody rb;
    private bool is_moving = false;

    private float horizontal_input;
    private float vertical_input;

    private Vector3 move_dir;
    InputAction movement;
    InputAction jumping;
    InputAction pausing;
    Keyboard kb;
    private float score = 0f;
    private float distance_to_target = 0f;
    private bool started = false;
    private float smallest_distance = 0f;
    private float prev_distance = 0f;
    private bool is_reset;
    private bool game_started = false;
    private float tot_time_taken = 0f;
    private bool is_paused = false;
    private bool has_won = false;
    private bool has_lost = false;
    private bool sent_speed = false;
    private bool sent_score = false;
    private bool sent_time = false;

    void Awake() 
    {
        controls = new InputMaster();
        kb = InputSystem.GetDevice<Keyboard>();
    }

    private void ResetGame()
    {
        game_started = false;
        prev_distance = 0f;
        smallest_distance = 0f;
        distance_to_target = 0f;
        tot_time_taken = 0f;
        score = 0f;

        //Reset the player position to the correct place
        //transform.localPosition = new Vector3(0, 1.77f, -33.05f);
        started = false;
        is_reset = false;

        if(respawn)
        {
            transform.position = respawn_point.position;
            respawn = false;

            GameManager.setWin(false);
            GameManager.setLose(false);
        }
    }

    private void OnEnable() 
    {
        //Initialise the controls and assign the input actions
        controls.Enable();

        //Assign input actions here
        jumping = controls.Player.Jump;
        movement = controls.Player.Movement;
        pausing = controls.Player.Pause;

        //Enable Input actions
        jumping.Enable();
        movement.Enable();
        pausing.Enable();



        jumping.started += _ => Jump();
        movement.started += _ => PlayerMovement();
    }

    private void OnDisable()
    {
        controls.Disable();

        jumping.Disable();
        movement.Disable();
        pausing.Disable();
    }

    // Update is called once per frame
    private void Update()
    {   
        //Check if the game is paused (toggle)
        if(is_paused)
        {
            if(pausing.triggered)
            {
                is_paused = false;
                GameManager.setPaused(false);
                Time.timeScale = 1f;
            }
        }
        else
        {
            if(pausing.triggered)
            {
                is_paused = true;
                GameManager.setPaused(true);
                Time.timeScale = 0f;
            }
        }

        //Check that the player score has not gone below zero otherwise, reset the game
        if(score <= -0.1f)
        {
            is_reset = true;
            respawn = true;

            GameManager.setLose(true);
            GameManager.setMove(false);
        }

        //Always check if the game has been won
        if(GameManager.getWin() || GameManager.getLose() || !GameManager.getMove())
        {
            SetParams();
            ResetGame();
            is_reset = true;
            respawn = true;
        }

        if(GameManager.getMove())
        {
            //Time the player so the agent can take this into account
            tot_time_taken += Time.deltaTime;

            //If the player starts to move then the game's begun and the agent can move
            //We want this called only once
            if(rb.velocity.z > 0.3f && !game_started)
            {
                game_started = true;
            }

            //Check if the player is grounded
            bool is_floor = Physics.CheckSphere(ground_pos.position, 0.2f, is_ground);

            //If the player is touching the ground then they can move
            if(is_floor)
            {
                grounded = true;
            }
            else
            {
                grounded = false;
            }

            PlayerInput();
            
            //Player can move while grounded is true, so capture the input
            if(grounded)
            {
                SpeedThreshold();
            }
            else
            {
                //Else the player cannot move and drag is set to 0
                rb.drag = 0f;
            }
        }
        else
        {
            rb.velocity = new Vector3(0,0,0);
        }

        HideCursor();
    }

    private void FixedUpdate() 
    {
        //Call player movement. Update in relation to frame rate
        PlayerMovement();    
    }

    void PlayerInput()
    {
        move_input = movement.ReadValue<Vector2>();
    }

    void PlayerMovement()
    {       
        move_dir = orientation.forward * move_input.y + orientation.right * move_input.x;

        if(grounded)
        {
            rb.AddForce(move_dir.normalized * speed * 10, ForceMode.Force);
        
            // Debug.DrawRay(orientation.position, orientation.forward * 2, Color.blue);
            // Debug.DrawRay(orientation.position, orientation.right * 2, Color.red);
            // Debug.DrawRay(rb.position, move_dir, Color.green);
        }
        else if(!grounded)
        {
            rb.AddForce(move_dir.normalized * speed * 5f * air_multiplyer, ForceMode.Force);
        }
    }

    void SpeedThreshold()
    {
        Vector3 current_vel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        //Limit the velocity if over the threshold
        if(current_vel.magnitude > speed)
        {
            Vector3 limit_vel = current_vel.normalized * speed;
            rb.velocity = new Vector3(limit_vel.x, rb.velocity.y, limit_vel.z);
        }
    }

    void Jump()
    {
        //Debug.Log("Player has jumped");

        if(grounded)
        {
            //Reset the y component so the player jumps to the same height every time
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            //Apply the force to the player for jumping
            rb.AddForce(transform.up * (jump_force * 10), ForceMode.Force);
        }
    }

    public Vector2 GetPlayerInput()
    {
        return move_input;
    }

    //Collisions for the player
    private void OnTriggerEnter(Collider other) 
    {
        if(other.gameObject.tag == "Goal")
        {
            score += 20f;

            has_won = true;

            floor_meshrenderer.material = win_material;
            GameManager.setWin(true);            
            GameManager.setMove(false);
        } 
    }

    private void OnCollisionStay(Collision other) 
    {   
        //Reward player for getting closer to the goal
        if(other.gameObject.tag == "Ground")
        {
            distance_to_target = Vector3.Distance(transform.localPosition, target_transform.localPosition);

            if(!started)
            {
                smallest_distance = distance_to_target;
                started = true;
            }

            //Reward the player only once for getting closer to goal.
            //If they move away then closer, no points will be awarded
            if(distance_to_target < smallest_distance)
            {
                smallest_distance = distance_to_target;

                if(distance_to_target < prev_distance)
                {
                    score += 1f;
                }
            }

            prev_distance = distance_to_target;
        }    

        if(other.gameObject.tag == "Wall")
        {
            score -= 10f;

            floor_meshrenderer.material = lose_material;

            //params_ref.setPlayerHasWon(false);
            has_lost = true;

            //Reset the game if the player touches the wall
            GameManager.setLose(true);
            GameManager.setMove(false);
        }   

        //Deduct points for making contact with an obstacle
        if(other.gameObject.tag == "Obstacle")
        {
            Debug.Log("Hurtiiiing meeee obstacle");
            score -= 0.05f;
        }

        if(other.gameObject.tag == "JumpObstacle")
        {
            Debug.Log("Hurtiiiing meeee jumpobstacle");
            score -= 0.05f;
        }
    }

    public bool isReset()
    {
        return is_reset;
    }

    private void SetParams()
    {
        //All parameters the param manager requires will be sent from here since there is a problem with values being reset when the level resets.
        if(has_won)
        {
            params_ref.setPlayerWins(1);
            sent_score = true;
            sent_time = true;
            sent_speed = true;
            has_won = false;
        }
        else if(has_lost)
        {
            params_ref.setAgentWins(1);
            sent_score = true;
            sent_time = true;
            sent_speed = true;
            has_lost = false;
        }

        if(sent_score)
        {
            params_ref.setPlayerScore(score);
            sent_score = false;
        }

        if(sent_speed)
        {
            params_ref.setPlayerSpeed(speed);
            sent_speed = false;
        }

        if(sent_time)
        {
            params_ref.setPlayerTime(tot_time_taken);
            sent_time = false;
        }

    }

    public void setReset(bool val)
    {
        is_reset = val;
    }

    public void setSpeed(float val)
    {
        speed = val;
    }

    public bool GameStart()
    {
        return game_started;
    }

    public float getScore()
    {
        return score;
    }

    public float getSpeed()
    {
        return speed;
    }

    public float getTime()
    {
        return tot_time_taken;
    }

    private void HideCursor()
    {
        //Make cursor disappear while playing game
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}