using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PatrolAI : MonoBehaviour
{
    //This script handles the Patrol AI's logic
    //This AI uses a vision cone and partols between specific points,
    //then switches states depending on certain triggers

    //This is the Finate State Machine for this AI, and all the states associated with it
    //
    //Patrol: Moves to patrol points across a level
    //Chase: Follows the Player
    //GoToAlarm: When the alarm is activated, this AI goes to its location
    //WaitAtAlarm: The AI waits a specified amount of time at the alarm location
    public enum State
    {
        Patrol,
        Chase,
        GoToAlarm,
        WaitAtAlarm
    }

    //Stores the current state the AI is in
    public State currentState;

    //References the component on the AI, and the transform of the Player object
    private NavMeshAgent agent;
    public Transform player;

    //An array that holds all the patrol point gameObject's transforms
    public Transform[] patrolPoints;
    private int patrolIndex = 0;

    //Below is the vision cone settings
    public float visionRange = 10f;
    public float visionAngle = 60f;

    //Reference for when the Alarm is activated
    private bool alarmActive = false;
    private Vector3 alarmPosition;

    //Timer for how long this AI waits at the alarm location
    public float waitAtAlarmTime = 5f;
    private float alarmTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        //Sets the current state of the AI to the "Patrol" state
        currentState = State.Patrol;
    }

    void Update()
    {
        //This is a refernence if the Player is in line of sight of the vision cone
        bool canSeePlayer = CanSeePlayer();

        //Below is all the states and how they are triggered
        switch (currentState)
        {
            case State.Patrol:
                Patrol();

                //If the player is in sight, switch to "Chase" state
                //If the alarm is active and not in "Chase" state anymore,
                //switch to "GoToAlarm" state
                if (canSeePlayer)
                {
                    currentState = State.Chase;
                }
                else if (alarmActive)
                {
                    currentState = State.GoToAlarm;
                }
                break;

            case State.Chase:
                Chase();

                //If the AI can't see the player anymore, switch back to "Patrol" state
                //But if the alarm is active too, then switch to "GoToAlarm" state
                if (!canSeePlayer)
                {
                    if (alarmActive)
                    {
                        currentState = State.GoToAlarm;
                    }
                    else
                    {
                        currentState = State.Patrol;
                    }
                }
                break;

            case State.GoToAlarm:
                agent.SetDestination(alarmPosition);

                //If the Player is visible, go straight to the "Chase" state
                if (canSeePlayer)
                {
                    currentState = State.Chase;
                }
                //If it has reached the alarm position, go to "WaitAtAlarm" state
                //and start the timer
                else if (Vector3.Distance(transform.position, alarmPosition) < 1.5f)
                {
                    currentState = State.WaitAtAlarm;
                    alarmTimer = 0f;
                }
                break;

            case State.WaitAtAlarm:
                agent.ResetPath(); //Stops the AI's movement

                alarmTimer += Time.deltaTime;

                //If the Player is in the vision cone, go to "Chase" state
                if (canSeePlayer)
                {
                    currentState = State.Chase;
                }
                //After waiting till the timer finishes, go back to "Patrol" state
                else if (alarmTimer >= waitAtAlarmTime)
                {
                    alarmActive = false; //Turns off the alarm
                    currentState = State.Patrol;
                }
                break;
        }
    }

    //This here makes the AI move to each patrol point
    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[patrolIndex].position);

        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].position) < 1f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }
    }

    //This has the AI continously moving towards the Player
    //while they are visible
    void Chase()
    {
        agent.SetDestination(player.position);
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
    //This tells this AI the alarm is active, and where it was activated
    public void TriggerAlarm(Vector3 pos)
    {
        alarmActive = true;
        alarmPosition = pos;
    }

    //This just kills the player and makes them respawn
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                player.Respawn();
            }
        }
    }

    //This draws Gizmos to see the vision cone in the Scene View,
    //as well as different coloured cones to determine the AI's current state
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        //Change vision cone color based on state
        if (currentState == State.Patrol)
            Gizmos.color = Color.green;
        else if (currentState == State.Chase)
            Gizmos.color = Color.red;
        else if (currentState == State.GoToAlarm)
            Gizmos.color = Color.yellow;
        else if (currentState == State.WaitAtAlarm)
            Gizmos.color = Color.blue;

        Vector3 left = Quaternion.Euler(0, -visionAngle / 2, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, visionAngle / 2, 0) * transform.forward;

        Gizmos.DrawLine(transform.position, transform.position + left * visionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * visionRange);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * visionRange);
    }
}