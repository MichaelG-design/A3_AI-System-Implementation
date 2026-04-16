using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AlarmAI : MonoBehaviour
{
    //This script handles the Alarm AI
    //Just like the Patrol AI, this agent uses a vision cone to find the player
    //and also has different states that switch when triggered


    //Below is the finate state machine that holds all the states this AI has
    //
    //Patrol: Moves to different points within the level
    //Alarm: Activates an alarm upon seeing the Player, stays there until it finishes
    //Cooldown: The AI returns to the next patrol point,
    //and cannot set off the alarm until it does
    public enum State
    {
        Patrol,
        Alarm,
        Cooldown
    }

    //Stores the current state
    public State currentState;

    //Stores the Player's transform and references this object's component
    private NavMeshAgent agent;
    public Transform player;

    //Reference for the Patrol AI
    public PatrolAI patrolAI;

    //Vision cone settings
    public float visionRange = 10f;
    public float visionAngle = 60f;

    //Similar to the Patrol AI, this is an array to store empty gameObjects
    //acting as patrol points for this AI
    public Transform[] patrolPoints;
    private int patrolIndex = 0;

    //Time limit and timer for the alarm
    public float alarmDuration = 10f;
    private float alarmTimer = 0f;

    //Sets the state of this AI to "Patrol" from the get go
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentState = State.Patrol;
    }

    void Update()
    {
        //This checks to see if the Player is in the vision cone
        bool canSeePlayer = CanSeePlayer();

        //Here is where all the triggers/conditions for each state are kept
        switch (currentState)
        {
            //If this AI sees the Player, switch to "Alarm" state, stop moving,
            //trigger the alarm, AND start the timer for how long the alarm goes on for
            case State.Patrol:
                Patrol();

                if (canSeePlayer)
                {
                    currentState = State.Alarm;

                    patrolAI.TriggerAlarm(transform.position);

                    agent.ResetPath();

                    alarmTimer = 0f;
                }
                break;

            //After the alarm timer hits the time limit (10 seconds),
            //switch to the "Cooldown" state
            case State.Alarm:
                agent.ResetPath();

                alarmTimer += Time.deltaTime;

                if (alarmTimer >= alarmDuration)
                {
                    currentState = State.Cooldown;
                }
                break;

            //Stay in the "Cooldown" state until reaching a patrol point,
            //then switch the current state to "Patrol"
            case State.Cooldown:

                agent.SetDestination(patrolPoints[patrolIndex].position);

                if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].position) < 1f)
                {
                    patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                    currentState = State.Patrol;
                }
                break;
        }
    }

    //Below is what makes the AI move to each patrol point while its
    //in the "Patrol" state
    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[patrolIndex].position);

        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].position) < 1f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }
    }

    //This checks to see if the Player is in the vision cone
    bool CanSeePlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (angle < visionAngle / 2f)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance < visionRange)
            {
                return true;
            }
        }

        return false;
    }

    //These Gizmos just help me see the vision cones and current states when
    //testing in the Editor View
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (currentState == State.Patrol)
            Gizmos.color = Color.green;
        else if (currentState == State.Alarm)
            Gizmos.color = Color.red;
        else if (currentState == State.Cooldown)
            Gizmos.color = Color.blue;

        Vector3 left = Quaternion.Euler(0, -visionAngle / 2, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, visionAngle / 2, 0) * transform.forward;

        Gizmos.DrawLine(transform.position, transform.position + left * visionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * visionRange);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * visionRange);
    }
}