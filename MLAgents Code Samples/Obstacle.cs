using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private GameObject block_obstacle;
    [SerializeField] private GameObject bollard_obstacle;
    [SerializeField] private GameObject jump_obstacle;
    [SerializeField] private MoveToGoalAgent agent_ref;
    private GameObject[] bollards;
    private GameObject[] blocks;
    private GameObject[] jump_obs;
    private int num_bollards = 8;
    private int num_blocks = 4;
    private int num_jump_obs = 3;
    private Vector3 last_pos;
    private Vector3 pos;
    private float min_distance = 5;
    private float distance_to_last_obs = 0f;
    private float x;
    private float y;
    private float z;
    private int seed = 0;


    // Start is called before the first frame update
    private void Awake() 
    {
        bollards = new GameObject[num_bollards];
        blocks = new GameObject[num_blocks];
        jump_obs = new GameObject[num_jump_obs];

         //Set up the environment - Randomly position obstacles
        //BOLLARDS

        for(int i = 0; i < num_bollards; i++)
        {
            x = Random.Range(-3.5f, 3.5f);
            y = 1.5f;
            z = Random.Range(-30, 25);

            pos = new Vector3(x,y,z);

            //If there is more than one object already instantiated then store the previous position
            if(i > 0)
            {
                last_pos = bollards[i - 1].transform.position;
                distance_to_last_obs = Vector3.Distance(pos, last_pos);
                
                //If the obstacle is too close to the last then move the obstacle elsewhere
                if(distance_to_last_obs < min_distance)
                {
                    pos += (last_pos - pos).normalized * min_distance;
                }
            }

            bollards[i] = Instantiate(bollard_obstacle, this.transform, false) as GameObject;
            bollards[i].transform.localPosition = pos;
        }  

        //BLOCKS
        for(int i = 0; i < num_blocks; i++)
        {
            x = Random.Range(-3, 3);
            y = 3f;
            z = Random.Range(-30, 25);

            pos = new Vector3(x,y,z);

            //If there is more than one object already instantiated then store the previous position
            if(i > 0)
            {
                last_pos = blocks[i - 1].transform.position;
                distance_to_last_obs = Vector3.Distance(pos, last_pos);
                
                

                //If the obstacle is too close to the last then move the obstacle elsewhere
                if(distance_to_last_obs < min_distance)
                {
                    pos += (last_pos - pos).normalized * min_distance;
                }
            }

            blocks[i] = Instantiate(block_obstacle, this.transform, false) as GameObject;
            blocks[i].transform.localPosition = pos;
        }

        //JUMP OBSTACLES
        for(int i = 0; i < num_jump_obs; i++)
        {
            x = 0f;
            y = 0.75f;
            z = Random.Range(-30, 25);

            pos = new Vector3(x,y,z);

            //If there is more than one object already instantiated then store the previous position
            if(i > 0)
            {
                last_pos = jump_obs[i - 1].transform.position;
                distance_to_last_obs = Vector3.Distance(pos, last_pos);
                
                

                //If the obstacle is too close to the last then move the obstacle elsewhere
                if(distance_to_last_obs < min_distance)
                {
                    pos += (last_pos - pos).normalized * min_distance;
                }
            }

            jump_obs[i] = Instantiate(jump_obstacle, this.transform, false) as GameObject;
            jump_obs[i].transform.localPosition = pos;
        }  
    }

    private void Update() 
    {
        bool reposition = agent_ref.isReset();

        if(reposition)
        {
            seed = (int) Time.time;
            Random.InitState(seed);
            //If the episode resets then reposition all of the obstacles for now
            //BOLLARDS
            for(int i = 0; i < num_bollards; i++)
            {
                x = Random.Range(-3.5f, 3.5f);
                y = 1.5f;
                z = Random.Range(-30, 25);

                pos = new Vector3(x,y,z);

                //If there is more than one object already instantiated then store the previous position
                if(i > 0)
                {
                    last_pos = bollards[i - 1].transform.position;
                    distance_to_last_obs = Vector3.Distance(pos, last_pos);

                    //If the obstacle is too close to the last then move the obstacle elsewhere
                    if(distance_to_last_obs < min_distance)
                    {
                        pos += (last_pos - pos).normalized * min_distance;
                    }
                }

                bollards[i].transform.localPosition = pos;
            }

            //BLOCKS    
            for(int i = 0; i < num_blocks; i++)
            {
                x = Random.Range(-3, 3);
                y = 3f;
                z = Random.Range(-30, 25);

                pos = new Vector3(x,y,z);

                //If there is more than one object already instantiated then store the previous position
                if(i > 0)
                {
                    last_pos = blocks[i - 1].transform.position;
                    distance_to_last_obs = Vector3.Distance(pos, last_pos);
                    
                     

                    //If the obstacle is too close to the last then move the obstacle elsewhere
                    if(distance_to_last_obs < min_distance)
                    {
                        pos += (last_pos - pos).normalized * min_distance;
                    }
                }

                blocks[i].transform.localPosition = pos;
            }

            //JUMP OBSTACLE    
            for(int i = 0; i < num_jump_obs; i++)
            {
                x = 0f;
                y = 0.75f;
                z = Random.Range(-30, 25);

                pos = new Vector3(x,y,z);

                jump_obs[i].transform.localPosition = pos;
    
            }

            agent_ref.setReset(false);
        }
        else
        {
            //Do nothing
        }
    }
}
