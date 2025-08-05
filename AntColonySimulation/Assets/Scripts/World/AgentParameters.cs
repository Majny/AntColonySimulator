using UnityEngine;

[CreateAssetMenu(menuName = "Ants/Agent Parameters")]
public class AgentParameters : ScriptableObject
{
    public float maxSpeed = 2f;
    public float acceleration = 6f;
    public float steerStrength = 1f;

    public float detectionRadius = 0.5f;
    public float pickupDistance = 0.15f;
    public float targetSteerStrength = 1.5f;

    public float randomWiggleInterval = 1f;
    public float randomTurnAngle = 10f;

    public float randomSteerStrength = 0.8f;
    public float randomSteerMaxDuration = 1.5f;

    public float pheromoneSpacing = 0.2f;
    public float pheromoneDecayTime = 8f;
    public float pheromoneSensorAngle = 25f;
    public float pheromoneSensorDistance = 0.4f;
    public float pheromoneSensorRadius = 0.3f;

    public float timeBetweenSensorUpdate = 0.1f;
}