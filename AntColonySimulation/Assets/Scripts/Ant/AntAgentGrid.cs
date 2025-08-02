using UnityEngine;

[DisallowMultipleComponent]
public class AntAgentGrid : MonoBehaviour
{
    [Header("Movement")]
    public float speed        = 5f;
    public float maxTurnDeg   = 360f;

    [Header("Sensing")]
    public float sensorLength = 1.2f;
    public float sensorAngle  = 35f;
    public float sensorRadius = 0.5f;

    [Header("Exploration")] [Range(0, 180)]
    public float randomSteerDeg = 25f;
    [Range(0,1)]   public float randomSteerProb = 0.15f;

    [Header("Pheromones")]
    public float depositStrength = 1f;

    bool carrying = false;
    PheromoneGrid grid;
    GridSettings  cfg;

    void Awake()
    {
        grid = PheromoneGrid.Instance;
        cfg  = grid.settings;
    }

    void Update()
    {
        float bestVal = -1f;
        int   bestIdx = 0;          

        for (int i = -1; i <= 1; ++i)
        {
            float ang = i * sensorAngle;
            Vector2 dir = Quaternion.Euler(0, 0, ang) * transform.up;
            Vector2 pos = (Vector2)transform.position + dir * sensorLength;

            int gx = Mathf.FloorToInt(pos.x / cfg.cellSize) + cfg.width  / 2;
            int gy = Mathf.FloorToInt(pos.y / cfg.cellSize) + cfg.height / 2;

            float v = grid.Sample(gx, gy, carrying);
            if (!carrying) v += grid.FoodAt(gx, gy); 

            if (v > bestVal)
            {
                bestVal = v;
                bestIdx = i;
            }
        }

        float targetTurnDeg;

        if (bestVal <= 1e-4f)                   
        {
            targetTurnDeg = Random.Range(-randomSteerDeg, randomSteerDeg);
        }
        else                                     
        {
            targetTurnDeg = bestIdx * sensorAngle;
            if (Random.value < randomSteerProb)  
                targetTurnDeg += Random.Range(-randomSteerDeg, randomSteerDeg);
        }

        float maxStep = maxTurnDeg * Time.deltaTime;
        float stepDeg = Mathf.Clamp(-targetTurnDeg, -maxStep, maxStep);
        transform.Rotate(0, 0, stepDeg);

        transform.position += transform.up * speed * Time.deltaTime;

        
        int gxSelf = Mathf.FloorToInt(transform.position.x / cfg.cellSize) + cfg.width  / 2;
        int gySelf = Mathf.FloorToInt(transform.position.y / cfg.cellSize) + cfg.height / 2;

        if (!carrying)
        {
            if (grid.TakeFood(gxSelf, gySelf))
                carrying = true;            
        }
        else
        {
            if (transform.position.sqrMagnitude < 2.25f) 
                carrying = false;
        }
        
        for (int dx = -1; dx <= 1; ++dx)
            for (int dy = -1; dy <= 1; ++dy)
                grid.Deposit(gxSelf + dx, gySelf + dy, !carrying);
    }
}
 