using System.Collections.Generic;
using UnityEngine;

public class PheromoneField : MonoBehaviour
{
    public Vector2 area = new Vector2(50, 50);

    public AgentParameters agentParams;

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

    void Awake()
    {
        EnsureParticleSystem(); 
        Init();
    }

    void Update()
    {
        if (particleDisplay != null)
            emitParams.startLifetime = (agentParams != null ? agentParams.pheromoneDecayTime : 10f);
    }

    void Init()
    {
        float perceptionRadius = Mathf.Max(0.01f, (agentParams != null ? agentParams.pheromoneSensorRadius : 0.25f));
        sqrPerceptionRadius = perceptionRadius * perceptionRadius;

        numCellsX = Mathf.CeilToInt(area.x / perceptionRadius);
        numCellsY = Mathf.CeilToInt(area.y / perceptionRadius);
        halfSize = new Vector2(numCellsX * perceptionRadius, numCellsY * perceptionRadius) * 0.5f;
        cellSizeReciprocal = 1f / perceptionRadius;

        cells = new Cell[numCellsX, numCellsY];
        for (int y = 0; y < numCellsY; y++)
            for (int x = 0; x < numCellsX; x++)
                cells[x, y] = new Cell();

        if (particleDisplay != null)
        {
            emitParams.startLifetime = (agentParams != null ? agentParams.pheromoneDecayTime : 10f);
            emitParams.startSize = pheremoneSize;

            var main = particleDisplay.main;
            main.maxParticles = 100 * 1000;

            var col = particleDisplay.colorOverLifetime;
            col.enabled = true;

            Gradient grad = new Gradient();
            grad.colorKeys = new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            };
            grad.alphaKeys = new GradientAlphaKey[] {
                new GradientAlphaKey(initialAlpha, 0f),
                new GradientAlphaKey(0f, 1f)
            };
            col.color = grad;
        }
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
    
    public int GetAllInCircle(Entry[] result, Vector2 centre)
    {
        Vector2Int cellCoord = CellCoordFromPos(centre);
        int i = 0;
        float now = Time.time;
        float evap = (agentParams != null ? agentParams.pheromoneDecayTime : 10f);

        for (int oy = -1; oy <= 1; oy++)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                int cx = cellCoord.x + ox;
                int cy = cellCoord.y + oy;
                if (cx < 0 || cx >= numCellsX || cy < 0 || cy >= numCellsY) continue;

                Cell cell = cells[cx, cy];
                var node = cell.entries.First;
                while (node != null)
                {
                    var current = node.Value;
                    var next = node.Next;

                    float age = now - current.creationTime;
                    if (age > evap)
                    {
                        cell.entries.Remove(node);
                    }
                    else if ((current.position - centre).sqrMagnitude < sqrPerceptionRadius)
                    {
                        if (i >= result.Length) return result.Length;
                        result[i++] = current;
                    }

                    node = next;
                }
            }
        }
        return i;
    }
    public void AddPheromone(Vector2 worldPos, float strength, bool toHome)
    {
        Add(worldPos, strength);
    }
    
    public float SampleStrength(Vector2 worldPos, float radius, bool toHome)
    {
        float total = 0f;
        float now = Time.time;
        float evap = (agentParams != null ? agentParams.pheromoneDecayTime : 10f);

        float radiusSqr = radius * radius;
        Vector2Int center = CellCoordFromPos(worldPos);

        int cellRadius = Mathf.CeilToInt(radius * cellSizeReciprocal);
        for (int y = -cellRadius; y <= cellRadius; y++)
        {
            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                int cx = center.x + x;
                int cy = center.y + y;
                if (cx < 0 || cx >= numCellsX || cy < 0 || cy >= numCellsY) continue;

                Cell cell = cells[cx, cy];
                var node = cell.entries.First;
                while (node != null)
                {
                    var current = node.Value;
                    var next = node.Next;

                    float age = now - current.creationTime;
                    if (age > evap)
                    {
                        cell.entries.Remove(node);
                    }
                    else
                    {
                        float distSqr = (current.position - worldPos).sqrMagnitude;
                        if (distSqr < radiusSqr)
                        {
                            float decay = 1f - (age / evap);
                            total += current.initialWeight * decay;
                        }
                    }
                    node = next;
                }
            }
        }
        return total;
    }
    
    Vector2Int CellCoordFromPos(Vector2 point)
    {
        int x = (int)((point.x + halfSize.x) * cellSizeReciprocal);
        int y = (int)((point.y + halfSize.y) * cellSizeReciprocal);
        return new Vector2Int(Mathf.Clamp(x, 0, numCellsX - 1), Mathf.Clamp(y, 0, numCellsY - 1));
    }

    public class Cell
    {
        public LinkedList<Entry> entries = new LinkedList<Entry>();
        public void Add(Entry e) => entries.AddLast(e);
    }

    public struct Entry
    {
        public Vector2 position;
        public float initialWeight;
        public float creationTime;
    }
    
    void EnsureParticleSystem() {
        if (particleDisplay != null) return;

        var go = new GameObject("PheromoneParticles");
        go.transform.SetParent(transform, false);
        particleDisplay = go.AddComponent<ParticleSystem>();

        var main = particleDisplay.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0;
        main.startLifetime = agentParams ? agentParams.pheromoneDecayTime : 8f;
        main.startSize = pheremoneSize;
        main.maxParticles = 100000;

        var emission = particleDisplay.emission; emission.rateOverTime = 0;
        var shape = particleDisplay.shape; shape.enabled = false;
        var col = particleDisplay.colorOverLifetime; col.enabled = true;

        var r = particleDisplay.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material = new Material(Shader.Find("Sprites/Default"));
        r.sortingOrder = 20;
    }
    
    public Rect GetWorldRect() {
        Vector2 size = area;
        Vector2 center = transform.position;
        return new Rect(center - size * 0.5f, size);
    }
    
    public Vector2 ClampToArea(Vector2 p) {
        var r = GetWorldRect();
        return new Vector2(
            Mathf.Clamp(p.x, r.xMin, r.xMax),
            Mathf.Clamp(p.y, r.yMin, r.yMax)
        );
    }
    
    public Vector2 RandomPointInArea() {
        var r = GetWorldRect();
        return new Vector2(Random.Range(r.xMin, r.xMax), Random.Range(r.yMin, r.yMax));
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.25f);
        Gizmos.DrawWireCube(transform.position, new Vector3(area.x, area.y, 0f));
    }
#endif
}
