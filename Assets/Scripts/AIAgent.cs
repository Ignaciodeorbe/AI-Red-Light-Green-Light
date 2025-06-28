using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections;
using TMPro;


// .----------------------------.
// |    ____        _           |
// |   |  _ \ _   _| | ___      |
// |   | |_) | | | | |/ _ \     |
// |   |  _ <| |_| | |  __/     |
// |   |_| \_\\__,_|_|\___      |
// |                            |
// |   Rule-Based AI Methods    |
// |   _NamedLikeThis_          |
// '----------------------------'

public class AIAgent : Agent
{
    // Enviornment parameters
    [SerializeField] private Transform goal;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float rotationSpeed = 180f;
    private bool reachedGoal = false;


    private Renderer agentRenderer;

    // RL parameters
    [HideInInspector] private int currentEpisode = 0;
    [HideInInspector] private float cumulativeReward = 0f;

    // Visual feedback params
    [SerializeField] private Renderer groundRenderer;
    private Color defaultGroundColor;
    private Coroutine flashGroundCorotine;
    [SerializeField] public TextMeshPro trainingResults = null;

    public enum StopLight
    {
        GreenLight,
        YellowLight,
        RedLight
    }

    private StopLight currentLight = StopLight.GreenLight;
    private int lightChangeStep = 0;

    float totalTime = 0;


    public StopLight GetCurrentLight()
    {
        return currentLight;
    }

    [SerializeField] private Vector2Int greenLightStepsRange = new Vector2Int(1000, 3000);
    [SerializeField] private int yellowLightSteps = 300;
    [SerializeField] private Vector2Int redLightStepsRange = new Vector2Int(500, 1500);


    [SerializeField] public Collider mlAgentCollider;
    [SerializeField] public Collider aiAgentCollider;

    // Get the rule based AI
    [SerializeField] private RuleBasedAgent ruleBasedAgent;

    bool reset = false;



    public override void Initialize()
    {
        // Reset episode and reward counter
        agentRenderer = GetComponent<Renderer>();
        currentEpisode = 0;
        cumulativeReward = 0f;

        // Reset ground color
        if (groundRenderer != null)
        {
            defaultGroundColor = groundRenderer.material.color;
        }

        // Set initial light change step
        lightChangeStep = Random.Range(greenLightStepsRange.x, greenLightStepsRange.y);

        Physics.IgnoreCollision(mlAgentCollider, aiAgentCollider, true);



    }

    /// <summary>
    /// Makes sure light updates consistently regardless of frame rate
    /// </summary>
    private void FixedUpdate()
    {
        UpdateLightCycle();
        totalTime += Time.deltaTime;

    }

    /// <summary>
    /// Manages stop light state machine
    /// </summary>
    private void UpdateLightCycle()
    {
        // Checks if current steo count exceeds the next change threshold
        if (StepCount >= lightChangeStep)
        {
            // checks through light states and changes to next state if need to
            switch (currentLight)
            {
                case StopLight.GreenLight:
                    currentLight = StopLight.YellowLight;
                    lightChangeStep = StepCount + yellowLightSteps;
                    SetGroundColor(Color.yellow);
                    break;

                case StopLight.YellowLight:
                    currentLight = StopLight.RedLight;
                    lightChangeStep = StepCount + Random.Range(redLightStepsRange.x, redLightStepsRange.y);
                    SetGroundColor(Color.red);
                    break;

                case StopLight.RedLight:
                    currentLight = StopLight.GreenLight;
                    lightChangeStep = StepCount + Random.Range(greenLightStepsRange.x, greenLightStepsRange.y);
                    SetGroundColor(Color.green);
                    break;
            }

            // Update light state for rulebased AI
            ruleBasedAgent.UpdateTrafficLight(currentLight);

        }
    }

