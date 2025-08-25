using UnityEngine;

[CreateAssetMenu(menuName = "Ants/Agent Parameters")]
public class AgentParameters : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────────────
    // POHYB
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Pohyb

    [Header("Movement")]
    [Tooltip("Maximální rychlost agenta (jednotky/s).")]
    public float maxSpeed = 2f;

    [Tooltip("Rychlost přibližování k cílové rychlosti.")]
    public float acceleration = 3f;

    [Tooltip("Max. délka integračního kroku pro sub-stepping (s). Menší = stabilnější při vysokém timeScale.")]
    public float maxIntegrationStep = 0.01f;

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // ŘÍZENÍ
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Řízení

    [Header("Steering")]
    [Tooltip("Základní síla řízení.")]
    public float steerStrength = 1.8f;

    [Tooltip("Síla cílového řízení na jídlo/hnízdo.")]
    public float targetSteerStrength = 3f;

    [Tooltip("Síla náhodného řízení (explorace).")]
    public float randomSteerStrength = 0.35f;

    [Tooltip("Maximální doba mezi změnami náhodného směru.")]
    public float randomSteerMaxDuration = 1.5f;

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VYHLEDÁVÁNÍ CÍLŮ
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Vyhledávání cílů

    [Header("Targeting / Search")]
    [Tooltip("Dosah pro vyhledávání jídla/hnízda.")]
    public float detectionRadius = 2.5f;

    [Tooltip("Efektivní dosah pro převzetí jídla.")]
    public float pickupDistance = 0.15f;

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // SENZORY FEROMONŮ
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Pheromone sensing

    [Header("Pheromone Sensors")]
    [Tooltip("Interval mezi vzorkováním senzorů (throttling).")]
    public float timeBetweenSensorUpdate = 0.10f;

    [Tooltip("Úhlové odchylky levého/pravého senzoru od směru (stupně).")]
    public float pheromoneSensorAngle = 25f;

    [Tooltip("Vzdálenost před agentem, kde se senzory odečítají.")]
    public float pheromoneSensorDistance = 1.25f;

    [Tooltip("Velikost vzorkovací oblasti senzoru (rádius).")]
    public float pheromoneSensorSize = 0.75f;

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // FEROMONOVÁ STOPA
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Pheromone trail

    [Header("Pheromone Trail")]
    [Tooltip("Vzdálenost mezi po sobě jdoucími „kapkami“ feromonu.")]
    public float pheromoneSpacing = 0.75f;

    [Tooltip("Doba, po které barvivo postupně dochází.")]
    public float pheromoneRunOutTime = 70f;

    [Tooltip("Čas odpařování feromonů v poli.")]
    public float pheromoneEvaporateTime = 100f;

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // KOLIZE A ANTÉNY
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Kolize / Antény

    [Header("Collisions / Antennas")]
    [Tooltip("Kolizní poloměr agenta.")]
    public float collisionRadius = 0.15f;

    [Tooltip("Síla odklonu při detekci překážky (antény/raycast).")]
    public float collisionAvoidSteerStrength = 5f;

    [Tooltip("Délka antén pro detekci překážky před agentem.")]
    public float antennaDistance = 0.25f;

    [Tooltip("Příčná vzdálenost mezi levou/pravou anténou.")]
    public float antennaOffset = 0.20f;

    [Header("Whiskers (vějíř paprsků)")]
    [Tooltip("Počet whisker paprsků na KAŽDÉ straně (kromě středového).")]
    public int whiskersPerSide = 2;

    [Tooltip("Poloviční FOV vějíře (stupně) od směru dopředu.")]
    public float whiskerFOV = 35f;

    [Tooltip("Násobič délky whisker paprsků vůči antennaDistance.")]
    public float whiskerDistanceMultiplier = 1.4f;

    [Tooltip("Jak dlouho držet vyhýbací sílu po detekci (s).")]
    public float avoidMemory = 0.25f;

    #endregion
}
