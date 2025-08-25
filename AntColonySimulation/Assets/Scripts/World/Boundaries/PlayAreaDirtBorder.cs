using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class PlayAreaRectBoundary : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Konfigurace

    [Header("Authoritative area")]
    public PheromoneField playArea;                   // Referenční obdélník herní oblasti

    [Header("Collision walls")]
    public string obstacleLayerName = "Obstacle";     // Vrstva pro kolizní "zdi"
    public float wallInset = 0.05f;                   // Vnitřní odsazení zdí od hranice
    [Min(0.001f)] public float wallThickness = 0.35f; // Tloušťka kolizních stěn

    [Header("Optional outline (for player)")]
    public bool showOutline = false;                  // Volitelný vizuální rámeček
    public Color outlineColor = Color.black;       // Barva rámečku
    [Min(0.001f)] public float outlineWidth = 0.06f;  // Šířka čáry rámečku
    public int sortingOrder = 50;                     // Sorting order pro LineRenderer

    [Header("Debug")]
    public bool showPlayAreaGizmo = false;            // Debug náhled oblasti v editoru

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VNITŘNÍ STAV
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Vnitřní stav

    Transform wallsRoot;                              // Rodič pro kolizní stěny
    Transform outlineRoot;                            // Rodič pro grafický rámeček
    BoxCollider2D[] walls = new BoxCollider2D[4];     // 4 stěny: L, P, T, B

    Rect lastRect; 
    float lastInset, lastThickness; 
    bool lastShowOutline;

    #if UNITY_EDITOR
    bool pendingRebuild;
    #endif

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity lifecycle

    // Při aktivaci komponenty vynutí okamžitou rekonstrukci.
    void OnEnable() => ForceRebuild();

    #if UNITY_EDITOR
    // V editoru hlídá změny parametrů a odloženě spouští rebuild.
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            pendingRebuild = true;
            EditorApplication.delayCall += () =>
            {
                if (this && pendingRebuild)
                {
                    pendingRebuild=false; 
                    ForceRebuild();
                }
            };
        }
    }
    #endif

    // Každý frame kontroluje změny velikosti/parametrů a případně přestaví stěny/rámeček.
    void LateUpdate()
    {
        if (!playArea) return;
        var r = playArea.GetWorldRect();
        if (r != lastRect 
            || !Mathf.Approximately(wallInset, lastInset) 
            || !Mathf.Approximately(wallThickness, lastThickness) 
            || lastShowOutline != showOutline)
        {
            ForceRebuild();
        }
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // STAVBA HRANIC A RÁMEČKU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Build

    // Vynutí kompletní rebuild: úklid, vytvoření potomků, stavba zdí a rámečku.
    [ContextMenu("Rebuild")]
    public void ForceRebuild()
    {
        if (!playArea) return;
        CleanupLegacy();
        EnsureChildren();
        BuildWalls();
        BuildOutline();

        lastRect = playArea.GetWorldRect();
        lastInset = wallInset;
        lastThickness = wallThickness;
        lastShowOutline = showOutline;
    }

    // Odstraní staré/nepoužívané uzly z předchozích verzí komponenty.
    void CleanupLegacy()
    {
        string[] names = { "__BorderCollider__", "__BorderVisual__", "__Composite__", "__Edge__" };
        foreach (var n in names){ var t = transform.Find(n); if (t) SafeDestroy(t.gameObject); }
    }

    // Bezpečně zničí objekt jak v play módu, tak v editoru.
    static void SafeDestroy(Object o)
    {
        if (!o) return;
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }

    // Zajistí existenci kontejnerů a vytvoří 4 BoxCollider2D stěny.
    void EnsureChildren()
    {
        wallsRoot = transform.Find("__Walls__");
        if (!wallsRoot){ wallsRoot = new GameObject("__Walls__").transform; wallsRoot.SetParent(transform,false); }
        for (int i = wallsRoot.childCount-1; i>=0; i--) SafeDestroy(wallsRoot.GetChild(i).gameObject);

        int wallLayer = LayerMask.NameToLayer(obstacleLayerName);
        for (int i=0;i<4;i++)
        {
            var go = new GameObject($"Wall_{i}");
            go.transform.SetParent(wallsRoot,false);
            if (wallLayer>=0) go.layer = wallLayer;
            walls[i] = go.AddComponent<BoxCollider2D>();
        }

        outlineRoot = transform.Find("__Outline__");
        if (!outlineRoot){ outlineRoot = new GameObject("__Outline__").transform; outlineRoot.SetParent(transform,false); }
        else for (int i = outlineRoot.childCount-1; i>=0; i--) SafeDestroy(outlineRoot.GetChild(i).gameObject);
    }

    // Přepočítá pozice a rozměry 4 kolizních stěn podle PlayArea a nastavení.
    void BuildWalls()
    {
        var r = playArea.GetWorldRect();
        r.xMin += wallInset; r.xMax -= wallInset; r.yMin += wallInset; r.yMax -= wallInset;

        float t = wallThickness; Vector2 c = r.center;

        Vector2[] centers =
        {
            new(r.xMin - t*0.5f, c.y),     // Left
            new(r.xMax + t*0.5f, c.y),     // Right
            new(c.x, r.yMax + t*0.5f),     // Top
            new(c.x, r.yMin - t*0.5f)      // Bottom
        };
        Vector2[] sizes =
        {
            new(t, r.height + t*2f),
            new(t, r.height + t*2f),
            new(r.width + t*2f, t),
            new(r.width + t*2f, t)
        };

        for (int i=0;i<4;i++)
        {
            walls[i].offset = Vector2.zero;
            walls[i].size = sizes[i];
            walls[i].transform.localPosition = Vector3.zero;
            walls[i].transform.position = centers[i];
        }
    }

    // Podle nastavení vytvoří viditelný rámeček LineRenderem kolem oblasti.
    void BuildOutline()
    {
        if (!showOutline) return;

        var r = playArea.GetWorldRect();
        float eps = Mathf.Min(outlineWidth * 0.5f, 0.02f);
        r.xMin += eps; r.xMax -= eps; r.yMin += eps; r.yMax -= eps;

        var go = new GameObject("Border");
        go.transform.SetParent(outlineRoot,false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.widthMultiplier = outlineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = outlineColor;
        lr.sortingOrder = sortingOrder;

        lr.positionCount = 4;
        lr.SetPositions(new Vector3[]
        {
            new(r.xMin, r.yMin, 0),
            new(r.xMax, r.yMin, 0),
            new(r.xMax, r.yMax, 0),
            new(r.xMin, r.yMax, 0)
        });
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // DEBUG / EDITOR
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Debug (Editor)

    // V editoru volitelně vizualizuje obdélník herní oblasti.
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showPlayAreaGizmo || !playArea) return;
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.35f);
        var rr = playArea.GetWorldRect();
        Gizmos.DrawWireCube(rr.center, rr.size);
    }
    #endif

    #endregion
}
