using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class PlayAreaRectBoundary : MonoBehaviour
{
    [Header("Authoritative area")]
    public PheromoneField playArea;

    [Header("Collision walls")]
    public string obstacleLayerName = "Obstacle";
    public float wallInset = 0.05f;
    [Min(0.001f)] public float wallThickness = 0.35f;

    [Header("Optional outline (for player)")]
    public bool showOutline = false;
    public Color outlineColor = Color.black;
    [Min(0.001f)] public float outlineWidth = 0.06f;
    public int sortingOrder = 50;

    [Header("Debug")]
    public bool showPlayAreaGizmo = false;

    Transform wallsRoot;
    Transform outlineRoot;
    BoxCollider2D[] walls = new BoxCollider2D[4];

    Rect lastRect; float lastInset, lastThickness; bool lastShowOutline;

#if UNITY_EDITOR
    bool pendingRebuild;
#endif

    void OnEnable() => ForceRebuild();

#if UNITY_EDITOR
    void OnValidate()
    {
        wallThickness = Mathf.Max(0.001f, wallThickness);
        outlineWidth = Mathf.Max(0.001f, outlineWidth);
        if (!Application.isPlaying)
        {
            pendingRebuild = true;
            EditorApplication.delayCall += () => { if (this && pendingRebuild){ pendingRebuild=false; ForceRebuild(); } };
        }
    }
#endif

    void LateUpdate()
    {
        if (!playArea) return;
        var r = playArea.GetWorldRect();
        if (r != lastRect || !Mathf.Approximately(wallInset,lastInset) || !Mathf.Approximately(wallThickness,lastThickness) || lastShowOutline != showOutline)
            ForceRebuild();
    }

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

    void CleanupLegacy()
    {
        string[] names = { "__BorderCollider__", "__BorderVisual__", "__Composite__", "__Edge__" };
        foreach (var n in names){ var t = transform.Find(n); if (t) SafeDestroy(t.gameObject); }
    }

    static void SafeDestroy(Object o)
    {
        if (!o) return;
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }

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

    void BuildWalls()
    {
        var r = playArea.GetWorldRect();
        r.xMin += wallInset; r.xMax -= wallInset; r.yMin += wallInset; r.yMax -= wallInset;

        float t = wallThickness; Vector2 c = r.center;

        Vector2[] centers =
        {
            new(r.xMin - t*0.5f, c.y),
            new(r.xMax + t*0.5f, c.y),
            new(c.x, r.yMax + t*0.5f),
            new(c.x, r.yMin - t*0.5f)
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

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showPlayAreaGizmo || !playArea) return;
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.35f);
        var rr = playArea.GetWorldRect();
        Gizmos.DrawWireCube(rr.center, rr.size);
    }
#endif
}
