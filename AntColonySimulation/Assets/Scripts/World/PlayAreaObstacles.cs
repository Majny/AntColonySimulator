using UnityEngine;

[ExecuteAlways]
public class PlayAreaObstacles : MonoBehaviour
{
    public PheromoneField playArea;
    public float thickness = 0.6f;
    public string obstacleLayerName = "Obstacle";

    [SerializeField] BoxCollider2D[] walls;

    void OnEnable() => Build(setLayers: true);
#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying) Build(setLayers: false);
    }
#endif

    void EnsureArray()
    {
        if (walls == null || walls.Length != 4)
            walls = new BoxCollider2D[4];
    }

    public void Build(bool setLayers)
    {
        if (playArea == null) return;

        EnsureArray();

        for (int i = 0; i < 4; i++)
        {
            string childName = $"Wall_{i}";
            Transform t = transform.Find(childName);
            if (t == null)
            {
                var go = new GameObject(childName);
                go.transform.SetParent(transform, false);
                t = go.transform;
            }

            var col = t.GetComponent<BoxCollider2D>();
            if (col == null)
                col = t.gameObject.AddComponent<BoxCollider2D>();

            walls[i] = col;

            if (setLayers)
            {
                int layer = LayerMask.NameToLayer(obstacleLayerName);
                if (layer >= 0) t.gameObject.layer = layer;
            }
        }

        var rect = playArea.GetWorldRect();
        Vector2 sz = rect.size;
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
            new(thickness, sz.y + thickness * 2f),
            new(thickness, sz.y + thickness * 2f),
            new(sz.x + thickness * 2f, thickness),
            new(sz.x + thickness * 2f, thickness),
        };

        for (int i = 0; i < 4; i++)
        {
            var w = walls[i];
            w.isTrigger = false;
            w.offset = Vector2.zero;
            w.size = siz[i];
            w.transform.position = pos[i];
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
