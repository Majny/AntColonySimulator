using UnityEngine;

public class AntAgent : MonoBehaviour
{
    [Header("Config")]
    public AgentParameters parameters;
    public Transform sensorOrigin;
    public Transform head;
    public LayerMask foodLayer;
    public LayerMask nestLayer;

    [Header("Refs (set at runtime)")]
    private NestController nestReference;
    private PheromoneField homeField;
    private PheromoneField foodField;

    private AntMode mode;
    private Transform carriedItem;
    private Transform targetFood;

    private Vector2 velocity;
    private Vector2 heading;

    private Vector2 pheromoneSteer;
    private Vector2 randomSteer;
    private Vector2 targetSteer;
    private Vector2 turnSteerForce;
    private bool isTurning;
    private float turnEndTimestamp;

    private float nextWiggle;
    private float nextRandomSteerTime;
    private float nextSensorUpdateTime;
    private Vector2 lastPheromonePos;

    private float timeSinceLeftNest;
    private float timeSinceLeftFood;

    Vector2 obstacleAvoidForce;
    float obstacleForceResetTime;
    enum Antenna { None, Left, Right }
    Antenna lastAntennaCollision = Antenna.None;

    [Header("Obstacles")]
    public LayerMask obstacleMask;
    public float obstacleProbe = 0.4f;

    private PheromoneField playArea;
    public void SetPlayArea(PheromoneField area) => playArea = area;

    private readonly Collider2D[] foodBuffer = new Collider2D[8];

    public void Init(NestController nest, PheromoneField home, PheromoneField food)
    {
        nestReference = nest;
        homeField = home;
        foodField = food;

        if (sensorOrigin == null) sensorOrigin = transform;

        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        heading = transform.up;
        velocity = heading * parameters.maxSpeed;
        lastPheromonePos = transform.position;
        mode = AntMode.ToFood;

        timeSinceLeftNest = Time.time;
        nextWiggle = Time.time + Random.Range(0.3f, 1.2f);
        nextSensorUpdateTime = Time.time + Random.Range(0f, parameters.timeBetweenSensorUpdate);
        nextRandomSteerTime = Time.time + Random.Range(parameters.randomSteerMaxDuration / 3f, parameters.randomSteerMaxDuration);
    }

    void Update()
    {
        TryDropPheromone();
        ApplyWiggle();
        UpdatePheromoneSteering();
        UpdateRandomSteer();

        if (mode == AntMode.ToFood) HandleFoodSeeking();
        else HandleReturnHome();

        HandleCollisionSteering();

        PerformMovement();
    }

    void LateUpdate() => EnforcePlayArea();

    void PerformMovement()
    {
        Vector2 steer = pheromoneSteer + randomSteer + targetSteer + obstacleAvoidForce;

        if (isTurning)
        {
            steer += turnSteerForce * parameters.steerStrength;
            if (Time.time > turnEndTimestamp) isTurning = false;
        }

        steer = (steer.sqrMagnitude > 1e-6f) ? steer.normalized : heading;

        Vector2 desiredVel = steer * parameters.maxSpeed;
        velocity = Vector2.Lerp(velocity, desiredVel, parameters.acceleration * Time.deltaTime);

        Vector2 deltaMove = velocity * Time.deltaTime;

        Vector2 preHitDir = heading;
        float probeDist = Mathf.Max(obstacleProbe, deltaMove.magnitude, parameters.collisionRadius);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, preHitDir, probeDist, obstacleMask);
        if (hit)
        {
            Vector2 reflect = Vector2.Reflect(preHitDir, hit.normal).normalized;
            heading = reflect;

            transform.position = hit.point - preHitDir * parameters.collisionRadius;

            deltaMove = heading * (parameters.maxSpeed * Time.deltaTime * 0.5f);
            velocity = heading * parameters.maxSpeed;

            if (!isTurning) StartTurn();
        }

        transform.position += (Vector3)deltaMove;

