using System.Collections.Generic;
using UnityEngine;

public class TeamManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // KONSTANTY A STATISTIKY
    // ─────────────────────────────────────────────────────────────────────────────
    #region — konstanty

    public static TeamManager Instance { get; private set; }

    public const int MaxTeams = 5;

    public static readonly string[] TeamNames = { "Red","Blue","Green","Yellow","Purple" };
    public static readonly Color[]  TeamColors = {
        new (0.92f,0.22f,0.22f),
        new (0.22f,0.48f,0.92f),
        new (0.20f,0.80f,0.35f),
        new (0.95f,0.83f,0.25f),
        new (0.70f,0.40f,0.85f),
    };

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Konfigurace z Inspectoru

    [Header("Defaults for pheromone fields")]
    public PheromoneField defaultHomeFieldPrefab;
    public PheromoneField defaultFoodFieldPrefab;
    public Transform fieldsRoot;

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // DATOVÉ STRUKTURY
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Data

    public class TeamData
    {
        public int   teamId;
        public string teamName;
        public Color teamColor;

        public readonly List<NestController> nests = new();

        public PheromoneField homeField;
        public PheromoneField foodField;

        public int totalFoodCollected;
        public int currentAnts;
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VNITŘNÍ STAV
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Vnitřní stav

    private readonly Dictionary<int, TeamData> teams = new();

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity lifecycle

    // Nastaví a připraví záznamy pro všechny týmy a vytvoří root pro pole.
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < MaxTeams; i++)
        {
            teams[i] = new TeamData {
                teamId = i,
                teamName = TeamNames[i],
                teamColor = TeamColors[i]
            };
        }

        if (!fieldsRoot)
        {
            var go = new GameObject("__TeamPheromoneFields__");
            fieldsRoot = go.transform;
        }
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // REGISTRACE A VYHLEDÁVÁNÍ
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Registrace / Lookup

    // Zaregistruje hnízdo do příslušného týmového seznamu.
    public void RegisterNest(int teamId, NestController nest)
    {
        if (!teams.TryGetValue(teamId, out var t) || nest == null) return;
        if (!t.nests.Contains(nest)) t.nests.Add(nest);
    }

    // Vrátí týmová feromonová pole pro daný tým.
    public (PheromoneField home, PheromoneField food) GetOrCreateTeamFields(
        int teamId, PheromoneField homePrefab, PheromoneField foodPrefab, Transform parentFallback)
    {
        if (!teams.TryGetValue(teamId, out var t)) return (null, null);

        var root = fieldsRoot ? fieldsRoot : parentFallback;

        var hp = homePrefab ? homePrefab : defaultHomeFieldPrefab;
        var fp = foodPrefab ? foodPrefab : defaultFoodFieldPrefab;

        
        
        if (t.homeField == null) t.homeField = Instantiate(hp, Vector3.zero, Quaternion.identity, root);
        if (t.foodField == null) t.foodField = Instantiate(fp, Vector3.zero, Quaternion.identity, root);

        if (t.homeField) t.homeField.SetVisible(true);
        if (t.foodField) t.foodField.SetVisible(true);

        return (t.homeField, t.foodField);
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // STATISTIKY A POČTY
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Statistika

    // Přičte množství jídla do statistik týmu.
    public void AddFood(int teamId, int amount)
    {
        if (teams.TryGetValue(teamId, out var t)) t.totalFoodCollected += amount;
    }

    // Zaregistruje nového mravence.
    public void RegisterAnt(int teamId)
    {
        if (teams.TryGetValue(teamId, out var t)) t.currentAnts++;
    }

    // Odregistruje mravence.
    public void UnregisterAnt(int teamId)
    {
        if (teams.TryGetValue(teamId, out var t)) t.currentAnts = Mathf.Max(0, t.currentAnts - 1);
    }

    // Vrátí aktuální počet mravenců v týmu.
    public int GetTeamAntCount(int teamId)
    {
        return teams.TryGetValue(teamId, out var t) ? t.currentAnts : 0;
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // GETTERY (BARVA, JMÉNO, JÍDLO, CELÁ MAPA)
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Gettery

    // Vrátí celkové množství jídla nasbírané týmem.
    public int GetTeamFoodCount(int teamId) => teams.TryGetValue(teamId, out var t) ? t.totalFoodCollected : 0;

    // Vrátí barvu týmu.
    public Color GetTeamColor(int teamId) => teams.TryGetValue(teamId, out var t) ? t.teamColor : Color.white;

    // Vrátí název týmu.
    public string GetTeamName(int teamId) => teams.TryGetValue(teamId, out var t) ? t.teamName : $"Team {teamId}";

    // Vrátí referenci na celou tabulku týmů.
    public Dictionary<int, TeamData> GetAll() => teams;

    #endregion
}
