using UnityEngine;

[ExecuteAlways]
public class PlayAreaObstacles : MonoBehaviour
{
    public PheromoneField playArea;
    public float thickness = 0.6f;
    public string obstacleLayerName = "Obstacle";

    [SerializeField] BoxCollider2D[] walls;

    void OnEnable() => RebuildInternal();
#if UNITY_EDITOR
    void OnValidate() { if (!Application.isPlaying) RebuildInternal(); }
#endif

    [ContextMenu("Rebuild")]
    public void RebuildInternal()
    {
        if (playArea == null) return;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(transform.GetChild(i).gameObject);
            else Destroy(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }

        if (walls == null || walls.Length != 4) walls = new BoxCollider2D[4];
        int layer = LayerMask.NameToLayer(obstacleLayerName);

        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject($"Wall_{i}");
            go.transform.SetParent(transform, false);
            if (layer >= 0) go.layer = layer;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
            col.usedByComposite = false;
            col.offset = Vector2.zero;
            walls[i] = col;
        }

        var rect = playArea.GetWorldRect();
        Vector2 size = rect.size;
        Vector2 c = rect.center;

        Vector2[] pos =
        {
            new(rect.xMin - thickness * 0.5f, c.y),
            new(rect.xMax + thickness * 0.5f, c.y),
            new(c.x, rect.yMax + thickness * 0.5f),
            new(c.x, rect.yMin - thickness * 0.5f),
        };

        Vector2[] siz =
        {
            new(thickness, size.y + thickness * 2f),
            new(thickness, size.y + thickness * 2f),
            new(size.x + thickness * 2f, thickness),
            new(size.x + thickness * 2f, thickness),
        };

        for (int i = 0; i < 4; i++)
        {
            walls[i].size = siz[i];
            walls[i].transform.position = pos[i];
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (playArea == null) return;
        var r = playArea.GetWorldRect();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(r.center, r.size);
    }
#endif
}
