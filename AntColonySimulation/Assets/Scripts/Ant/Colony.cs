using UnityEngine;

[DisallowMultipleComponent]
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

    [Header("Spawn")]
    public float spawnRadius = 0.25f;

    [Header("Graphics (optional)")]
    public Transform graphic;
    public TextMesh foodCounter;

    int foodCollected;

    void Awake()
    {
        EnsureAgentsParent();
        TryFindGraphic();
        UpdateGraphicScale();
        UpdateCounter();
    }

    void Start()
    {
        for (int i = 0; i < initialAgents; i++) SpawnAgent();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        TryFindGraphic();
        UpdateGraphicScale();
    }
#endif

    void EnsureAgentsParent()
    {
        if (agentsParent == null)
        {
            var go = new GameObject("Agents");
            go.transform.SetParent(transform, false);
            agentsParent = go.transform;
        }
    }

    void TryFindGraphic()
    {
        if (graphic == null)
        {
            var t = transform.Find("Graphic");
            if (t != null) graphic = t;
        }
    }

    void UpdateGraphicScale()
    {
        if (graphic != null)
        {
            float d = spawnRadius * 2f;
            graphic.localScale = new Vector3(d, d, 1f);
        }
    }

    void UpdateCounter()
    {
        if (foodCounter != null)
            foodCounter.text = foodCollected.ToString();
    }

    public void SpawnAgent()
    {
        Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;

        var agent = Instantiate(agentPrefab, (Vector3)spawnPos, Quaternion.identity, agentsParent);

        if (agent.parameters == null && agentParams != null) agent.parameters = agentParams;
        if (agent.sensorOrigin == null) agent.sensorOrigin = agent.transform;

        agent.Init(this, homeMarkersField, foodMarkersField);
    }

    public void ReportFood()
    {
        foodCollected++;
        UpdateCounter();
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
