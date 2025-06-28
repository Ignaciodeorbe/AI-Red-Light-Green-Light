using TMPro;
using UnityEngine;


public class RuleBasedAgent : MonoBehaviour
{
    // Enviornment parameters
    [SerializeField] private Transform goal;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float rotationSpeed = 180f;

    private AIAgent.StopLight currentLight;

    float totalTime = 0;

    private bool ruleBasedWins = false;

    public bool RuleBasedWins()
        { return ruleBasedWins; }

    private void Start()
    {
        // Resets the agent to a starting position
        transform.localRotation = Quaternion.identity;
        transform.localPosition = new Vector3(1f, 0.15f, -3f);
    }

    private void Update()
    {
        if (currentLight != AIAgent.StopLight.RedLight)
        {
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }

        totalTime += Time.deltaTime;
    }

    public void ResetAgent()
    {
        transform.localRotation = Quaternion.identity;
        transform.localPosition = new Vector3(1f, 0.15f, -3.0625f);
        totalTime = 0;
        ruleBasedWins = false;

    }

    public void UpdateTrafficLight(AIAgent.StopLight newLight)
    {
        currentLight = newLight;
    }


    private void OnTriggerEnter(Collider other)
    {
        // Check for goal collision using tags applied to gameobjects
        if (other.gameObject.CompareTag("Goal"))
        {
            ruleBasedWins = true;
            Debug.Log("Rule Base AI Agent WINS!!!");
            Debug.Log("Rule Base AI Time: " + totalTime);
        }
    }
}

