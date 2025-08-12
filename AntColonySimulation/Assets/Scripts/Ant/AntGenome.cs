using UnityEngine;

[System.Serializable]
public class AntGenome
{
    public float speedMult = 1f;
    public float accelMult = 1f;
    public float steerMult = 1f;
    public float sensorDistanceMult = 1f;
    public float randomSteerMult = 1f;
    public float pheromoneRunOutMult = 1f;
    public float pheromoneSpacingMult = 1f;

    public static AntGenome Random(GameRules r)
    {
        var g = new AntGenome();
        g.speedMult = UnityEngine.Random.Range(r.speedMult.x, r.speedMult.y);
        g.accelMult = UnityEngine.Random.Range(r.accelMult.x, r.accelMult.y);
        g.steerMult = UnityEngine.Random.Range(r.steerMult.x, r.steerMult.y);
        g.sensorDistanceMult = UnityEngine.Random.Range(r.sensorDistanceMult.x, r.sensorDistanceMult.y);
        g.randomSteerMult = UnityEngine.Random.Range(r.randomSteerMult.x, r.randomSteerMult.y);
        g.pheromoneRunOutMult = UnityEngine.Random.Range(r.pheromoneRunOutMult.x, r.pheromoneRunOutMult.y);
        g.pheromoneSpacingMult = UnityEngine.Random.Range(r.pheromoneSpacingMult.x,r.pheromoneSpacingMult.y);
        return g;
    }

    public void ApplyTo(AgentParameters p)
    {
        if (!p) return;
        p.maxSpeed *= speedMult;
        p.acceleration *= accelMult;
        p.steerStrength *= steerMult;
        p.pheromoneSensorDistance*= sensorDistanceMult;
        p.randomSteerStrength *= randomSteerMult;
        p.pheromoneRunOutTime *= pheromoneRunOutMult;
        p.pheromoneSpacing *= pheromoneSpacingMult;
    }
}