using UnityEngine;


public class AntAgent : MonoBehaviour
{
    [Header("Config & refs")]
    public AgentParameters parameters;
    public Transform sensorOrigin;
    public Transform head;
    public LayerMask foodLayer;
    public LayerMask nestLayer;
    public LayerMask obstacleMask;

    PheromoneField homeField;
    PheromoneField foodField;

    private NestController nestRef;
    PheromoneField playArea;
    public void SetPlayArea(PheromoneField f) => playArea = f;

    AntMode mode;
    Transform carriedItem;
    Transform targetFood;

    Vector2 heading, velocity;
    Vector2 randomSteer, pheromoneSteer, targetSteer;
    Vector2 obstacleAvoidForce;

    float nextRandomSteerTime;

    Vector2 lastPheromonePos;
    float timeSinceLeftNest, timeSinceLeftFood;

    bool isTurning;
    Vector2 turnSteerForce;
    float turnEndTime;

    float nextSensorSampleTime;

    enum Antenna { None, Left, Right }
    Antenna lastAntennaHit = Antenna.None;
    float obstacleResetTime;

    readonly Collider2D[] foodBuffer = new Collider2D[4];

    #region — Initializace

    public void Init(NestController nest, PheromoneField home, PheromoneField food)
    {
        nestRef = nest;
        homeField = home;
        foodField = food;

        if (!sensorOrigin) sensorOrigin = transform;

        transform.rotation = Quaternion.Euler(0, 0, Random.value * 360f);
        heading = transform.right;
        velocity = heading * parameters.maxSpeed;
        mode = AntMode.ToFood;

        lastPheromonePos = transform.position;
        timeSinceLeftNest = Time.time;

        ScheduleNextRandomSteer();
        nextSensorSampleTime = Random.value * parameters.timeBetweenSensorUpdate;
    }

    #endregion
    #region — Unity Loop

    void Update ()
    {
        PlacePheromoneIfNeeded();
        HandleRandomSteering();
        HandlePheromoneSteering();

        if (mode == AntMode.ToFood) HandleFoodSeeking();
        else HandleReturnHome();

        HandleCollisionSteering();
        IntegrateMovement();
    }

    void LateUpdate() => EnforcePlayArea();

    #endregion
    
    #region — Movement

    void IntegrateMovement ()
    {
        Vector2 steer = randomSteer + pheromoneSteer + targetSteer + obstacleAvoidForce;

        if (isTurning)
        {
            steer += turnSteerForce * parameters.targetSteerStrength;
            if (Time.time > turnEndTime) isTurning = false;
        }

        steer = steer.sqrMagnitude > 1e-6f ? steer.normalized : heading;
        Vector2 dv = steer * parameters.maxSpeed;
        velocity = Vector2.Lerp(velocity, dv, parameters.acceleration * Time.deltaTime);

        float probe = Mathf.Max(parameters.collisionRadius, Mathf.Max(parameters.antennaDistance, velocity.magnitude * Time.deltaTime));

        if (Physics2D.Raycast(transform.position, heading, probe, obstacleMask))
            StartTurnAround();

        transform.position += (Vector3)(velocity * Time.deltaTime);
        heading = velocity.normalized;
        transform.right = heading;
    }

    #endregion
    
    #region — Random Steering

    void HandleRandomSteering ()
    {
        if (targetFood) { randomSteer = Vector2.zero; return; }

        if (Time.time > nextRandomSteerTime)
        {
            ScheduleNextRandomSteer();
            randomSteer = BestRandomDir(heading, 5) * parameters.randomSteerStrength;
        }
    }
    void ScheduleNextRandomSteer() =>
        nextRandomSteerTime = Time.time +
            Random.Range(parameters.randomSteerMaxDuration / 3f, parameters.randomSteerMaxDuration);

    static Vector2 BestRandomDir (Vector2 refDir, int tries)
    {
        Vector2 best = Vector2.zero; float bestDot = -1;
        for (int i = 0; i < tries; i++)
        {
            Vector2 r = Random.insideUnitCircle.normalized;
            float d = Vector2.Dot(refDir, r);
            if (d > bestDot) { bestDot = d; best = r; }
        }
        return best;
    }

    #endregion
    
    #region — Pheromones

    void PlacePheromoneIfNeeded ()
    {
        if (Vector2.Distance(transform.position, lastPheromonePos) <= parameters.pheromoneSpacing)
            return;

        float legTime = (mode == AntMode.ToHome) ? Time.time - timeSinceLeftFood : Time.time - timeSinceLeftNest;

        if (parameters.pheromoneRunOutTime > 0 &&
            legTime > parameters.pheromoneRunOutTime)
            return;
        float t = (parameters.pheromoneRunOutTime <= 0f) ? 1f : 1f - (legTime / parameters.pheromoneRunOutTime);
        float strength = Mathf.Lerp(0.5f, 1f, t);

        if (mode == AntMode.ToFood) homeField?.Add(transform.position, strength);
        else foodField?.Add(transform.position, strength);

        lastPheromonePos = (Vector2)transform.position + Random.insideUnitCircle * parameters.pheromoneSpacing * 0.2f;
    }

    #endregion
    
    #region — Pheromone Steering

