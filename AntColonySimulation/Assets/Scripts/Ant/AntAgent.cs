using UnityEngine;

public class AntAgent : MonoBehaviour
{
    public AgentParameters parameters;
    public Transform sensorOrigin;
    public LayerMask foodLayer;
    public LayerMask nestLayer;
    public LayerMask obstacleLayer;

    private AntMode state;
    private Vector2 velocity;
    private Vector2 direction;

    private NestController homeNest;
    private Transform carriedFood;

    private float deathTimer;
    private Vector2 lastMarkerDrop;

    private float nextWiggleTime;
    private float nextTurnAngle;

    public void Initialize(NestController nest)
    {
        homeNest = nest;
        transform.eulerAngles = Vector3.forward * Random.Range(0, 360f);
        direction = transform.up;
        velocity = direction * parameters.maxSpeed;
        deathTimer = Time.time + parameters.lifespan + Random.Range(0, parameters.lifespan * 0.5f);
        lastMarkerDrop = transform.position;
        state = AntMode.Foraging;
        nextWiggleTime = Time.time + Random.Range(0.3f, 1.2f);
        nextTurnAngle = 0;
    }

    private void Update()
    {
        if (parameters.enableDeath && Time.time > deathTimer)
        {
            Destroy(gameObject);
            return;
        }

        if (state == AntMode.Foraging)
            SearchForFood();
        else
            ReturnToNest();

        MoveAgent();
    }

    private void MoveAgent()
    {
        HandleRandomSteering();
        AvoidObstacles();

        direction = direction.normalized;
        Vector2 desired = direction * parameters.maxSpeed;
        velocity = Vector2.Lerp(velocity, desired, parameters.acceleration * Time.deltaTime);
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.up = velocity.normalized;
    }

    private void HandleRandomSteering()
    {
        if (Time.time > nextWiggleTime)
        {
            float angle = Random.Range(-parameters.randomTurnAngle, parameters.randomTurnAngle);
            direction = Quaternion.Euler(0, 0, angle) * direction;
            nextWiggleTime = Time.time + Random.Range(0.3f, parameters.randomWiggleInterval);
        }
    }

    
    // TODO: dodelat prekazky pozdejc
    private void AvoidObstacles()
    {
        RaycastHit2D hit = Physics2D.Raycast(sensorOrigin.position, direction, parameters.avoidanceDistance, obstacleLayer);
        if (hit.collider != null)
        {
            Vector2 normal = hit.normal;
            direction = Vector2.Reflect(direction, normal).normalized;
        }
    }

    private void SearchForFood()
    {
        Collider2D food = Physics2D.OverlapCircle(sensorOrigin.position, parameters.detectionRadius, foodLayer);
        if (food != null)
        {
            carriedFood = food.transform;
            carriedFood.SetParent(transform);
            carriedFood.localPosition = Vector3.zero;
            state = AntMode.Returning;
        }
    }

    private void ReturnToNest()
    {
        Collider2D nest = Physics2D.OverlapCircle(sensorOrigin.position, parameters.detectionRadius, nestLayer);
        if (nest != null)
        {
            if (carriedFood)
                Destroy(carriedFood.gameObject);

            homeNest.ReportFood();
            carriedFood = null;
            state = AntMode.Foraging;
        }
    }
}
