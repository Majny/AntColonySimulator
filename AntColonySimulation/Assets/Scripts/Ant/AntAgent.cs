using UnityEngine;

public class AntAgent : MonoBehaviour
{
    public float speed = 1.5f;
    private float markerDetectionRange = 5.0f;
    public float directionChangeInterval = 1.0f;
    public float angleRange = 90f;
    public AntMode mode = AntMode.ToFood;
    
    private Vector2 _direction;
    private float timeSinceLastDirectionChange = 0f;


    void Start()
    {
        PickRandomDirection();
    }


    void Update()
    {
        timeSinceLastDirectionChange += Time.deltaTime;
        if (timeSinceLastDirectionChange > directionChangeInterval)
        {
            ScanAndChooseDirection();
            timeSinceLastDirectionChange = 0f;
        }

        Move();
    }

    void Move()
    {
        transform.Translate(_direction * speed * Time.deltaTime);
    }


    void ScanAndChooseDirection()
    {
        Vector2 bestDirection = _direction;
        float bestScore = 0f;

        for (int i = 0; i < 32; i++)
        {
            float angleOffset = Random.Range(-angleRange / 2f, angleRange / 2f);
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg + angleOffset;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector2 samplePos = (Vector2)transform.position + dir * Random.Range(0.5f, markerDetectionRange);

            Collider2D hit = Physics2D.OverlapPoint(samplePos);
            if (hit != null)
            {
                if (mode == AntMode.ToFood && hit.CompareTag("Food"))
                {
                    _direction = dir.normalized;
                    return;
                }
                else if (mode == AntMode.ToHome && hit.CompareTag("Home"))
                {
                    _direction = dir.normalized;
                    return;
                }
            }

        }

        PickRandomDirection();
    }
    
    void PickRandomDirection()
    {
        float angle = Random.Range(0f, 360f);
        _direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
    }
}

