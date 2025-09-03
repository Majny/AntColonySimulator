using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameRules : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Globální přístup k pravidlům v běhu
    // ─────────────────────────────────────────────────────────────────────────────
    public static GameRules Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU (přepínače a parametry)
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Konfigurace z Inspectoru

    [Header("Toggles")]
    public bool simulationOfLife = false;                   // Když je zapnuto, jídlo se konvertuje na nové agenty (viz NestController.ReportFood)
    public bool upgradedAnts = false;                       // Když je zapnuto, nově spawnovaní mravenci dostanou náhodný genom (multiplikátory)

    [Header("Simulation of life")]
    [Min(1)] public int foodPerNewAnt = 5;                  // Kolik jednotek jídla je potřeba na spawn jednoho nového agenta

    [Header("Upgraded Ants – ranges (multipliers)")]
    // Každá dvojice (x,y) představuje interval <min,max>, ze kterého si genom vybírá multiplikátor pro daný parametr.
    public Vector2 speedMult = new (0.9f, 1.3f);            // maxSpeed
    public Vector2 accelMult = new (0.9f, 1.2f);            // acceleration
    public Vector2 steerMult = new (0.9f, 1.2f);            // steerStrength / targetSteer
    public Vector2 sensorDistanceMult = new (0.8f, 1.4f);   // vzdálenost senzorů feromonů
    public Vector2 randomSteerMult = new (0.9f, 1.3f);      // síla náhodného řízení
    public Vector2 pheromoneRunOutMult = new (0.8f, 1.2f);  // doba do vyprchání
    public Vector2 pheromoneSpacingMult = new (0.8f, 1.2f); // rozestup mezi kapkami

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VEŘEJNÉ API
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Veřejné API

    /// Vytvoří náhodný genom podle zadaných intervalů multiplikátorů.
    public AntGenome GenerateGenome()
    {
        return AntGenome.Create()
            .Rand()
            .FromRules(this) // náhodní všechny podporované multiplikátory z rozsahů v GameRules
            .Done()
            .Clamp(0.5f, 2.0f); // volitelný ořez extrémů
    }
    
    
    #endregion
}
