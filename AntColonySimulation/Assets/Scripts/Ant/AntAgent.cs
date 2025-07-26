using UnityEngine;
using Marker;
using World;

[DisallowMultipleComponent]
public class AntAgent : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 5f;
    [Range(0f, 1f)] public float steerStrength = 0.35f;
    [Range(0f, 0.5f * Mathf.PI)] public float directionNoiseRange = Mathf.PI * 0.02f;

    [Header("Timing")]
    [Min(0.01f)] public float directionUpdatePeriod = 0.25f;
    [Min(0.01f)] public float markerPeriod = 0.25f;

    [Header("Sensing")]
    [Min(0f)] public float senseRadius = 40f;
    [Range(1, 256)] public int sampleCount = 32;
    [Range(0.1f, Mathf.PI)] public float sampleAngleRange = Mathf.PI * 0.5f; // ±π/4
    [Min(0f)] public float intensityDecayCoef = 0.05f;
    [Min(0f)] public float markerBaseIntensity = 8000f;
    [Range(0.5f, 3f)] public float distanceFalloffPower = 1.0f;
    [Range(0f, 1f)] public float forwardBias = 0.5f;

    [Header("Home preference")]
    [Min(0f)] public float toHomeWeightMultiplier = 3f;
    [Min(0f)] public float nestAttraction = 20000f;
    [Range(0.5f, 3f)] public float nestDistPower = 1.0f;
    [Min(0f)] public float nestSnapDistance = 10f;

    [Header("Collision filtering")]
    public bool useRaycastObstacleCheck = false;
    public LayerMask obstacleMask = ~0;
    public float raycastDistance = 40f;
    public bool useBoundsPenalty = true;

    [Header("Pickup & Nest")]
    public float pickupRadius = 1f;
    public Transform nestTransform;

    private Vector2 directionVec;
    private float directionTimer;
    private float markerTimer;
    private AntMode phase = AntMode.ToFood;
    private PheromoneFactory pheromoneFactory;

    public enum AntMode { ToFood, ToHome }

    void Start()
    {
        pheromoneFactory = FindObjectOfType<PheromoneFactory>();
        float a = Random.Range(0f, 2f * Mathf.PI);
        directionVec = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    void Update()
    {
        float dt = Time.deltaTime;
        directionTimer += dt;
        markerTimer  += dt;

        Move(dt);

        if (phase == AntMode.ToFood) CheckFood();
        else CheckNest();

        if (directionTimer >= directionUpdatePeriod)
        {
            SenseAndSteer();
            directionTimer = 0f;
        }

        if (markerTimer >= markerPeriod)
        {
            DropMarker();
            markerTimer = 0f;
        }
    }

    private void Move(float dt)
    {
        Vector2 dirN = directionVec.sqrMagnitude > 1e-6f ? directionVec.normalized : Vector2.up;
        transform.position += (Vector3)(dirN * moveSpeed * dt);
        transform.up = dirN;

        BounceOnBoundsIfNeeded();
    }

    private void SenseAndSteer()
    {
        float coneHalf = sampleAngleRange * 0.5f;
        float baseAngle = Mathf.Atan2(directionVec.y, directionVec.x);

        Vector2 bestDir = directionVec;
        float bestScore = 0f;

        MarkerPheromoneType searchType =
            (phase == AntMode.ToHome) ? MarkerPheromoneType.ToHome : MarkerPheromoneType.ToFood;

        bool snappedToNest = false;
        if (nestTransform != null && phase == AntMode.ToHome)
        {
            Vector2 toNest = (Vector2)nestTransform.position - (Vector2)transform.position;
            float distNest = toNest.magnitude;
            if (distNest > 1e-3f)
            {
                Vector2 dirNest = toNest / distNest;
                float delta =
                    Mathf.Abs(Mathf.DeltaAngle(baseAngle * Mathf.Rad2Deg,
                        Mathf.Atan2(dirNest.y, dirNest.x) * Mathf.Rad2Deg)) * Mathf.Deg2Rad;

                float homeScore = (nestAttraction) / Mathf.Pow(distNest + 0.1f, nestDistPower);
                if (forwardBias > 0f)
                {
                    float cos = Mathf.Cos(delta);
                    homeScore *= Mathf.Lerp(1f, cos, forwardBias);
                }

                if (distNest <= nestSnapDistance && delta <= coneHalf)
                {
                    bestDir = dirNest;
                    bestScore = float.MaxValue * 0.5f;
                    snappedToNest = true;
                }
                else if (homeScore > bestScore)
                {
                    bestScore = homeScore;
                    bestDir = dirNest;
                }
            }
        }

        bool foundFood = false;

        if (!snappedToNest)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                float delta = Random.Range(-coneHalf, coneHalf);
                float ang = baseAngle + delta;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vector2 probe = (Vector2)transform.position + dir * senseRadius;

                float wallFactor = 1f;
                if (useRaycastObstacleCheck)
                {
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, raycastDistance, obstacleMask);
                    if (hit.collider != null && !hit.collider.TryGetComponent<PheromoneMarker>(out _))
                    {
                        continue;
                    }

                    if (hit.collider != null)
                    {
                        wallFactor = Mathf.Pow(hit.distance / Mathf.Max(1e-3f, raycastDistance), 2f);
                    }
                }

                if (useBoundsPenalty)
                {
                    wallFactor = Mathf.Min(wallFactor, BoundaryWallFactor(transform.position, dir));
                }

                if (phase == AntMode.ToFood)
                {
                    var foods = Physics2D.OverlapCircleAll(probe, senseRadius * 0.1f);
                    foreach (var f in foods)
                    {
                        if (f.TryGetComponent<FoodMarker>(out var fm) && fm.amount > 0)
                        {
                            bestDir = ((Vector2)f.transform.position - (Vector2)transform.position).normalized;
                            bestScore = float.MaxValue * 0.25f;
                            foundFood = true;
                            break;
                        }
                    }

                    if (foundFood)
                        break;
                }

                var cols = Physics2D.OverlapCircleAll(probe, senseRadius * 0.1f);
                foreach (var c in cols)
                {
                    if (!c.TryGetComponent<PheromoneMarker>(out var pm)) continue;
                    if (pm.type != searchType) continue;

                    float dist = Vector2.Distance(transform.position, pm.transform.position);
                    if (dist < 0.05f) continue;

                    float t = pm.TimeAliveNormalized * pm.lifetime;
                    float intensity = markerBaseIntensity * Mathf.Exp(-intensityDecayCoef * t);

                    float typeMul = (searchType == MarkerPheromoneType.ToHome) ? toHomeWeightMultiplier : 1f;

                    float score = typeMul * intensity / Mathf.Pow(dist + 0.1f, distanceFalloffPower);

                    if (forwardBias > 0f)
                    {
                        float cos = Mathf.Cos(Mathf.Abs(delta));
                        score *= Mathf.Lerp(1f, cos, forwardBias);
                    }

                    score *= wallFactor;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestDir = ((Vector2)pm.transform.position - (Vector2)transform.position).normalized;
                    }
                }
            }
        }
    }

    private void CheckFood()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius);
        foreach (var h in hits)
        {
            if (h.TryGetComponent<FoodMarker>(out var food) && food.TakeOne())
            {
                phase = AntMode.ToHome;
                directionVec = -directionVec;
                return;
            }
        }
    }

    private void CheckNest()
    {
        if (!nestTransform) return;
        if (Vector2.Distance(transform.position, nestTransform.position) <= pickupRadius)
        {
            phase = AntMode.ToFood;
            directionVec = -directionVec;
        }
    }

    private void DropMarker()
    {
        if (!pheromoneFactory) return;
        MarkerPheromoneType markerType =
            (phase == AntMode.ToFood) ? MarkerPheromoneType.ToHome : MarkerPheromoneType.ToFood;
        pheromoneFactory.SpawnMarker(transform.position, markerType);
    }
    
    private void BounceOnBoundsIfNeeded()
    {
        if (WorldBounds.Instance == null) return;
        Vector2 normal;
        Vector2 corrected = WorldBounds.Instance.ClampInsideAndGetNormal(transform.position, out normal);
        if ((Vector2)transform.position != corrected)
        {
            transform.position = corrected;
            if (normal != Vector2.zero)
            {
                directionVec = Vector2.Reflect(directionVec, normal).normalized;
            }
        }
    }

    private float BoundaryWallFactor(Vector2 origin, Vector2 dir)
    {
        if (WorldBounds.Instance == null) return 1f;
        if (!WorldBounds.Instance.RayToBoundary(origin, dir, out float t)) return 1f;
        float norm = Mathf.Clamp01(t / senseRadius);
        return norm * norm;
    }
}
