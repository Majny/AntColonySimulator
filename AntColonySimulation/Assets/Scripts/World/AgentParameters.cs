using UnityEngine;

[CreateAssetMenu(menuName = "Ants/Agent Parameters")]
public class AgentParameters : ScriptableObject
{
    public float maxSpeed = 2f;
    public float acceleration = 3f;

    public float steerStrength = 1.8f;
    public float targetSteerStrength = 3f;

    public float detectionRadius = 2.5f;
    public float pickupDistance = 0.15f;

    public float timeBetweenSensorUpdate = 0.10f;
    public float pheromoneSensorAngle = 25f;
    public float pheromoneSensorDistance = 1.25f;
    public float pheromoneSensorSize = 0.75f;

    public float randomSteerStrength = 0.35f;
    public float randomSteerMaxDuration = 1.5f;

    public float pheromoneSpacing = 0.75f;
    public float pheromoneRunOutTime = 70f;
    public float pheromoneEvaporateTime = 100f;

    public float collisionRadius = 0.15f;
    public float collisionAvoidSteerStrength = 5f;
    public float antennaDistance = 0.25f;
    public float antennaOffset = 0.20f;

}