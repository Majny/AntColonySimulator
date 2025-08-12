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
    Collider2D targetFoodCol;

    Vector2 heading, velocity;
    Vector2 randomSteer, pheromoneSteer, targetSteer;
    Vector2 obstacleAvoidForce;

    float nextRandomSteerTime;

    Vector2 lastPheromonePos;
    float timeSinceLeftNest, timeSinceLeftFood;
    
    float nextLegRefreshTime;
    const float LegRefreshInterval = 0.15f;


    bool isTurning;
    Vector2 turnSteerForce;
    float turnEndTime;

    float nextSensorSampleTime;

    enum Antenna { None, Left, Right }
    Antenna lastAntennaHit = Antenna.None;
    float obstacleResetTime;

    readonly Collider2D[] foodBuffer = new Collider2D[8];

    int myTeamId = -1;

    Vector2 prevPos;
    int stuckFrames;
    const int StuckFramesThreshold = 14;
    const float MinMoveSqr = 0.00004f;

    #region — Inicializace
    public void Init(NestController nest, PheromoneField home, PheromoneField food)
    {
        nestRef = nest;
        homeField = home;
        foodField = food;

        myTeamId = (nestRef != null) ? nestRef.TeamId : -1;

        if (!sensorOrigin) sensorOrigin = transform;

        transform.rotation = Quaternion.Euler(0, 0, Random.value * 360f);
        heading = transform.right;
        velocity = heading * parameters.maxSpeed;
        mode = AntMode.ToFood;

        lastPheromonePos = transform.position;
        timeSinceLeftNest = Time.time;

        ScheduleNextRandomSteer();
        nextSensorSampleTime = Random.value * parameters.timeBetweenSensorUpdate;

        prevPos = transform.position;
    }
    #endregion

    #region — Unity Loop
    void Update()
    {
        MaybeRefreshLegTimers();

        PlacePheromoneIfNeeded();
        HandleRandomSteering();
        HandlePheromoneSteering();

        if (mode == AntMode.ToFood) HandleFoodSeeking();
        else HandleReturnHome();

        HandleCollisionSteering();
        IntegrateMovement();

        Vector2 p = transform.position;
        if ((p - prevPos).sqrMagnitude < MinMoveSqr)
        {
            stuckFrames++;
            if (stuckFrames > StuckFramesThreshold)
            {
                Vector2 kick = Perp(heading) * parameters.maxSpeed * 0.6f * (Random.value < 0.5f ? 1f : -1f);
                velocity = kick;
                heading = velocity.normalized;
                randomSteer += heading * 0.4f;
                StartTurnAround();
                stuckFrames = 0;
            }
        }
        else stuckFrames = 0;
        prevPos = p;
    }

    void MaybeRefreshLegTimers()
    {
        if (Time.time < nextLegRefreshTime) return;
        nextLegRefreshTime = Time.time + LegRefreshInterval;

        if (!sensorOrigin) sensorOrigin = transform;

        if (mode == AntMode.ToFood)
        {
            if (!nestRef) return;

            Collider2D nestCol = nestRef.GetComponentInChildren<Collider2D>();
            bool atNest = nestCol ? nestCol.OverlapPoint(sensorOrigin.position)
                : Vector2.Distance(sensorOrigin.position, nestRef.transform.position)
                  <= parameters.pickupDistance * 1.25f;

            if (atNest)
            {
                timeSinceLeftNest = Time.time;
                lastPheromonePos = transform.position;
            }
        }
        else if (mode == AntMode.ToHome)
        {
            float r = Mathf.Max(parameters.pickupDistance * 1.0f, 0.20f);

            int n = Physics2D.OverlapCircleNonAlloc(sensorOrigin.position, r, foodBuffer, foodLayer);
            for (int i = 0; i < n; i++)
            {
                var col = foodBuffer[i];
                if (!col) continue;

                if (col.GetComponentInParent<AntAgent>()) continue;

                if (col.TryGetComponent(out FoodItem fi) && fi.taken) continue;

                timeSinceLeftFood = Time.time;
                break;
            }
        }
    }


    void LateUpdate() => EnforcePlayArea();
    #endregion

    #region — Movement
    void IntegrateMovement()
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

        float dt = Time.deltaTime;
        Vector2 move = velocity * dt;

        float r = Mathf.Max(parameters.collisionRadius, 0.05f);
        float eps = 0.006f;

        Collider2D inside = Physics2D.OverlapCircle(transform.position, r * 0.98f, obstacleMask);
        if (inside)
        {
            Vector2 cp = inside.ClosestPoint(transform.position);
            Vector2 n = (Vector2)transform.position - cp;
            if (n.sqrMagnitude < 1e-6f) n = heading;
            n.Normalize();

            transform.position = cp + n * (r + eps);
            SlideAlong(n);
            return;
        }

        RaycastHit2D hit = Physics2D.CircleCast(transform.position, r, heading, move.magnitude + eps, obstacleMask);
        if (hit.collider)
        {
            Vector2 n = hit.normal;
            Vector2 posAtContact = hit.point + n * (r + eps);
            transform.position = posAtContact;

            SlideAlong(n);
            return;
        }

        float probe = Mathf.Max(parameters.collisionRadius, Mathf.Max(parameters.antennaDistance, velocity.magnitude * dt));
        if (Physics2D.Raycast(transform.position, heading, probe, obstacleMask))
            StartTurnAround();

        transform.position += (Vector3)move;
        heading = velocity.sqrMagnitude > 1e-6f ? velocity.normalized : heading;
        transform.right = heading;
    }

    void SlideAlong(Vector2 n)
    {
        Vector2 t = TangentFromNormal(n, velocity);
        float speed = Mathf.Max(parameters.maxSpeed * 0.55f, velocity.magnitude * 0.65f);
        Vector2 vt = t.normalized * speed;

        velocity = vt;
        heading = vt.normalized;
        transform.right = heading;

        randomSteer += Perp(n) * 0.06f;
        obstacleResetTime = Time.time + 0.25f;
    }

    static Vector2 TangentFromNormal(Vector2 n, Vector2 prefer)
    {
        Vector2 t = new(-n.y, n.x);
        if (Vector2.Dot(t, prefer) < 0) t = -t;
        return t;
    }

    static Vector2 Perp(Vector2 v) => new(-v.y, v.x);
    static Vector2 Reflect(Vector2 v, Vector2 n) => v - 2f * Vector2.Dot(v, n) * n;
    #endregion

    #region — Random Steering
    void HandleRandomSteering()
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

    static Vector2 BestRandomDir(Vector2 refDir, int tries)
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
    void PlacePheromoneIfNeeded()
    {
        if (Vector2.Distance(transform.position, lastPheromonePos) <= parameters.pheromoneSpacing)
            return;

        float legTime = (mode == AntMode.ToHome) ? Time.time - timeSinceLeftFood : Time.time - timeSinceLeftNest;

        if (parameters.pheromoneRunOutTime > 0 && legTime > parameters.pheromoneRunOutTime)
            return;

        float t = (parameters.pheromoneRunOutTime <= 0f) ? 1f : 1f - (legTime / parameters.pheromoneRunOutTime);
        float strength = Mathf.Lerp(0.5f, 1f, t);

        if (mode == AntMode.ToFood) homeField?.Add(transform.position, strength);
        else foodField?.Add(transform.position, strength);

        lastPheromonePos = (Vector2)transform.position + Random.insideUnitCircle * parameters.pheromoneSpacing * 0.2f;
    }
    #endregion

    #region — Pheromone Steering
    void HandlePheromoneSteering()
    {
        if (Time.time < nextSensorSampleTime) return;
        nextSensorSampleTime = Time.time + parameters.timeBetweenSensorUpdate;

        float a = parameters.pheromoneSensorAngle;
        float L = SampleSensor(-a);
        float C = SampleSensor(0);
        float R = SampleSensor(a);

        if (L > C && L > R) pheromoneSteer = Rotate(heading, -a) * parameters.steerStrength;
        else if (R > C) pheromoneSteer = Rotate(heading, a) * parameters.steerStrength;
        else pheromoneSteer = heading * parameters.steerStrength;
    }

    float SampleSensor(float angleDeg)
    {
        Vector2 dir = Rotate(heading, angleDeg);
        Vector2 from = sensorOrigin ? (Vector2)sensorOrigin.position : (Vector2)transform.position;
        Vector2 pos = from + dir * parameters.pheromoneSensorDistance;

        if (Physics2D.Linecast(from, pos, obstacleMask))
            return 0f;

        var field = (mode == AntMode.ToFood) ? foodField : homeField;
        return field ? field.SampleStrength(pos, parameters.pheromoneSensorSize, false) : 0f;
    }

    static Vector2 Rotate(Vector2 v, float ang) =>
        (Vector2)(Quaternion.Euler(0, 0, ang) * (Vector3)v);
    #endregion

    #region —  Food
    void HandleFoodSeeking()
    {
        if (!targetFood) AcquireTargetFood();
        if (!targetFood) { targetSteer = Vector2.zero; return; }

        randomSteer = Vector2.zero;
        pheromoneSteer *= 0.2f;

        Vector2 toFood = (Vector2)targetFood.position - (Vector2)transform.position;
        targetSteer = toFood.sqrMagnitude > 1e-6f
            ? toFood.normalized * (parameters.targetSteerStrength * 1.15f)
            : Vector2.zero;

        Vector2 probeFrom = sensorOrigin ? (Vector2)sensorOrigin.position : (Vector2)transform.position;
        Vector2 closest = targetFoodCol ? targetFoodCol.ClosestPoint(probeFrom) : (Vector2)targetFood.position;
        float reach = parameters.pickupDistance + GetTargetRadius(targetFoodCol);

        if (Vector2.Distance(probeFrom, closest) <= reach)
            PickupFoodTarget();
    }

    void AcquireTargetFood()
    {
        int n = Physics2D.OverlapCircleNonAlloc(
            sensorOrigin ? (Vector2)sensorOrigin.position : (Vector2)transform.position,
            parameters.detectionRadius, foodBuffer, foodLayer);

        Collider2D best = null;
        float bestD2 = float.PositiveInfinity;

        for (int i = 0; i < n; i++)
        {
            var col = foodBuffer[i];
            if (!col) continue;
            if (col.GetComponentInParent<AntAgent>()) continue;
            if (col.TryGetComponent(out FoodItem fi) && fi.taken) continue;

            float d2 = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d2 < bestD2) { bestD2 = d2; best = col; }
        }

        targetFoodCol = best;
        targetFood = best ? best.transform : null;
    }

    float GetTargetRadius(Collider2D col)
    {
        if (!col) return 0.05f;
        if (col is CircleCollider2D cc)
            return Mathf.Abs(cc.radius) * Mathf.Max(col.transform.lossyScale.x, col.transform.lossyScale.y);
        var e = col.bounds.extents;
        return Mathf.Max(0.03f, Mathf.Max(e.x, e.y));
    }

    void PickupFoodTarget()
    {
        Transform root = targetFoodCol
            ? (targetFoodCol.GetComponentInParent<FoodItem>()?.transform ?? targetFoodCol.transform)
            : targetFood;

        if (root && root.TryGetComponent(out FoodItem item))
        {
            if (!item.TryTake())
            {
                targetFood = null;
                targetFoodCol = null;
                return;
            }
        }

        carriedItem = root;
        root.SetParent(head ? head : transform, true);
        root.localPosition = Vector3.zero;

        root.gameObject.layer = 0;
        foreach (var c in root.GetComponentsInChildren<Collider2D>()) c.enabled = false;
        if (root.TryGetComponent<Rigidbody2D>(out var rb)) Destroy(rb);

        mode = AntMode.ToHome;
        timeSinceLeftFood = Time.time;
        targetFood = null;
        targetFoodCol = null;
        StartTurnAround();
    }

    void HandleReturnHome()
    {
        if (!nestRef) { targetSteer = Vector2.zero; return; }

        Collider2D nestCol = nestRef.GetComponentInChildren<Collider2D>();
        bool atNest = nestCol ? nestCol.OverlapPoint(sensorOrigin.position)
                              : Vector2.Distance(sensorOrigin.position, nestRef.transform.position)
                                <= parameters.pickupDistance * 1.25f;
        if (atNest) { DepositFood(); return; }

        targetSteer = Vector2.zero;

        float r = parameters.detectionRadius;
        var nestInRange = Physics2D.OverlapCircle(sensorOrigin.position, r, nestLayer);
        if (nestInRange &&
            !Physics2D.Linecast(sensorOrigin.position, (Vector2)nestRef.transform.position, obstacleMask))
        {
            Vector2 toNest = (Vector2)nestRef.transform.position - (Vector2)sensorOrigin.position;
            if (toNest.sqrMagnitude > 1e-6f)
                targetSteer = toNest.normalized * parameters.targetSteerStrength;
        }
    }

    void DepositFood()
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
    void HandleCollisionSteering()
    {
        if (Time.time > obstacleResetTime)
        {
            obstacleAvoidForce = Vector2.zero;
            lastAntennaHit = Antenna.None;
        }

        Vector2 side = new(-heading.y, heading.x);

        Vector2 leftOrigin = (Vector2)sensorOrigin.position - side * parameters.antennaOffset;
        Vector2 rightOrigin = (Vector2)sensorOrigin.position + side * parameters.antennaOffset;

        RaycastHit2D hitL = Physics2D.Raycast(leftOrigin, heading, parameters.antennaDistance, obstacleMask);
        RaycastHit2D hitR = Physics2D.Raycast(rightOrigin, heading, parameters.antennaDistance, obstacleMask);

        if (hitL || hitR)
        {
            if (hitL && (lastAntennaHit != Antenna.Right) && (!hitR || hitL.distance < hitR.distance))
            { obstacleAvoidForce = side * parameters.collisionAvoidSteerStrength; lastAntennaHit = Antenna.Left; }
            if (hitR && (lastAntennaHit != Antenna.Left) && (!hitL || hitR.distance < hitL.distance))
            { obstacleAvoidForce = -side * parameters.collisionAvoidSteerStrength; lastAntennaHit = Antenna.Right; }

            obstacleResetTime = Time.time + 0.35f;
            randomSteer = obstacleAvoidForce.normalized * parameters.randomSteerStrength;
        }
    }

    void StartTurnAround()
    {
        isTurning = true;
        turnEndTime = Time.time + 1.1f;

        Vector2 baseDir = -heading;
        Vector2 side = new(-baseDir.y, baseDir.x);
        turnSteerForce = baseDir + side * (Random.value - .5f) * .4f;
    }
    #endregion

    #region — Play Area
    void EnforcePlayArea()
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

    void OnDestroy()
    {
        if (myTeamId >= 0)
            TeamManager.Instance?.UnregisterAnt(myTeamId);
    }
}
