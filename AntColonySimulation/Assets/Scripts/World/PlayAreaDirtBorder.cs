// Assets/Scripts/Runtime/PlayAreaDirtBorder.cs
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class PlayAreaDirtBorder : MonoBehaviour
{
    [Header("Authoritative area")]
    public PheromoneField playArea;            // ← přetáhni sem svůj PheromoneField (60 x 35)

    [Header("Dirt prefab (vizuál + collider)")]
    public GameObject dirtSegmentPrefab;       // ← stejný prefab jako pro ruční hlínu (má CircleCollider2D)
    public float segmentRadius = 0.6f;         // tloušťka lemu = poloměr segmentu
    public float segmentSpacing = 0.45f;       // vzdálenost mezi středy (<= 2*radius => překryv)

    [Header("Irregularity (noise)")]
    public float jitterAmplitude = 0.25f;      // max vychýlení dovnitř/ven
    public float jitterFrequency = 0.15f;      // frekvence Perlinu (menší = hladší)
    public int seed = 12345;

    [Header("Collision & layer")]
    public string obstacleLayerName = "Obstacle";

    [Header("Build controls")]
    public bool rebuildOnEnable = true;
    public bool clearOnDisable = true;

    const string ROOT = "Perimeter__Auto";
    readonly List<Transform> spawned = new();

    void OnEnable()
    {
        if (rebuildOnEnable) Rebuild();
    }

    void OnDisable()
    {
        if (clearOnDisable) Clear();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        segmentRadius = Mathf.Max(0.05f, segmentRadius);
        segmentSpacing = Mathf.Clamp(segmentSpacing, 0.05f, segmentRadius * 2f);
        if (!Application.isPlaying) Rebuild();
    }
#endif

    [ContextMenu("Rebuild border")]
    public void Rebuild()
    {
        Clear();
        if (!playArea || !dirtSegmentPrefab) return;

        // „Pravda“: vnitřní hranice hřiště = rect
        var rect = playArea.GetWorldRect();

        // Projdeme celý obvod a nasypeme segmenty v daném kroku
        float perim = rect.width * 2f + rect.height * 2f;
        float step = Mathf.Max(0.05f, segmentSpacing);
        int count = Mathf.CeilToInt(perim / step);

        // Pozice na obvodu (t) -> bod + normála ven (pro jitter)
        // Topologie: jdeme po hranách v pořadí Left→Top→Right→Bottom
        System.Random prng = new System.Random(seed);
        int obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);

        Transform root = EnsureRoot();

        float w = rect.width;
        float h = rect.height;
        Vector2 c = rect.center;
        Vector2 p0 = new(rect.xMin, rect.yMin);
        Vector2 p1 = new(rect.xMin, rect.yMax);
        Vector2 p2 = new(rect.xMax, rect.yMax);
        Vector2 p3 = new(rect.xMax, rect.yMin);

        // Helper na sampling šumu stabilně podél hran
        float kx = jitterFrequency;
        float ky = jitterFrequency * 1.37f;

        // Vygeneruj body po hranách
        var path = new List<(Vector2 pos, Vector2 normal)>(count + 4);
        // Left edge (upwards)
        SampleEdge(path, p0, p1, new Vector2(-1, 0), kx, ky);
        // Top edge (rightwards)
        SampleEdge(path, p1, p2, new Vector2(0, 1), kx, ky);
        // Right edge (downwards)
        SampleEdge(path, p2, p3, new Vector2(1, 0), kx, ky);
        // Bottom edge (leftwards)
        SampleEdge(path, p3, p0, new Vector2(0, -1), kx, ky);

        // Rozmísti segmenty
        float acc = 0f;
        Vector2 cur = path[0].pos;
        int idx = 0;
        for (int i = 1; i < path.Count + 1; i++)
        {
            var a = path[(i - 1) % path.Count];
            var b = path[i % path.Count];

            float segLen = Vector2.Distance(a.pos, b.pos);
            Vector2 dir = (segLen > 1e-4f) ? (b.pos - a.pos) / segLen : Vector2.right;

            while (acc + segLen >= step)
            {
                float t = (step - acc) / segLen;
                Vector2 basePos = a.pos + dir * (t * segLen);

                // jitter dovnitř/ven podle normály hranice + Perlin
                float noise = Mathf.PerlinNoise(basePos.x * kx + seed * 0.01931f,
                                                basePos.y * ky + seed * 0.00777f);
                float centerBias = (noise - 0.5f) * 2f; // -1..1
                Vector2 nrm = Vector2.zero;
                // najdi nejbližší hranu kvůli normále (rychlý hack: porovnej vzdál. od center k okrajům)
                if (Mathf.Abs(basePos.x - rect.xMin) < 0.001f) nrm = Vector2.left;
                else if (Mathf.Abs(basePos.x - rect.xMax) < 0.001f) nrm = Vector2.right;
                else if (Mathf.Abs(basePos.y - rect.yMax) < 0.001f) nrm = Vector2.up;
                else if (Mathf.Abs(basePos.y - rect.yMin) < 0.001f) nrm = Vector2.down;
                else nrm = Vector2.zero;

                Vector2 jitter = nrm * (centerBias * jitterAmplitude);

                Vector2 pos = basePos + jitter;

                // posuň lehce dovnitř, ať vnitřní hrana pásu nekoliduje s rectem
                pos -= nrm * (segmentRadius * 0.2f);

                // spawn
                var go = Instantiate(dirtSegmentPrefab, pos, Quaternion.identity, root);
                go.name = $"seg_{idx++:0000}";
                if (obstacleLayer >= 0) go.layer = obstacleLayer;

                // sjednoť scale + collider radius pro konzistentní tloušťku
                go.transform.localScale = Vector3.one * (segmentRadius * 2f);

                var circle = go.GetComponent<CircleCollider2D>();
                if (circle) circle.radius = segmentRadius;

                spawned.Add(go.transform);
                acc = acc + segLen - step;
                a.pos = basePos; // pokračuj od posledního místa
                segLen = Vector2.Distance(a.pos, b.pos);
            }
            acc += segLen;
        }
    }

    static void SampleEdge(List<(Vector2 pos, Vector2 normal)> outPts,
                           Vector2 a, Vector2 b, Vector2 outward,
                           float kx, float ky)
    {
        float len = Vector2.Distance(a, b);
        int steps = Mathf.Max(2, Mathf.CeilToInt(len / 0.25f)); // hustota vzorku
        Vector2 dir = (len > 1e-4f) ? (b - a) / steps : Vector2.right;

        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = a + dir * i;
            outPts.Add((p, outward));
        }
    }

    [ContextMenu("Clear border")]
    public void Clear()
    {
        spawned.Clear();
        var root = transform.Find(ROOT);
        if (!root) return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(root.GetChild(i).gameObject);
            else Destroy(root.GetChild(i).gameObject);
#else
            Destroy(root.GetChild(i).gameObject);
#endif
        }
    }

    Transform EnsureRoot()
    {
        var t = transform.Find(ROOT);
        if (!t)
        {
            var g = new GameObject(ROOT);
            g.transform.SetParent(transform, false);
            t = g.transform;
        }
        return t;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!playArea) return;
        var r = playArea.GetWorldRect();
        Gizmos.color = new Color(1f,0.8f,0.2f,0.5f);
        Gizmos.DrawWireCube(r.center, r.size);
    }
#endif
}
