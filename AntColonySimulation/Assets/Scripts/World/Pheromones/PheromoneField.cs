using System.Collections.Generic;
using UnityEngine;

public class PheromoneField : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Konfigurace

    public Vector2 area;                   // Rozměr obdélníkové oblasti pole
    public AgentParameters agentParams;    // Parametry agenta

    [Header("Visuals")]
    public ParticleSystem particleDisplay; // Volitelný vizuál stopy přes částice
    public Color pheremoneColor = Color.white;
    public float pheremoneSize = 0.05f;
    public float initialAlpha = 1f;

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VNITŘNÍ STAV 
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Vnitřní stav

    private ParticleSystem.EmitParams emitParams;

    private int numCellsX, numCellsY;
    private Vector2 halfSize;
    private float cellSizeReciprocal;
    private Cell[,] cells;

    // Vrací aktuální čas odpařování.
    float EvapTime => agentParams.pheromoneEvaporateTime;

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity lifecycle

    // Inicializuje mřížku buněk a připraví particle systém.
    void Awake() => Init();

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // INITIALIZACE A VIZUÁLY
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Init + Visuals

    // Postaví mřížku buněk, spočítá rozměry a nastaví particle display.
    void Init()
    {
        if (particleDisplay == null)
            particleDisplay = GetComponentInChildren<ParticleSystem>(true) ?? GetComponent<ParticleSystem>();

        float perceptionRadius = Mathf.Max(
            0.01f,
            (agentParams != null && agentParams.pheromoneSensorSize > 0f) ? agentParams.pheromoneSensorSize : 0.75f
        );
        
        numCellsX = Mathf.CeilToInt(area.x / perceptionRadius);
        numCellsY = Mathf.CeilToInt(area.y / perceptionRadius);
        halfSize = new Vector2(numCellsX * perceptionRadius, numCellsY * perceptionRadius) * 0.5f;
        cellSizeReciprocal = 1f / perceptionRadius;

        cells = new Cell[numCellsX, numCellsY];
        for (int y = 0; y < numCellsY; y++)
            for (int x = 0; x < numCellsX; x++)
                cells[x, y] = new Cell();

        SetupParticleSystemVisuals();

        if (particleDisplay != null)
        {
            if (!particleDisplay.isPlaying) particleDisplay.Play();
            var rend = particleDisplay.GetComponent<Renderer>();
            if (rend) rend.sortingOrder = 9999;
        }
        else
        {
            Debug.LogWarning($"[PheromoneField] No ParticleSystem found on {name}. Trails will be invisible.");
        }
    }

    // Nastaví vzhled a chování částic tak, aby odpovídaly evaporaci a velikosti stop.
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

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VEŘEJNÉ API
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Veřejné API

    // Zapíše novou kapku feromonu do příslušné buňky a emituje částici.
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

    // Vrátí celkovou sílu stopy v okolí dané pozice a průběžně odstraňuje vyprchlé stopy.
    public float SampleStrength(Vector2 worldPos, float radius)
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
                    var cur = node.Value;
                    var next = node.Next;

                    float weight = cur.initialWeight;
                    if (!infinite)
                    {
                        float age = now - cur.creationTime;
                        if (age > evap)
                        {
                            cell.entries.Remove(node);
                            node = next;
                            continue;
                        }
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

    // Zapne/vypne vizibilitu částic, podle možností rendereru.
    public void SetVisible(bool visible)
    {
        if (particleDisplay)
        {
            var rends = particleDisplay.GetComponentsInChildren<Renderer>(true);
            if (rends != null && rends.Length > 0)
            {
                foreach (var r in rends) r.enabled = visible;
                return;
            }
        }
        gameObject.SetActive(visible);
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // POMOCNÉ FUNKCE (GEOMETRIE / MŘÍŽKA)
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Helpers

    // Převede světovou pozici na index buňky s ořezem do rozsahu mřížky.
    Vector2Int CellCoordFromPos(Vector2 cell)
    {
        int x = (int)((cell.x + halfSize.x) * cellSizeReciprocal);
        int y = (int)((cell.y + halfSize.y) * cellSizeReciprocal);
        return new Vector2Int(Mathf.Clamp(x, 0, numCellsX - 1), Mathf.Clamp(y, 0, numCellsY - 1));
    }

    // Vrátí světový obdélník oblasti pole.
    public Rect GetWorldRect()
    {
        Vector2 size = area;
        Vector2 center = transform.position;
        return new Rect(center - size * 0.5f, size);
    }

    // Omezí bod do hranic oblasti pole.
    public Vector2 ClampToArea(Vector2 p)
    {
        var r = GetWorldRect();
        return new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax));
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // DATOVÉ TYPY (CELL / ENTRY)
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Data types

    public class Cell
    {
        public LinkedList<Entry> entries = new();
        // Přidá záznam stopy na konec seznamu dané buňky.
        public void Add(Entry e) => entries.AddLast(e);
    }

    public struct Entry
    {
        public Vector2 position;
        public float initialWeight;
        public float creationTime;
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // DEBUG / EDITOR
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Debug (Editor)

    // V editoru vykreslí hranice oblasti feromonového pole.
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.25f);
        Gizmos.DrawWireCube(transform.position, new Vector3(area.x, area.y, 0f));
    }
    #endif

    #endregion
}
