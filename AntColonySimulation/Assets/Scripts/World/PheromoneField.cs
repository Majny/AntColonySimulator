using System.Collections.Generic;
using UnityEngine;

public class PheromoneField : MonoBehaviour
{
    public Vector2 area;

    public AgentParameters agentParams;

    [Header("Visuals)")]
    public ParticleSystem particleDisplay;
    public Color pheremoneColor = Color.white;
    public float pheremoneSize = 0.05f;
    public float initialAlpha = 1f;

    private ParticleSystem.EmitParams emitParams;

    private float sqrPerceptionRadius;
    private int numCellsX, numCellsY;
    private Vector2 halfSize;
    private float cellSizeReciprocal;
    private Cell[,] cells;

    float EvapTime => (agentParams != null ? agentParams.pheromoneEvaporateTime : 10f);

    void Awake() => Init();

    void Init()
    {
        float perceptionRadius = Mathf.Max(0.01f,
            (agentParams != null && agentParams.pheromoneSensorSize > 0f) ? agentParams.pheromoneSensorSize : 0.75f);

        sqrPerceptionRadius = perceptionRadius * perceptionRadius;

        numCellsX = Mathf.CeilToInt(area.x / perceptionRadius);
        numCellsY = Mathf.CeilToInt(area.y / perceptionRadius);
        halfSize = new Vector2(numCellsX * perceptionRadius, numCellsY * perceptionRadius) * 0.5f;
        cellSizeReciprocal = 1f / perceptionRadius;

        cells = new Cell[numCellsX, numCellsY];
        for (int y = 0; y < numCellsY; y++)
        {
            for (int x = 0; x < numCellsX; x++)
            {
                cells[x, y] = new Cell();
            }
        }
        SetupParticleSystemVisuals();
    }

    void SetupParticleSystemVisuals()
    {
        if (particleDisplay == null) return;

        float life = (agentParams != null ? agentParams.pheromoneEvaporateTime : 10f);

        emitParams.startLifetime = life;
        emitParams.startSize = pheremoneSize;

        var m = particleDisplay.main;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.maxParticles = 100 * 1000;

        var c = particleDisplay.colorOverLifetime;
        c.enabled = true;

        Gradient grad = new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(initialAlpha, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        };
        c.color = grad;
    }


    public void Add(Vector2 point, float initialWeight)
    {
        Vector2Int cellCoord = CellCoordFromPos(point);
        Cell cell = cells[cellCoord.x, cellCoord.y];

        Entry entry = new Entry
        {
            position = point, 
            creationTime = Time.time, 
            initialWeight = initialWeight
        };
        cell.Add(entry);

        if (particleDisplay != null)
        {
            emitParams.startColor = new Color(pheremoneColor.r, pheremoneColor.g, pheremoneColor.b, initialWeight);
            emitParams.position = point;
            particleDisplay.Emit(emitParams, 1);
        }
    }
    
    
    public float SampleStrength(Vector2 worldPos, float radius, bool toHome)
    {
        float total = 0f;
        float now = Time.time;
        float evap = EvapTime;
        bool infinite = evap <= 0f || float.IsInfinity(evap);

        float r2 = radius * radius;
        Vector2Int center = CellCoordFromPos(worldPos);
        int cellRadius = Mathf.CeilToInt(radius * cellSizeReciprocal);

        for (int y = -cellRadius; y <= cellRadius; y++)
        {
            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                int cx = center.x + x, cy = center.y + y;
                if (cx < 0 || cx >= numCellsX || cy < 0 || cy >= numCellsY) continue;

                Cell cell = cells[cx, cy];
                var node = cell.entries.First;
                while (node != null)
                {
                    var cur = node.Value; var next = node.Next;

                    float weight = cur.initialWeight;
                    if (!infinite)
                    {
                        float age = now - cur.creationTime;
                        if (age > evap) { cell.entries.Remove(node); node = next; continue; }
                        weight *= 1f - (age / evap);
                    }

                    if ((cur.position - worldPos).sqrMagnitude < r2)
                        total += weight;

                    node = next;
                }
            }
        }
        return total;
    }

    Vector2Int CellCoordFromPos(Vector2 cell)
    {
        int x = (int)((cell.x + halfSize.x) * cellSizeReciprocal);
        int y = (int)((cell.y + halfSize.y) * cellSizeReciprocal);
        return new Vector2Int(Mathf.Clamp(x, 0, numCellsX - 1), Mathf.Clamp(y, 0, numCellsY - 1));
    }

    public class Cell
    {
        public LinkedList<Entry> entries = new(); 
        public void Add(Entry e) => entries.AddLast(e);
    }

    public struct Entry
    {
        public Vector2 position; 
        public float initialWeight; 
        public float creationTime;
    }

    public Rect GetWorldRect()
    {
        Vector2 size = area;
        Vector2 center = transform.position;
        return new Rect(center - size * 0.5f, size);
    }

    public Vector2 ClampToArea(Vector2 p)
    {
        var r = GetWorldRect();
        return new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax));
    }
    
    
    
    
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.25f);
        Gizmos.DrawWireCube(transform.position, new Vector3(area.x, area.y, 0f));
    }
#endif
}