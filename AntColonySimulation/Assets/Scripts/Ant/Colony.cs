using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class NestController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Konfigurace z Inspectoru

    [Header("Agent setup")]
    public AgentParameters agentParams;   // Výchozí parametry chování pro agenty (lze později modifikovat genomem)
    public AntAgent agentPrefab;          // Prefab mravence
    public int initialAgents = 10;        // Počet agentů na startu
    public Transform agentsParent;        // Parent všech agentů (kvůli přehledu v Hierarchy)

    [Header("Team")]
    public int teamId;                       // ID týmu (pro sdílení polí a skóre)
    public Color teamColor = Color.white; // Barva týmu

    [Header("Pheromone fields (per TEAM)")]
    public PheromoneField homeFieldPrefab; // Prefab týmového pole "ToHome"
    public PheromoneField foodFieldPrefab; // Prefab týmového pole "ToFood"

    [Header("Spawn")]
    public float spawnRadius = 0.25f;     // Poloměr kruhu kolem hnízda, kde se spawnujou agenti

    [Header("Graphics (optional)")]
    public Transform graphic;             // Volitelný vizuál hnízda
    public TextMeshPro foodCounter;       // Volitelný counter nasbíraného jídla

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // INTERNÍ STAV A ODKAZY
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Interní stav

    int foodCollected; // Celkové jídlo nasbírané tímto hnízdem
    int foodSinceLastSpawn; // Akumulované jídlo od posledního auto-spawnu (Simulation of Life)

    PheromoneField teamHomeField; // Sdílené týmové pole ToHome
    PheromoneField teamFoodField; // Sdílené týmové pole ToFood

    // Veřejné read-only vlastnosti
    public int TeamId => teamId;

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // ŽIVOTNÍ CYKLUS UNITY (Awake/Start/Validate)
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity lifecycle

    void Awake()
    {
        // Připrav strukturu v Hierarchy a grafiku
        EnsureAgentsParent(); // vytvoří child "Agents", pokud chybí
        TryFindGraphic(); // pokusí se dohledat Transform "Graphic", pokud není nastaven
        UpdateGraphicScale(); // přizpůsobí velikost grafiky podle spawnRadius
        UpdateCounter(); // inicializuje text counteru
    }

    void Start()
    {
        // Získání/zakládání týmových feromonových polí přes TeamManager
        var (home, food) = TeamManager.Instance.GetOrCreateTeamFields(teamId, homeFieldPrefab, foodFieldPrefab, transform.parent);
        teamHomeField = home; 
        teamFoodField = food;
        TeamManager.Instance.RegisterNest(teamId, this); // registrace hnízda v manageru
        
        // Počáteční spawn agentů
        for (int i = 0; i < initialAgents; i++)
            SpawnAgent();
    }

    #if UNITY_EDITOR
    void OnValidate()
    {
        // Editor-only: udržujeme grafiku v konzistenci i při změnách hodnot v Inspectoru
        TryFindGraphic();
        UpdateGraphicScale();
    }
    #endif

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // HELPERY PRO STRUKTURU A GRAFIKU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Hierarchy, grafika, UI

    // Zajistí existenci rodiče pro agenty
    void EnsureAgentsParent()
    {
        if (agentsParent == null)
        {
            var go = new GameObject("Agents");
            go.transform.SetParent(transform, false);
            agentsParent = go.transform;
        }
    }

    // Pokusí se automaticky najít podobjekt Grafic, pokud ho nemáme ručně nastavený
    void TryFindGraphic()
    {
        if (graphic == null)
        {
            var t = transform.Find("Graphic");
            if (t != null) graphic = t;
        }
    }

    // Škáluje grafiku hnízda tak, aby odpovídala spawn zóně
    void UpdateGraphicScale()
    {
        if (graphic != null)
        {
            float d = spawnRadius * 2f;
            graphic.localScale = new Vector3(d, d, 1f);
        }
    }

    // Aktualizace textového counteru jídla
    void UpdateCounter()
    {
        if (foodCounter != null)
            foodCounter.text = foodCollected.ToString();
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // SPAWN AGENTŮ
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Spawn agentů

    public void SpawnAgent()
    {
        if (!agentPrefab) return;

        // Náhodná pozice v disku kolem hnízda
        Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;

        // Vytvoř nového agenta
        var agent = Instantiate(agentPrefab, spawnPos, Quaternion.identity, agentsParent);

        // Volba parametrů: buď přímo z Inspectoru, nebo vylepšené přes GameRules
        AgentParameters chosenParams = agentParams;
        var rules = GameRules.Instance;

        if (rules && rules.upgradedAnts && agentParams != null)
        {
            // Vytvoříme instanci paramů pro tohoto agenta a aplikujeme vygenerovaný genom
            chosenParams = Instantiate(agentParams)
                .Apply(rules.GenerateGenome());
        }

        agent.parameters = chosenParams;

        // Inicializace agenta s referencemi na hnízdo a týmová pole
        agent.Init(this, teamHomeField, teamFoodField);

        // Barevné označení týmu
        var rends = agent.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in rends) r.color = teamColor;

        // Registrace agenta v TeamManageru
        TeamManager.Instance.RegisterAnt(teamId);
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // REPORTOVÁNÍ JÍDLA A SIMULATION OF LIFE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Report jídla a auto-spawn

    // Volá AntAgent po odevzdání jídla v hnízdě
    public void ReportFood()
    {
        foodCollected++;
        UpdateCounter();

        // Globální inkrement v TeamManageru
        TeamManager.Instance.AddFood(teamId, 1);

        // Simulation of Life: pokud je zapnuto, jídlo se konvertuje na nové agenty
        var rules = GameRules.Instance;
        if (rules && rules.simulationOfLife)
        {
            foodSinceLastSpawn++;
            int need = Mathf.Max(1, rules.foodPerNewAnt); // kolik jídla je potřeba na jednoho agenta

            while (foodSinceLastSpawn >= need)
            {
                foodSinceLastSpawn -= need;
                SpawnAgent();
            }
        }
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // DEBUG / EDITOR VIZUALIZACE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Gizmos (Editor)

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
    #endif

    #endregion
}
