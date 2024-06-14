using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

//Inherit from Agents
public class MoveToGoalAgent : Agent
{
    private float distance_to_target;
    private float max_distance_target;
    private float prev_distance = 0f;
    private float smallest_distance;
    private float threshold_distance = 5f;
    private bool started = false;
    private bool is_reset = false;
    private bool is_jumping = false;
    private float jump_force = 3f;
    [SerializeField]private Rigidbody rb;

    private float epsilon = 0f;
    private System.Random random = new System.Random();

    [SerializeField] private Transform target_transform;
    [SerializeField] private Material win_material;
    [SerializeField] private Material lose_material;
    [SerializeField] private MeshRenderer floor_meshrenderer;
    [SerializeField] private float speed;
    [SerializeField] private LayerMask wall_layer_mask;
    [SerializeField] private Player_Controller player_ref;
    [SerializeField] private ParameterManager params_ref;
    private float total_deduction = 0f;
    private float avoid_bonus = 0f;
    private float move_x = 0f;
    private float move_z = 0f;
    private float move_y = 0f;
    private float avoid_distance = 2f;
    private float wall_check_angle = 90f;
    private float avoid_speed;
    private float start_score = 0f;

    private int num_rays = 8;
    private float angle_increment = 0f;
    private float inference_score = 0f;
    private float tot_time_taken = 0f;
    private bool has_won = false;
    private bool has_lost = false;
    private bool sent_speed = false;
    private bool sent_score = false;
    private bool sent_time = false;

    private int tot_trials_size = 0;
    private int successful_trials = 0;
    private int[] trials_arr;
    private int losses = 0;
    private int wins = 0;

    public override void OnEpisodeBegin()
    {
        //Can reset parameters back to starting state
        //Angle between each ray for checking the agent surroundings

        angle_increment = 180f / num_rays;
        avoid_speed = speed + 2f;   //Avoidance speed must always be greater than the agent speed
        
        started = false;
        start_score = 0f;
        inference_score = 0f;
        tot_time_taken = 0f;
        successful_trials = 0;
        transform.localPosition = new Vector3(0f ,1, -33);
        
        GameManager.setWin(false);
        GameManager.setLose(false);
        GameManager.setMove(true);

        max_distance_target = Vector3.Distance(transform.localPosition, target_transform.localPosition);

        //To calculate the success rate of the agent
        tot_trials_size = Mathf.RoundToInt(max_distance_target);

        //Initialise the array size
        trials_arr = new int[tot_trials_size];

        for(int i = 0; i < trials_arr.Length; i++)
        {
            //Initialise all indexes to 0
            trials_arr[i] = 0;
        }
    }

    private void Update() 
    {
        //Always check if the game has been won
        if(GameManager.getWin() || GameManager.getLose() || !GameManager.getMove())
        {
            SetParams();
            is_reset = true;
        }

        if(is_reset)
        {
            is_reset = false;
            SuccessfulTrials();
            EndEpisode();
        }

        if(GameManager.getMove())
        {
            tot_time_taken += Time.deltaTime;

            RaycastHit hit;
            float max_distance = 5f;

            //Debug the forward vector
            //Debug.DrawRay(transform.position, transform.forward * max_distance, Color.blue);

            //Check if the agent is going to collide with an obstacle specifically
            //Loop for the amount of rays around agent for checking surroundings
            for(int i = 0; i < num_rays; i++)
            {
                //Calculate the ray direction based on the angle
                Vector3 ray_dir = Quaternion.Euler(0f, -90 + angle_increment * i, 0f) * transform.forward;

                Vector3 ray_start_pos = transform.position - new Vector3(0f,0.8f,0f);
                
                //Cast the ray here
                if(Physics.Raycast(ray_start_pos, ray_dir, out hit, max_distance))
                {
                    if((hit.collider.gameObject.tag == "Obstacle"))
                    {
                        //Avoid the obstacle
                        float distance_to_obstacle = hit.distance;
                        //move_x = Mathf.Sign(-hit.normal.x) * 0.5f;

                        //Check if there is a wall to the left or the right of the agent
                        Vector3 left_ray_dir = Quaternion.Euler(0f, -wall_check_angle, 0f) * transform.forward;
                        Vector2 right_ray_dir = Quaternion.Euler(0f, wall_check_angle, 0f) * transform.forward;
                        bool is_left_wall = Physics.Raycast(transform.position, left_ray_dir, wall_check_angle, wall_layer_mask);
                        bool is_right_wall = Physics.Raycast(transform.position, right_ray_dir, wall_check_angle, wall_layer_mask);

                        //Move in the opposite direction to the wall
                        if(is_left_wall && !is_right_wall)
                        {
                            //Move right
                            move_x = 0.5f;
                        }
                        else if(is_right_wall && !is_left_wall)
                        {
                            //Move left
                            move_x = -0.5f;
                        }
                        else
                        {
                            //Move randomly here (to the left or to the right)
                            //move_x = Mathf.Sign(-hit.normal.x) * 0.5f;
                            move_x = Random.Range(-0.5f, 0.5f);
                        }

                        //Check if too close to the obstacle
                        if(distance_to_obstacle < avoid_distance)
                        {
                            transform.Translate(new Vector3(move_x, 0f, 0f) * avoid_speed * Time.deltaTime);

                            //Reward the agent here for avoiding obstacle
                            avoid_bonus += 0.1f;
                            start_score += 0.1f;
                            SetReward(start_score);
                        }

                        Debug.DrawRay(ray_start_pos, ray_dir * max_distance, Color.blue);
                    }
                    else
                    {
                        //Doesn't need to do anything here yet
                        Debug.DrawRay(ray_start_pos, ray_dir * max_distance, Color.grey);
                    }

                    //Get the agent to jump over the obstacle
                    if(hit.collider.gameObject.tag == "JumpObstacle")
                    {
                        is_jumping = true;
                        Debug.DrawRay(ray_start_pos, ray_dir * max_distance, Color.red);
                    }
                }
            }

            //Penalise the agent for staying near the goal and not crossing the finsh line when in proximity
            if(distance_to_target <= threshold_distance)
            {
                //Will be rewarded for touching goal
                //else
                start_score -= 0.01f;
                SetReward(start_score);
            }
        }
    }

