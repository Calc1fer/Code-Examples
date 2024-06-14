using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParameterManager : MonoBehaviour
{
    //Improvement could be to use arrays for all parameters to decide whether
    //to increase the difficulty or not by comparing data
    [SerializeField] Player_Controller player_ref;
    [SerializeField] MoveToGoalAgent agent_ref;
    [SerializeField] Obstacle obs_ref;

    const int size = 3;
    private float[] player_speed = new float[size];
    private int player_wins = 0;
    private float[] player_time = new float[size];
    private float[] player_score = new float[size];
    private float[] agent_speed = new float[size];
    private int agent_wins = 0;
    private int agent_losses = 0;
    private float[] agent_time = new float[size];
    private float[] agent_score = new float[size];
    private int[] agent_successrates = new int[size];
    private int game_count = 0;
    private int agent_trial_success = 0;
    private int old_gamecount = 0;
    private int current_idx = 0;

    private float lowest_speed_threshold = 10f;
    private float highest_speed_threshold = 20f;

    private bool help_player = false;
    private bool help_agent = false;
    private bool allow_update = true;
    private bool allow_buff = true;
    private bool buff_player = false;
    private float player_buff = 15f;
    private float player_base_speed = 7f;
    private int next_idx = 0;
    private int num_eps = 0;
    private int prev_eps = 0;

    //This script will get the necessary information in order to pass back into the player and agent classes so that the difficulty changes dynamically
    // Start is called before the first frame update
    void Start()
    {
        // player_base_speed = player_ref.getSpeed();
    }

    // Update is called once per frame
    void Update()
    {
        //Allow update if the game count has increased by one then set to false to update the variables once
        if(game_count == old_gamecount + 1)
        {
            allow_update = true;        
        }

        old_gamecount = game_count;
        
        //Determine how the difficulty will be changed
        //We only want to begin changing the difficulty when we have 3 indexes of data in
        //each array. Do the adjustments when counting down to start
        if(game_count >= 3 && !GameManager.getMove() && allow_update)
        {

            //Here we call comparative functions to check through the data and make necessary adjustments to the application (player and agent objects)

            //Compare last three player times and how many wins - Adjust player speed?
            //If true, adjust the difficulty for the agent, else the player
            CompareTimes();
            CheckWins();
            SetAgentEpsilon();
            // Debug.Log(player_ref.getSpeed());

            //Compare how many wins player has against agent - Adjust bollard force?
            //Can also include a slight increase in random movement from the agent

            if(player_wins < agent_wins)
            {
                buff_player = true;
                allow_buff = true;
            }
            

            allow_update = false;
        }


        if(prev_eps < num_eps)
        {
            float avg_success = AverageSuccessRate();
            float avg_wins = AgentAverageWins();
            float avg_losses = AgentAverageLosses();
            float avg_score = AgentAverageScore();

            //Debug.Log("Average Success Rate: " + avg_success);
            // Debug.Log("Average Win Rate: " + avg_wins);
            // Debug.Log("Average Lose Rate: " + avg_losses);
            //Debug.Log("Average Score: " + avg_score);
        }

        prev_eps = num_eps;
        
        if(buff_player && allow_buff)
        {
            //Give the player a perk if they are struggling
            if(player_wins < agent_wins)
            {
                //Help the player
                IncreasePlayerSpeed();
                allow_buff = false;
            }
        }
    }

    private void CompareTimes()
    {   
        //Get the average of previous 2 times
        float avg_player_times = (player_time[0] + player_time[1]) / 2;

        //If the latest player time is less than the average previous 2
        if(player_time[2] < avg_player_times)
        {
            //Check the wins to see if the player is doing well
            if(player_wins < agent_wins)
            {
                //Help the player
                help_player = true;
            }
            else
            {
                //Otherwise they're doing well and increase agent params
                help_agent = true;
            }
        }
        else if(player_time[2] > avg_player_times)
        {
            if(player_wins < agent_wins)
            {
                help_player = true;
            }
            else
            {
                help_agent = true;
            }
        }

        //Set parameters here since other functions will reset the help booleans 
        if(help_player)
        {
            //Adjust the agent speed
            if(agent_ref.getSpeed() <= 10f)
            {
                agent_ref.setSpeed(10f);
            }
            else
            {
                agent_ref.setSpeed(agent_ref.getSpeed() - 2f);
            }
        }
        else if(help_agent)
        {
            if(agent_ref.getSpeed() >= 20f)
            {
                agent_ref.setSpeed(20f);
            }
            else
            {
                agent_ref.setSpeed(agent_ref.getSpeed() + 2.5f);
            }
        }

        resetHelp();
    }

    private void CheckWins()
    {
        if(player_wins < agent_wins)
        {
            help_player = true;
        }
        else
        {
            help_agent = true;
        }

        //Increase params
        if(help_player)
        {
            //Adjust the agent speed
            if(agent_ref.getSpeed() <= 10f)
            {
                agent_ref.setSpeed(10f);
            }
            else
            {
                agent_ref.setSpeed(agent_ref.getSpeed() - 2f);
            }
        }
        else if(help_agent)
        {
            if(agent_ref.getSpeed() >= 20f)
            {
                agent_ref.setSpeed(20f);
            }
            else
            {
                agent_ref.setSpeed(agent_ref.getSpeed() + 2.5f);
            }
        }

        resetHelp();
    }

    //Option so that the player has a slightly better chance of winning
    //Can be used instead of making the agent move slower by giving the player a head start
    private void IncreasePlayerSpeed()
    {
        player_ref.setSpeed(player_buff);
        buff_player = false;
        //Apply speed for certain time
        Invoke("ResetPlayerSpeed", 5f);
    }

    //Increase or decrease the randomness of the agent movement. Make them make mistakes
    private void SetAgentEpsilon()
    {
        if(player_wins < agent_wins)
        {
            help_player = true;
        }
        else
        {
            help_agent = true;
        }

        if(help_player)
        {
            if(agent_ref.getEpsilon() >= 0.3f)
            {
                agent_ref.setEpsilon(0.3f);
            }
            else
            {
                agent_ref.setEpsilon(agent_ref.getEpsilon() + 0.05f);
            }
        }
        else
        {
            if(agent_ref.getEpsilon() <= 0f)
            {
                agent_ref.setEpsilon(0f);
            }
            else
            {
                agent_ref.setEpsilon(agent_ref.getEpsilon() - 0.075f);
            }
        }
        resetHelp();
    }

    private float AverageAgentSpeed()
    {
        float avg_agent_speed = 0f;

        for(int i = 0; i < agent_speed.Length; i++)
        {
            avg_agent_speed += agent_speed[i];
        }

        return avg_agent_speed / agent_speed.Length;
    }

    private float AverageSuccessRate()
    {
        float avg = 0f;

        for(int i = 0; i < next_idx; i++)
        {
            avg += agent_successrates[i];
        }

        avg /= num_eps;
        return avg;
    }

    private float AgentAverageScore()
    {
        float avg = 0f;

        for(int i = 0; i < agent_score.Length; i++)
        {
            avg += agent_score[i];
        }

        avg /= num_eps;

        return avg;
    }

    private float AgentAverageLosses()
    {
        float avg = 0;
        avg = (agent_losses / num_eps) * 100;
        return avg;
    }

    private float AgentAverageWins()
    {
        float avg = 0;
        avg = (agent_wins / num_eps) * 100;
        return avg;
    }

    private void ResetPlayerSpeed()
    {
        player_ref.setSpeed(player_base_speed);
    }
    public int getPlayerWins()
    {
        return player_wins;
    }

    public int getAgentWins()
    {
        return agent_wins;
    }

    public int getAgentLosses()
    {
        return agent_losses;
    }

    //Effectively we will take three readings and as we add another value, it will replace
    //the oldest and shift the other readings along an index, overwriting the data but 
    //always having 3 readings available

    // //Wins never change value so they stay as ints
    public void setPlayerWins(int val)
    {
        player_wins += val;
        game_count += 1;
    }

    public void setAgetTrialsSuccess(int val)
    {
        num_eps += 1;
        agent_trial_success = val;

        agent_successrates[next_idx] = agent_trial_success;
        next_idx++;
    }

    public void setPlayerScore(float val)
    {
        player_score[current_idx] = val;
        current_idx = (current_idx + 1) % player_score.Length; 
    }

    public void setPlayerTime(float val)
    {
        player_time[current_idx] = val;
        current_idx = (current_idx + 1) % player_time.Length;
    }

    public void setPlayerSpeed(float val)
    {
        player_speed[current_idx] = val;
        current_idx = (current_idx + 1) % player_speed.Length;
    }

    public void setAgentWins(int val)
    {
        agent_wins += val;
        game_count += 1;
    }

    public void setAgentLosses(int val)
    {
        agent_losses += val;
        game_count += 1;
    }

    public void setAgentScore(float val)
    {
        agent_score[current_idx] = val;
        current_idx = (current_idx + 1) % agent_score.Length;
    }

    public void setAgentTime(float val)
    {
        agent_time[current_idx] = val;
        current_idx = (current_idx + 1) % agent_time.Length;
    }

    public void setAgentSpeed(float val)
    {
        agent_speed[current_idx] = val;
        current_idx = (current_idx + 1) % agent_speed.Length;
    }

    private void resetHelp()
    {
        help_agent = false;
        help_player = false;
    }
}
