using UnityEngine;

[System.Serializable]
public class AntGenome
{
    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE A VÝCHOZÍ VLASTNOSTI
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Multiplikátory & Factory
    
    // Multiplikátory
    public float speedMult = 1f;
    public float accelMult = 1f;
    public float steerMult = 1f;
    public float sensorDistanceMult = 1f;
    public float randomSteerMult = 1f;
    public float pheromoneRunOutMult = 1f;
    public float pheromoneSpacingMult = 1f;

    // Factory
    public static AntGenome Create() => new AntGenome();

    // Náhoda
    public static AntGenome Random(GameRules r)
        => Create().WithRandomized(r);

    // Náhoda všech polí
    public AntGenome WithRandomized(GameRules r)
    {
        speedMult = UnityEngine.Random.Range(r.speedMult.x, r.speedMult.y);
        accelMult = UnityEngine.Random.Range(r.accelMult.x, r.accelMult.y);
        steerMult = UnityEngine.Random.Range(r.steerMult.x, r.steerMult.y);
        sensorDistanceMult = UnityEngine.Random.Range(r.sensorDistanceMult.x, r.sensorDistanceMult.y);
        randomSteerMult = UnityEngine.Random.Range(r.randomSteerMult.x, r.randomSteerMult.y);
        pheromoneRunOutMult = UnityEngine.Random.Range(r.pheromoneRunOutMult.x, r.pheromoneRunOutMult.y);
        pheromoneSpacingMult = UnityEngine.Random.Range(r.pheromoneSpacingMult.x, r.pheromoneSpacingMult.y);
        return this;
    }
    
    #endregion

    
    // ─────────────────────────────────────────────────────────────────────────────
    // OPERACE S GENOMEM
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Operace

    // Fluent settery
    public AntGenome WithSpeed(float m) { speedMult = m; return this; }
    public AntGenome WithAccel(float m) { accelMult = m; return this; }
    public AntGenome WithSteer(float m) { steerMult = m; return this; }
    public AntGenome WithSensorDistance(float m) { sensorDistanceMult = m; return this; }
    public AntGenome WithRandomSteer(float m) { randomSteerMult = m; return this; }
    public AntGenome WithPheroRunOut(float m) { pheromoneRunOutMult = m; return this; }
    public AntGenome WithPheroSpacing(float m) { pheromoneSpacingMult = m; return this; }

    // Ořez extrémů
    public AntGenome Clamp(float min = 0.1f, float max = 10f)
    {
        speedMult = Mathf.Clamp(speedMult, min, max);
        accelMult = Mathf.Clamp(accelMult, min, max);
        steerMult = Mathf.Clamp(steerMult, min, max);
        sensorDistanceMult = Mathf.Clamp(sensorDistanceMult, min, max);
        randomSteerMult = Mathf.Clamp(randomSteerMult, min, max);
        pheromoneRunOutMult = Mathf.Clamp(pheromoneRunOutMult,  min, max);
        pheromoneSpacingMult = Mathf.Clamp(pheromoneSpacingMult, min, max);
        return this;
    }

    // Aplikace na AgentParameters
    public AgentParameters ApplyTo(AgentParameters p)
    {
        if (!p) return p;
        p.maxSpeed *= speedMult;
        p.acceleration *= accelMult;
        p.steerStrength *= steerMult;
        p.pheromoneSensorDistance *= sensorDistanceMult;
        p.randomSteerStrength *= randomSteerMult;
        p.pheromoneRunOutTime *= pheromoneRunOutMult;
        p.pheromoneSpacing *= pheromoneSpacingMult;
        return p;
    }

    // Kombinace genomů (nepoužíváme, dal jsem to jako možné rozšíření)
    public AntGenome Multiply(AntGenome other)
    {
        if (other == null) return this;
        speedMult *= other.speedMult;
        accelMult *= other.accelMult;
        steerMult *= other.steerMult;
        sensorDistanceMult *= other.sensorDistanceMult;
        randomSteerMult *= other.randomSteerMult;
        pheromoneRunOutMult *= other.pheromoneRunOutMult;
        pheromoneSpacingMult *= other.pheromoneSpacingMult;
        return this;
    }
}

#endregion

// ─────────────────────────────────────────────────────────────────────────────
// RANDOMIZER + EXTENSIONS
// ─────────────────────────────────────────────────────────────────────────────
#region — Randomizer & Extensions

public sealed class AntGenomeRandomizer
{
    readonly AntGenome g;

    // Vytvoří builder pro daný genom.
    public AntGenomeRandomizer(AntGenome g)
    {
        this.g = g;
    }

    // Random speed v zadaném intervalu.
    public AntGenomeRandomizer Speed(Vector2 range)
    { g.speedMult = Random.Range(range.x, range.y); return this; }

    // Random acceleration v zadaném intervalu.
    public AntGenomeRandomizer Accel(Vector2 range)
    { g.accelMult = Random.Range(range.x, range.y); return this; }

    // Random steer v zadaném intervalu.
    public AntGenomeRandomizer Steer(Vector2 range)
    { g.steerMult = Random.Range(range.x, range.y); return this; }

    // Random vzdálenost senzorů v zadaném intervalu.
    public AntGenomeRandomizer SensorDistance(Vector2 range)
    { g.sensorDistanceMult = Random.Range(range.x, range.y); return this; }

    // Random random-steer sílu v zadaném intervalu.
    public AntGenomeRandomizer RandomSteer(Vector2 range)
    { g.randomSteerMult = Random.Range(range.x, range.y); return this; }

    // Rnadom run-out času feromonů.
    public AntGenomeRandomizer PheroRunOut(Vector2 range)
    { g.pheromoneRunOutMult = Random.Range(range.x, range.y); return this; }

    // Random rozestup mezi kapkami feromonů.
    public AntGenomeRandomizer PheroSpacing(Vector2 range)
    { g.pheromoneSpacingMult = Random.Range(range.x, range.y); return this; }

    // Z GameRules random všechny podporované parametry.
    public AntGenomeRandomizer FromRules(GameRules r)
    {
        return Speed(r.speedMult)
             .Accel(r.accelMult)
             .Steer(r.steerMult)
             .SensorDistance(r.sensorDistanceMult)
             .RandomSteer(r.randomSteerMult)
             .PheroRunOut(r.pheromoneRunOutMult)
             .PheroSpacing(r.pheromoneSpacingMult);
    }

    // Vrátí genom.
    public AntGenome Done() => g;
}


// ————————————  Extensiony   ————————————

public static class AntGenomeFluentExtensions
{
    public static AntGenomeRandomizer Rand(this AntGenome g) => new (g);

    public static AgentParameters Apply(this AgentParameters p, AntGenome g)
        => g?.ApplyTo(p);
}

#endregion
