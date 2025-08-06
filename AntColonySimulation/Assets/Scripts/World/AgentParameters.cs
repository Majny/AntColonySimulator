using UnityEngine;

[CreateAssetMenu(menuName = "Ants/Agent Parameters")]
public class AgentParameters : ScriptableObject
{
    public float maxSpeed = 2f;
    public float acceleration = 3f;
    public float steerStrength = 1f; 
    public float targetSteerStrength = 3f;

    public float detectionRadius = 2.5f; 
    public float pickupDistance = 0.15f;

    public float randomSteerStrength = 0.6f;
    public float randomSteerMaxDuration = 1.0f;
    public float timeBetweenSensorUpdate = 0.15f;

    public float pheromoneSpacing = 0.1f;
    public float pheromoneRunOutTime = -1f;
    public float pheromoneEvaporateTime = 99999999f;

    public float pheromoneSensorAngle = 25f;
    public float pheromoneSensorDistance = 1.25f;
    public float pheromoneSensorRadius = 0.75f;

    public float collisionRadius = 0.15f;
    public float collisionAvoidSteerStrength = 5f;
    public float antennaDistance = 0.25f;
    public float antennaOffset = 0.20f;

    public float randomWiggleInterval = 1f;
    public float randomTurnAngle = 10f;
}