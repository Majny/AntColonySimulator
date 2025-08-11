using System.Collections.Generic;
using UnityEngine;

public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance { get; private set; }

    public const int MaxTeams = 5;

    public static readonly string[] TeamNames = { "Red","Blue","Green","Yellow","Purple" };
    public static readonly Color[] TeamColors = {
        new Color(0.92f,0.22f,0.22f),
        new Color(0.22f,0.48f,0.92f),
        new Color(0.20f,0.80f,0.35f),
        new Color(0.95f,0.83f,0.25f),
        new Color(0.70f,0.40f,0.85f),
    };

    [Header("Defaults for pheromone fields (used if nest passes null)")]
    public PheromoneField defaultHomeFieldPrefab;
    public PheromoneField defaultFoodFieldPrefab;
    public Transform fieldsRoot;

    public class TeamData
    {
        public int teamId;
        public string teamName;
        public Color teamColor;

        public readonly List<NestController> nests = new();

        public PheromoneField homeField;
        public PheromoneField foodField;

        public int totalFoodCollected;
        public int currentAnts;
    }

    private readonly Dictionary<int, TeamData> teams = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        for (int i = 0; i < MaxTeams; i++)
        {
            teams[i] = new TeamData {
                teamId = i, teamName = TeamNames[i], teamColor = TeamColors[i]
            };
        }

        if (!fieldsRoot)
        {
            var go = new GameObject("__TeamPheromoneFields__");
            fieldsRoot = go.transform;
        }
    }

    public void RegisterNest(int teamId, NestController nest)
    {
        if (!teams.TryGetValue(teamId, out var t) || nest == null) return;
        if (!t.nests.Contains(nest)) t.nests.Add(nest);
    }

    public (PheromoneField home, PheromoneField food) GetOrCreateTeamFields(
        int teamId, PheromoneField homePrefab, PheromoneField foodPrefab, Transform parentFallback)
    {
        if (!teams.TryGetValue(teamId, out var t)) return (null, null);

        var root = fieldsRoot ? fieldsRoot : parentFallback;

        var hp = homePrefab ? homePrefab : defaultHomeFieldPrefab;
        var fp = foodPrefab ? foodPrefab : defaultFoodFieldPrefab;

        if (t.homeField == null)
        {
            if (!hp) Debug.LogError($"[TeamManager] Missing default homeFieldPrefab (team {teamId}).");
            else t.homeField = Instantiate(hp, Vector3.zero, Quaternion.identity, root);
        }
        if (t.foodField == null)
        {
            if (!fp) Debug.LogError($"[TeamManager] Missing default foodFieldPrefab (team {teamId}).");
            else t.foodField = Instantiate(fp, Vector3.zero, Quaternion.identity, root);
        }

        if (t.homeField) t.homeField.SetVisible(true);
        if (t.foodField) t.foodField.SetVisible(true);

        return (t.homeField, t.foodField);
    }

    public void AddFood(int teamId, int amount)
    {
        if (teams.TryGetValue(teamId, out var t)) t.totalFoodCollected += amount;
    }

    public void RegisterAnt(int teamId)
    {
        if (teams.TryGetValue(teamId, out var t)) t.currentAnts++;
    }
    public void UnregisterAnt(int teamId)
    {
        if (teams.TryGetValue(teamId, out var t)) t.currentAnts = Mathf.Max(0, t.currentAnts - 1);
    }
    public int GetTeamAntCount(int teamId)
    {
        return teams.TryGetValue(teamId, out var t) ? t.currentAnts : 0;
    }

    public int GetTeamFoodCount(int teamId) => teams.TryGetValue(teamId, out var t) ? t.totalFoodCollected : 0;
    public Color GetTeamColor(int teamId) => teams.TryGetValue(teamId, out var t) ? t.teamColor : Color.white;
    public string GetTeamName(int teamId) => teams.TryGetValue(teamId, out var t) ? t.teamName : $"Team {teamId}";

    public Dictionary<int, TeamData> GetAll() => teams;
}
