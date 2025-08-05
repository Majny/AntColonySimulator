using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class NestController : MonoBehaviour
{
    [Header("Agent setup")]
    public AgentParameters agentParams;
    public AntAgent agentPrefab;
    public int initialAgents = 10;

    public Transform agentsParent;

    [Header("Pheromone fields")]
    public PheromoneField homeMarkersField;
    public PheromoneField foodMarkersField;
    public PheromoneField playArea;

    [Header("Spawn")]
    public float spawnRadius = 0.25f;

    int foodCollected;

    void Awake()
    {
        AutoAssignPlayArea();
        EnsureAgentsParent();
    }

    void Start()
    {
        for (int i = 0; i < initialAgents; i++)
            SpawnAgent();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        AutoAssignPlayArea();
    }
#endif

    void AutoAssignPlayArea()
    {
        if (playArea == null)
        {
            if (homeMarkersField != null) playArea = homeMarkersField;
            else if (foodMarkersField != null) playArea = foodMarkersField;
        }
    }

    void EnsureAgentsParent()
    {
        if (agentsParent == null)
        {
            var go = new GameObject("Agents");
            go.transform.SetParent(transform, false);
            agentsParent = go.transform;
        }
    }

    public void SpawnAgent()
    {
        Vector2 offset = (spawnRadius > 0f) ? Random.insideUnitCircle * spawnRadius : Vector2.zero;
        Vector2 spawnPos = (Vector2)transform.position + offset;

        if (playArea != null)
            spawnPos = playArea.ClampToArea(spawnPos);

        var agent = Instantiate(agentPrefab, (Vector3)spawnPos, Quaternion.identity, agentsParent);

        if (agent.parameters == null && agentParams != null)
            agent.parameters = agentParams;
        if (agent.sensorOrigin == null)
            agent.sensorOrigin = agent.transform;

        agent.Init(this, homeMarkersField, foodMarkersField);

        if (playArea != null)
            agent.SetPlayArea(playArea);
    }

    public void ReportFood()
    {
        foodCollected++;
        Debug.Log("Food gathered: " + foodCollected);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
#endif
}
