using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class NestController : MonoBehaviour
{
    [Header("Agent setup")]
    public AgentParameters agentParams;
    public AntAgent agentPrefab;
    public int initialAgents = 10;
    public Transform agentsParent;

    [Header("Team")]
    public int teamId;
    public Color teamColor = Color.white;

    [Header("Pheromone fields (per TEAM, not global)")]
    public PheromoneField homeFieldPrefab;
    public PheromoneField foodFieldPrefab;

    [Header("Spawn")]
    public float spawnRadius = 0.25f;

    [Header("Graphics (optional)")]
    public Transform graphic;
    public TextMeshPro foodCounter;

    int foodCollected;

    PheromoneField teamHomeField;
    PheromoneField teamFoodField;

    public int TeamId => teamId;
    public PheromoneField TeamHomeField => teamHomeField;
    public PheromoneField TeamFoodField => teamFoodField;

    void Awake()
    {
        EnsureAgentsParent();
        TryFindGraphic();
        UpdateGraphicScale();
        UpdateCounter();
    }

    void Start()
    {
        if (TeamManager.Instance)
        {
            var (home, food) = TeamManager.Instance.GetOrCreateTeamFields(
                teamId, homeFieldPrefab, foodFieldPrefab, transform.parent);
            teamHomeField = home;
            teamFoodField = food;

            TeamManager.Instance.RegisterNest(teamId, this);
        }
        else
        {
            Debug.LogWarning("TeamManager not found in scene.");
        }

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
        if (foodCounter != null) foodCounter.text = foodCollected.ToString();
    }

    public void SpawnAgent()
    {
        Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;

        var agent = Instantiate(agentPrefab, (Vector3)spawnPos, Quaternion.identity, agentsParent);

        if (agent.parameters == null && agentParams != null) agent.parameters = agentParams;
        if (agent.sensorOrigin == null) agent.sensorOrigin = agent.transform;

        agent.Init(this, teamHomeField, teamFoodField);

        var rends = agent.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in rends) r.color = teamColor;

        TeamManager.Instance?.RegisterAnt(teamId);
    }

    public void ReportFood()
    {
        foodCollected++;
        UpdateCounter();
        TeamManager.Instance?.AddFood(teamId, 1);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
#endif
}
