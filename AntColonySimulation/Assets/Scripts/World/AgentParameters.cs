using UnityEngine;

[CreateAssetMenu(menuName = "Simulation/Agent Parameters")]
public class AgentParameters : ScriptableObject
{
    public float maxSpeed = 2f;
    public float acceleration = 2f;
    public float detectionRadius = 1.5f;

    public float lifespan = 100f;
    public bool enableDeath = false;
    
    public float randomTurnAngle = 25f; 
    public float randomWiggleInterval = 1.5f; 

    public float avoidanceDistance = 0.5f; 

}