    //How the agent observes its environment
    //What information does the agent need to do what it needs to learn
    public override void CollectObservations(VectorSensor sensor)
    {
        //Let the agent know where the goal is and where they are 
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(target_transform.localPosition);

        //Be aware of the score
        sensor.AddObservation(start_score);

        //Add the distance to target
        sensor.AddObservation(distance_to_target);

        //Tell agent the total deduction as a result of touching obstacles
        sensor.AddObservation(total_deduction);

        //Tell agent the total bonus they got from avoiding obstacles and walls
        sensor.AddObservation(avoid_bonus);
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        if(player_ref.GameStart())
        {
            //Epsilon Greedy Method
            if(random.NextDouble() < epsilon)
            {
                move_x = (float)random.NextDouble() * 2f - 1f;
                move_z = (float)random.NextDouble() * 2f - 1f;

                if(is_jumping)
                {
                    rb.AddForce(move_x ,jump_force, move_z, ForceMode.Impulse);

                    start_score += 0.2f;
                    SetReward(start_score);

                    Debug.Log("Jumping in epsilon");

                    is_jumping = false;
                }
            }
            else
            {
                move_x = actions.ContinuousActions[0];
                move_z = actions.ContinuousActions[1];

                if(is_jumping)
                {
                    rb.AddForce(move_x ,jump_force, move_z, ForceMode.Impulse);

                    start_score += 0.2f;
                    SetReward(start_score);
                    
                    is_jumping = false;
                }
            }
        }

        transform.localPosition += new Vector3 (move_x, 0f, move_z) * Time.deltaTime * speed;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuous_actions = actionsOut.ContinuousActions;
        continuous_actions[0] = Input.GetAxisRaw("Horizontal");
        continuous_actions[1] = Input.GetAxisRaw("Vertical");
    }
    private void OnTriggerEnter(Collider other) 
    {
        //Calculate the score in relation to the distance to the goal
        if(other.gameObject.tag == "Goal")
        {
            //There is Add reward and SetReward functions
            start_score += 20f; 
            inference_score += 20f;
            SetReward(start_score);
            
            //Set the last value to being 1 since the agent has completed the task successfully
            trials_arr[trials_arr.Length - 1] = 1;

            floor_meshrenderer.material = win_material;
            //Doesn't end the game but resets the states so we can train again

            has_won = true;

            //REMOVE ME
            is_reset = true;

            //params_ref.setPlayerHasWon(false);

            GameManager.setWin(true);
            GameManager.setMove(false);


            //EndEpisode();
        }

        //Decrement score from agent if they keep touching the wall
        if(other.gameObject.tag == "Wall")
        {
            start_score -= 10f;
            inference_score -= 10f;
            SetReward(start_score);
            floor_meshrenderer.material = lose_material;

            //Get the distance, round it to the nearest int and set the value in the trials array to 0
            distance_to_target = Vector3.Distance(transform.localPosition, target_transform.localPosition);
            int temp_val = Mathf.RoundToInt(distance_to_target);

            if(temp_val < trials_arr.Length)
            {
                trials_arr[temp_val] = 0;
            }

            //params_ref.setPlayerHasWon(true);
            has_lost = true;
            
            is_reset = true;
            
            //Reset variable so we can reposition the obstacles
            GameManager.setLose(true);
            GameManager.setMove(false);

            //EndEpisode();
        }   
    }

