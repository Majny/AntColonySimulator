using UnityEngine;

public class NestController : MonoBehaviour
{
    public AgentParameters agentParams;
    public AntAgent agentPrefab;
    public int initialAgents = 10;

    public Transform agentsParent;
    private int foodCollected;

    private void Start()
    {
        for (int i = 0; i < initialAgents; i++)
            SpawnAgent();
    }

    private void SpawnAgent()
    {
        AntAgent agent = Instantiate(agentPrefab, transform.position, Quaternion.identity, agentsParent);
        agent.Initialize(this);
    }

    public void ReportFood()
    {
        foodCollected++;
        Debug.Log("Food gathered: " + foodCollected);
    }
}