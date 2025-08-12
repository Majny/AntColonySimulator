using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameRules : MonoBehaviour
{
    public static GameRules Instance { get; private set; }

    [Header("Toggles")]
    public bool simulationOfLife = false;
    public bool upgradedAnts = false;

    [Header("Simulation of life")]
    [Min(1)] public int foodPerNewAnt = 5;

    [Header("Upgraded Ants – ranges (multipliers)")]
    public Vector2 speedMult = new Vector2(0.9f, 1.3f);
    public Vector2 accelMult = new Vector2(0.9f, 1.2f);
    public Vector2 steerMult = new Vector2(0.9f, 1.2f);
    public Vector2 sensorDistanceMult = new Vector2(0.8f, 1.4f);
    public Vector2 randomSteerMult = new Vector2(0.9f, 1.3f);
    public Vector2 pheromoneRunOutMult = new Vector2(0.8f, 1.2f);
    public Vector2 pheromoneSpacingMult = new Vector2(0.8f, 1.2f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public AntGenome GenerateGenome()
    {
        if (!upgradedAnts) return null;
        return AntGenome.Random(this);
    }
}