    public override void OnEpisodeBegin()
    {
        // Visual feedback on whether or not the agent ended up with a positive or negative reward
        if (groundRenderer != null && cumulativeReward != 0f)
        {
            // Blue for positive reward, black for negative reward
            Color flashColor = (cumulativeReward > 0f) ? Color.blue : Color.black;

            if (flashGroundCorotine != null)
            {
                StopCoroutine(flashGroundCorotine);
            }

            flashGroundCorotine = StartCoroutine(FlashGround(flashColor, 3.0f));
        }

        // Set default values when an episode begins
        reachedGoal = false;
        currentEpisode++;
        cumulativeReward = 0f;
        agentRenderer.material.color = Color.blue;
        currentLight = StopLight.GreenLight;
        lightChangeStep = Random.Range(greenLightStepsRange.x, greenLightStepsRange.y);

        // Sets up the environment
        SpawnObjects();

        totalTime = 0;
        reset = true;
    }

    /// <summary>
    /// Flashes the ground a given color as a visual indicator
    /// </summary>
    private IEnumerator FlashGround(Color color, float duration)
    {
        float elapsedTime = 0f;

        groundRenderer.material.color = color;

        // This causes the pulse of color
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            groundRenderer.material.color = Color.Lerp(color, defaultGroundColor, elapsedTime / duration);
            yield return null;
        }