    void HandlePheromoneSteering ()
    {
        if (Time.time < nextSensorSampleTime) return;
        nextSensorSampleTime = Time.time + parameters.timeBetweenSensorUpdate;

        float a = parameters.pheromoneSensorAngle;
        float L = SampleSensor(-a);
        float C = SampleSensor( 0);
        float R = SampleSensor( a);

        if (L > C && L > R) pheromoneSteer = Rotate(heading, -a) * parameters.steerStrength;
        else if (R > C) pheromoneSteer = Rotate(heading, a) * parameters.steerStrength;
        else pheromoneSteer = heading * parameters.steerStrength;
    }

    float SampleSensor (float angleDeg)
    {
        Vector2 dir = Rotate(heading, angleDeg);
        Vector2 pos = (Vector2)transform.position + dir * parameters.pheromoneSensorDistance;

        var field = mode == AntMode.ToFood ? foodField : homeField;
        return field ? field.SampleStrength(pos, parameters.pheromoneSensorSize, false) : 0f;
    }
    static Vector2 Rotate (Vector2 v, float ang) =>
        Quaternion.Euler(0,0,ang) * v;

    #endregion
    
    #region —  Food

    void HandleFoodSeeking ()
    {
        if (!targetFood) AcquireTargetFood();
        if (!targetFood) { targetSteer = Vector2.zero; return; }

        pheromoneSteer = randomSteer = Vector2.zero;

        Vector2 toFood = (Vector2)targetFood.position - (Vector2)transform.position;
        targetSteer = toFood.normalized * parameters.targetSteerStrength;

        if (toFood.magnitude < parameters.pickupDistance)
            PickupFood(targetFood);
    }

    void AcquireTargetFood ()
    {
        int n = Physics2D.OverlapCircleNonAlloc(sensorOrigin.position, parameters.detectionRadius, foodBuffer, foodLayer);
        for (int i = 0; i < n; i++)
            if (foodBuffer[i] && !foodBuffer[i].GetComponentInParent<AntAgent>())
            {
                targetFood = foodBuffer[i].transform; 
                break;
            }
    }

    void PickupFood (Transform food)
    {
        carriedItem = food;
        food.SetParent(head ? head : transform, true);
        food.localPosition = Vector3.zero;

        food.gameObject.layer = 0;
        foreach (var c in food.GetComponentsInChildren<Collider2D>()) c.enabled = false;
        if (food.TryGetComponent(out Rigidbody2D rb)) Destroy(rb);

        mode = AntMode.ToHome;
        timeSinceLeftFood = Time.time;
        targetFood = null;
        StartTurnAround();
    }

    void HandleReturnHome ()
    {
        Collider2D nest = Physics2D.OverlapCircle(sensorOrigin.position, parameters.detectionRadius, nestLayer);
        if (!nest) { targetSteer = Vector2.zero; return; }

        Vector2 toNest = ((Vector2)nest.transform.position - (Vector2)transform.position).normalized;
        targetSteer = toNest * parameters.targetSteerStrength;

        if (Vector2.Distance(transform.position, nest.transform.position) < parameters.collisionRadius)
            DepositFood();
    }

    void DepositFood ()
    {
        if (carriedItem) Destroy(carriedItem.gameObject);
        carriedItem = null;
        nestRef?.ReportFood();

        mode = AntMode.ToFood;
        timeSinceLeftNest = Time.time;
        StartTurnAround();
    }

    #endregion
    
    #region — Antennas + obstacles

    void HandleCollisionSteering ()
    {
        if (Time.time > obstacleResetTime)
        {
            obstacleAvoidForce = Vector2.zero;
            lastAntennaHit = Antenna.None;
        }

        Vector2 side = new Vector2(-heading.y, heading.x);

        Vector2 leftOrigin = (Vector2)sensorOrigin.position - side * parameters.antennaOffset;
        Vector2 rightOrigin = (Vector2)sensorOrigin.position + side * parameters.antennaOffset;

        RaycastHit2D hitL = Physics2D.Raycast(leftOrigin, heading, parameters.antennaDistance, obstacleMask);
        RaycastHit2D hitR = Physics2D.Raycast(rightOrigin, heading, parameters.antennaDistance, obstacleMask);

        if (hitL || hitR)
        {
            if (hitL && (lastAntennaHit != Antenna.Right) && (!hitR || hitL.distance < hitR.distance))
            { obstacleAvoidForce = side * parameters.collisionAvoidSteerStrength; lastAntennaHit = Antenna.Left; }
            if (hitR && (lastAntennaHit != Antenna.Left ) && (!hitL || hitR.distance < hitL.distance))
            { obstacleAvoidForce = -side * parameters.collisionAvoidSteerStrength; lastAntennaHit = Antenna.Right;}

            obstacleResetTime = Time.time + 0.5f;
            randomSteer = obstacleAvoidForce.normalized * parameters.randomSteerStrength;
        }
    }

    void StartTurnAround ()
    {
        isTurning = true;
        turnEndTime = Time.time + 1.5f;

        Vector2 baseDir = -heading;
        Vector2 side = new Vector2(-baseDir.y, baseDir.x);
        turnSteerForce = baseDir + side * (Random.value - .5f) * .4f;
    }

    #endregion
    
    #region — Play Area

    void EnforcePlayArea ()
    {
        if (!playArea) return;
        var rect = playArea.GetWorldRect();

        if (rect.Contains(transform.position)) return;

        Vector2 clamped = playArea.ClampToArea(transform.position);
        transform.position = clamped;

        heading = ((Vector2)rect.center - clamped).normalized;
        StartTurnAround();
    }

    #endregion
}