    private void OnCollisionStay(Collision other) 
    {
        distance_to_target = Vector3.Distance(transform.localPosition, target_transform.localPosition);
        int temp_val = Mathf.RoundToInt(distance_to_target);
        
        //Reward the agent for playing but this will incentivise them to stay away from the wall to maximise the score
        if(other.gameObject.tag == "Ground")
        {   

            //Initialise the smallest distance to the target to the current distance. Only reward the agent if they get closer to the goal once!
            //If they move away from the goal again then do not reward them.
            if(!started)
            {
                smallest_distance = distance_to_target;
                started = true;
            }
 
            if(distance_to_target < smallest_distance)
            {
                smallest_distance = distance_to_target;
                
                //We want to reward the agent for getting closer to the goal
                if(distance_to_target < prev_distance)
                {
                    start_score += 1f;
                    inference_score += 1f;
                    SetReward(start_score);

                    //Successfully another metre closer to the goal
                    if(temp_val < trials_arr.Length)
                    {
                        trials_arr[temp_val] = 1;
                    }

                }
            }

            if(distance_to_target > prev_distance)
            {
                start_score -= 0.1f;
                SetReward(start_score);

                //If agent moves away then it fails the trial
                if(temp_val < trials_arr.Length)
                {
                    trials_arr[temp_val] = 0;
                }


                //Debug.Log("Moving away from the goal!");
            }

            
            prev_distance = distance_to_target;

        }   

        //For each pass we will deduct some points from the agent because we want them to avoid obstacles
        //We will use a ray cast here possibly
        if(other.gameObject.tag == "Obstacle")
        {
            total_deduction += 0.05f;
            start_score -= 0.05f;
            inference_score -= 0.05f;
            SetReward(start_score);

            if(temp_val < trials_arr.Length)
            {
                trials_arr[temp_val] = 0;
            }

        }

        if(other.gameObject.tag == "JumpObstacle")
        {
            total_deduction += 0.05f;
            start_score -= 0.05f;
            inference_score -= 0.05f;
            SetReward(start_score);

            if(temp_val < trials_arr.Length)
            {
                trials_arr[temp_val] = 0;
            }

        }
    }

    //Add up all of the values in the array so we can get the success rate
    private void SuccessfulTrials()
    {
        for(int i = 0; i < trials_arr.Length; i++)
        {
            successful_trials += trials_arr[i];
        }

        params_ref.setAgetTrialsSuccess(successful_trials);
    }

    public bool isReset()
    {
        return is_reset;
    }

    private void SetParams()
    {

        if(has_won)
        {
            params_ref.setAgentWins(1);
            wins += 1;
            sent_score = true;
            sent_time = true;
            sent_speed = true;
            has_won = false;
        }
        else if(has_lost)
        {
           params_ref.setPlayerWins(1);
           params_ref.setAgentLosses(1);
           losses += 1;
           sent_speed = true;
           sent_score = true;
           sent_time = true;
           has_lost = false;
        }

        if(sent_speed)
        {
            params_ref.setAgentSpeed(speed);
            sent_speed = false;
        }

        if(sent_score)
        {
            params_ref.setAgentScore(inference_score);
            sent_score = false;
        }

        if(sent_time)
        {
            params_ref.setAgentTime(tot_time_taken);
            sent_time = false;
        }

        Debug.Log("Wins: " + wins);
        Debug.Log("Losses: " + losses);
    }

    public void setReset(bool val)
    {
        is_reset = val;
    }

    public void setSpeed(float val)
    {
        speed = val;
    }

    public void setEpsilon(float val)
    {
        epsilon = val;
    }

    public float getScore()
    {
        return inference_score;
    }

    public float getSpeed()
    {
        return speed;
    }

    public float getTime()
    {
        return tot_time_taken;
    }

    public float getEpsilon()
    {
        return epsilon;
    }
}