        // return color to stop light stste color
        SetGroundColor(GetColorFromLightState());
    }

    /// <summary>
    ///  Changes the color of the ground to match the light state
    /// </summary>
    private Color GetColorFromLightState()
    {
        return currentLight switch
        {
            StopLight.GreenLight => Color.green,
            StopLight.YellowLight => Color.yellow,
            StopLight.RedLight => Color.red,
            _ => defaultGroundColor
        };
    }

    /// <summary>
    /// Spawns the objects for environments
    /// </summary>
    private void SpawnObjects()
    {
        // Resets the agent to a starting position
        transform.localRotation = Quaternion.identity;
        transform.localPosition = new Vector3(0f, 0.15f, -3f);

        Vector3 goalPosition = transform.localPosition;

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Normalize goal position coordinates between -1 and 1 
        float goalPosXNormalized = goal.localPosition.x / 5f;                         // 
        float goalPosZNormalized = goal.localPosition.z / 10f;                        // 
                                                                                      // These are hard coded values based on the size of the map
        // Normalize agent position coordinates between -1 and 1                      // 
        float agentPosXNormalized = transform.localPosition.x / 5f;                   // 
        float agentPosZNormalized = transform.localPosition.z / 10f;                  // 

        // Normalize agent rotation (convert 0-360 to -1 to 1 range)
        float agentRotationNormalized = (transform.localRotation.eulerAngles.y / 360f) * 2f - 1f;

        // Add all the positions observations to the sensor
        sensor.AddObservation(goalPosXNormalized);
        sensor.AddObservation(goalPosZNormalized);
        sensor.AddObservation(agentPosXNormalized);
        sensor.AddObservation(agentPosZNormalized);
        sensor.AddObservation(agentRotationNormalized);

        // Convert current traffic light state to normalized value
        float lightStateNormalized = currentLight switch
        {
            StopLight.GreenLight => 0f,
            StopLight.YellowLight => 0.5f,
            StopLight.RedLight => 1f,
            _ => 0f
        };

        // Add traffic light state observation
        sensor.AddObservation(lightStateNormalized);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;

        // These are the equivelent of key board actions
        if (Input.GetKey(KeyCode.UpArrow)) discreteActionsOut[0] = 1; // Move forward
        else if (Input.GetKey(KeyCode.LeftArrow)) discreteActionsOut[0] = 2; // Turn left
        else if (Input.GetKey(KeyCode.RightArrow)) discreteActionsOut[0] = 3; // Turn right
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Store position before movement for distance calculation
        Vector3 beforeMovePosition = transform.position;

        // Calculate vector to goal for reward calculation
        Vector3 toGoal = goal.localPosition - transform.localPosition;

        // Execute the received action
        MoveAgent(actions.DiscreteActions);

        if (reset)
        {
            // Reset rule based AI
            ruleBasedAgent.ResetAgent();
            reset = false;
        }

        // Calculate movement distance this step
        Vector3 afterMovePosition = transform.position;
        float moveDistance = Vector3.Distance(beforeMovePosition, afterMovePosition);

        // Small constant penalty to encourage efficiency
        AddReward(-2f / MaxStep);

        // Red light behavior checks
        if (currentLight == StopLight.RedLight)
        {
            if(moveDistance > 0.001f)
            {
                AddReward(-1.0f); // Penalty for moving during red light

            }
            else
            {
                //AddReward(0.01f); // Reward for not moving during red light
            }
        }


        // Scaled positive reward if moving toward goal
        AddReward(0.01f * (1 / toGoal.magnitude));

        // Encourage exploration 
        if (moveDistance > 0.001f && currentLight != StopLight.RedLight)
        {
            AddReward(0.01f); // Small incentive to explore
        }

        // Update cumulative reward tracking
        cumulativeReward = GetCumulativeReward();

        // Since EndEpisode cant be overriden this is my way of having an EndEpisode where I can give a negative reward for not reaching the goal
        if (StepCount >= MaxStep)
        {
            if (!reachedGoal)
            {
                AddReward(-100.0f); // Large penalty for not reaching goal
            }

            EndEpisode();
        }
    }

    /// <summary>
    /// Moves the agent based on discrete action input
    /// </summary>
    /// <param name="act">Action segment containing movement command</param>
    public void MoveAgent(ActionSegment<int> act)
    {
        // Get first discrete action
        var action = act[0];

        // Causes the agent to move
        switch (action)
        {
            case 1:
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
                break;
            case 2:
                transform.Rotate(0f, -rotationSpeed * Time.deltaTime, 0f);
                break;
            case 3:
                transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
                break;
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for goal collision using tags applied to gameobjects
        if (other.gameObject.CompareTag("Goal"))
        {
            GoalReached();
        }
    }

    /// <summary>
    /// Handles successful goal reaching
    /// </summary>
    private void GoalReached()
    {
        if(!ruleBasedAgent.RuleBasedWins())
        {
            Debug.Log("ML Agent WINS!!!");
            Debug.Log("ML Agent Time: " + totalTime);



        }

        reachedGoal = true;
        AddReward(50.0f); // Large reward for reaching goal
        cumulativeReward = GetCumulativeReward();
        EndEpisode();
    }


    private void OnCollisionEnter(Collision collision)
    {
        // Wall collision handling
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Immediate collision penalty
            AddReward(-0.05f);
            if (agentRenderer != null)
            {
                agentRenderer.material.color = Color.red; // Visual feedback
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // Continuous wall contact penalty
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.01f * Time.deltaTime);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // When leaving wall collision change the agent back to normal color
        if (collision.gameObject.CompareTag("Wall"))
        {
            if (agentRenderer != null)
            {
                agentRenderer.material.color = Color.blue;
            }
        }
    }


    /// <summary>
    /// Changes the ground color for visual feedback
    /// </summary>
    private void SetGroundColor(Color color)
    {
        if (groundRenderer != null)
        {
            groundRenderer.material.color = color;
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        GUIStyle positiveStyle = new GUIStyle();
        GUIStyle negativeStyle = new GUIStyle();

        style.fontSize = 20;
        style.normal.textColor = Color.white;

        positiveStyle.fontSize = 20;
        positiveStyle.normal.textColor = Color.green;

        negativeStyle.fontSize = 20;
        negativeStyle.normal.textColor = Color.red;

        GUIStyle rewardStyle = cumulativeReward < 0 ? negativeStyle : positiveStyle;

        GUI.Label(new Rect(10, 10, 300, 30), "Episode: " + currentEpisode, style);
        GUI.Label(new Rect(10, 40, 300, 30), "Cumulative Reward: " + cumulativeReward.ToString("F2"), rewardStyle);
        GUI.Label(new Rect(10, 70, 300, 30), "Current Step: " + StepCount, style);

        string colorTag = cumulativeReward < 0 ? "<color=#ff4d4d>" : "<color=#4dff4d>";

        //trainingResults.faceColor = Color.red;

        trainingResults.text =
            colorTag + "Cumulative Reward: " + cumulativeReward.ToString("F2") + "<color=black>" +
            "\nEpisode: " + currentEpisode +
            "\nCurrent Step: " + StepCount;
    }
}