        heading = (velocity.sqrMagnitude > 1e-6f) ? velocity.normalized : heading;
        transform.up = heading;
    }

    void ApplyWiggle()
    {
        if (Time.time > nextWiggle)
        {
            float deltaAngle = Random.Range(-parameters.randomTurnAngle, parameters.randomTurnAngle);
            heading = Quaternion.Euler(0f, 0f, deltaAngle) * heading;
            nextWiggle = Time.time + Random.Range(0.3f, parameters.randomWiggleInterval);
        }
    }

    void StartTurn()
    {
        isTurning = true;
        turnEndTimestamp = Time.time + 1.5f;

        Vector2 baseDir = -heading;
        Vector2 side = new Vector2(-baseDir.y, baseDir.x);
        turnSteerForce = baseDir + side * (Random.value - 0.5f) * 0.4f;
    }

    void TryDropPheromone()
    {
        if (Vector2.Distance(transform.position, lastPheromonePos) < parameters.pheromoneSpacing)
            return;

        float timeSinceSwitch = (mode == AntMode.ToHome)
            ? Time.time - timeSinceLeftFood
            : Time.time - timeSinceLeftNest;

        bool keepDropping = parameters.pheromoneRunOutTime <= 0f || timeSinceSwitch < parameters.pheromoneRunOutTime;
        if (!keepDropping) return;

        float denom = (parameters.pheromoneRunOutTime > 0f) ? parameters.pheromoneRunOutTime : 1f;
        float t = Mathf.Clamp01(timeSinceSwitch / denom);
        float strength = Mathf.Lerp(1f, 0.5f, t);

        if (mode == AntMode.ToFood) homeField?.Add((Vector2)transform.position, strength);
        else foodField?.Add((Vector2)transform.position, strength);

        lastPheromonePos = (Vector2)transform.position + Random.insideUnitCircle * (parameters.pheromoneSpacing * 0.2f);
    }

    void UpdatePheromoneSteering()
    {
        if (Time.time < nextSensorUpdateTime) return;
        nextSensorUpdateTime = Time.time + parameters.timeBetweenSensorUpdate;

        float a = parameters.pheromoneSensorAngle;
        float l = SenseAtAngle(-a);
        float c = SenseAtAngle(0f);
        float r = SenseAtAngle(a);

        if (l > c && l > r) pheromoneSteer = (Quaternion.Euler(0, 0, -a) * heading) * parameters.steerStrength;
        else if (r > c) pheromoneSteer = (Quaternion.Euler(0, 0,  a) * heading) * parameters.steerStrength;
        else pheromoneSteer = heading * parameters.steerStrength;
    }

    float SenseAtAngle(float angle)
    {
        Vector2 dir = Quaternion.Euler(0, 0, angle) * heading;
        Vector2 pos = (Vector2)transform.position + dir * parameters.pheromoneSensorDistance;
        var fieldToRead = (mode == AntMode.ToFood) ? foodField : homeField;
        return (fieldToRead != null) ? fieldToRead.SampleStrength(pos, parameters.pheromoneSensorRadius, false) : 0f;
    }

    void UpdateRandomSteer()
    {
        if (Time.time > nextRandomSteerTime)
        {
            nextRandomSteerTime = Time.time + Random.Range(parameters.randomSteerMaxDuration / 3f, parameters.randomSteerMaxDuration);
            randomSteer = GetRandomDir(heading) * parameters.randomSteerStrength;
        }
    }

    Vector2 GetRandomDir(Vector2 referenceDir, int iterations = 4)
    {
        Vector2 best = Vector2.zero;
        float bestDot = -1f;
        for (int i = 0; i < iterations; i++)
        {
            Vector2 rnd = Random.insideUnitCircle.normalized;
            float d = Vector2.Dot(referenceDir, rnd);
            if (d > bestDot) { bestDot = d; best = rnd; }
        }
        return best;
    }

    void HandleFoodSeeking()
    {
        if (targetFood == null) AcquireTargetFood();

        if (targetFood != null)
        {
            pheromoneSteer = Vector2.zero;
            randomSteer = Vector2.zero;

            Vector2 toFood = (Vector2)targetFood.position - (Vector2)transform.position;
            float dst = toFood.magnitude;
            Vector2 dir = (dst > 1e-4f) ? (toFood / dst) : heading;
            targetSteer = dir * parameters.targetSteerStrength;

            float pickupDst = Mathf.Max(parameters.pickupDistance, (targetFood.lossyScale.x + targetFood.lossyScale.y) * 0.25f);
            if (dst < pickupDst)
            {
                var pile = targetFood.GetComponent<FoodPile>();
                if (pile != null)
                {
                    Transform unit = pile.TakeOne(head != null ? head : transform);
                    if (unit != null)
                    {
                        PickupCarriedUnit(unit);
                    }
                    else
                    {
                        targetFood = null;
                    }
                }
                else
                {
                    PickupFood(targetFood);
                }
            }
        }
        else
        {
            targetSteer = Vector2.zero;
        }
    }

    void PickupCarriedUnit(Transform unit)
    {
        carriedItem = unit;
        mode = AntMode.ToHome;
        timeSinceLeftFood = Time.time;
        targetFood = null;
        StartTurn();
    }

    void HandleReturnHome()
    {
        Collider2D nest = Physics2D.OverlapCircle(sensorOrigin.position, parameters.detectionRadius, nestLayer);
        if (nest != null)
        {
            Vector2 toNest = (Vector2)nest.transform.position - (Vector2)transform.position;
            targetSteer = (toNest.sqrMagnitude > 1e-6f) ? toNest.normalized * parameters.targetSteerStrength : Vector2.zero;
            TryDepositAtNest(nest);
        }
        else targetSteer = Vector2.zero;
    }

    void AcquireTargetFood()
    {
        int count = Physics2D.OverlapCircleNonAlloc(sensorOrigin.position, parameters.detectionRadius, foodBuffer, foodLayer);
        if (count > 0) targetFood = foodBuffer[Random.Range(0, count)].transform;
    }

    void PickupFood(Transform food)
    {
        carriedItem = food;
        Transform parent = (head != null) ? head : transform;
        carriedItem.SetParent(parent, worldPositionStays: true);
        carriedItem.position = parent.position;

        mode = AntMode.ToHome;
        timeSinceLeftFood = Time.time;
        targetFood = null;
        StartTurn();
    }

    void TryDepositAtNest(Collider2D nest = null)
    {
        if (nest == null)
            nest = Physics2D.OverlapCircle(sensorOrigin.position, parameters.detectionRadius, nestLayer);

        if (nest != null)
        {
            if (carriedItem != null) Destroy(carriedItem.gameObject);
            nestReference.ReportFood();
            carriedItem = null;

            mode = AntMode.ToFood;
            timeSinceLeftNest = Time.time;
            StartTurn();
        }
    }

    void HandleCollisionSteering()
    {
        if (Time.time > obstacleForceResetTime)
        {
            obstacleAvoidForce = Vector2.zero;
            lastAntennaCollision = Antenna.None;
        }

        Vector2 side = new Vector2(-heading.y, heading.x);

        Vector2 leftOrigin  = (Vector2)sensorOrigin.position - side * parameters.antennaOffset;
        Vector2 rightOrigin = (Vector2)sensorOrigin.position + side * parameters.antennaOffset;

        RaycastHit2D hitL = Physics2D.Raycast(leftOrigin, heading, parameters.antennaDistance, obstacleMask);
        RaycastHit2D hitR = Physics2D.Raycast(rightOrigin, heading, parameters.antennaDistance, obstacleMask);

        if (hitL || hitR)
        {
            if (hitL && lastAntennaCollision != Antenna.Right && (!hitR || hitL.distance < hitR.distance))
            {
                obstacleAvoidForce = side * parameters.collisionAvoidSteerStrength;
                lastAntennaCollision = Antenna.Left;
            }
            if (hitR && lastAntennaCollision != Antenna.Left && (!hitL || hitR.distance < hitL.distance))
            {
                obstacleAvoidForce = -side * parameters.collisionAvoidSteerStrength;
                lastAntennaCollision = Antenna.Right;
            }

            obstacleForceResetTime = Time.time + 0.5f;
            randomSteer = obstacleAvoidForce.normalized * parameters.randomSteerStrength;
        }
    }

    void EnforcePlayArea()
    {
        if (playArea == null) return;

        var rect = playArea.GetWorldRect();
        Vector2 pos = transform.position;

        if (!rect.Contains(pos))
        {
            Vector2 clamped = playArea.ClampToArea(pos);
            transform.position = clamped;

            Vector2 toCenter = ((Vector2)rect.center - clamped);
            if (toCenter.sqrMagnitude > 1e-6f)
            {
                heading = toCenter.normalized;
                StartTurn();
            }
        }
    }
}
