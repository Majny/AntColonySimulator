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

    [Header("Border settings")]
    public string dirtLayerName = "Dirt";            // Vrstva pro rámeček
    [Min(0.001f)] public float borderThickness = 0.35f; // Tloušťka borderu
    public Color borderColor = Color.gray;           // Barva „dirt“ bordelu
    public int sortingOrder = 50;                    // Sorting order pro SpriteRenderer

    [Header("Debug")]
    public bool showPlayAreaGizmo = false;

    Transform borderRoot;
    BoxCollider2D[] borders = new BoxCollider2D[4];
    SpriteRenderer[] visuals = new SpriteRenderer[4];

    Rect lastRect;
    float lastThickness;

    void OnEnable() => ForceRebuild();

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += () =>
            {
                if (this) ForceRebuild();
            };
        }
    }
#endif

    void LateUpdate()
    {
        if (!playArea) return;
        var r = playArea.GetWorldRect();
        if (r != lastRect || !Mathf.Approximately(borderThickness, lastThickness))
        {
            ForceRebuild();
        }
    }

    [ContextMenu("Rebuild")]
    public void ForceRebuild()
    {
        if (!playArea) return;
        Cleanup();
        EnsureChildren();
        BuildBorder();

        lastRect = playArea.GetWorldRect();
        lastThickness = borderThickness;
    }

    void Cleanup()
    {
        // Najdi všechny děti pojmenované "__Border__" a smaž je
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name.StartsWith("__Border__"))
                SafeDestroy(child.gameObject);
        }
    }


    static void SafeDestroy(Object o)
    {
        if (!o) return;
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }

    void EnsureChildren()
    {
        borderRoot = new GameObject("__Border__").transform;
        borderRoot.SetParent(transform, false);

        int dirtLayer = LayerMask.NameToLayer(dirtLayerName);

        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject($"Border_{i}");
            go.transform.SetParent(borderRoot, false);
            if (dirtLayer >= 0) go.layer = dirtLayer;

            borders[i] = go.AddComponent<BoxCollider2D>();

            // vizuální část – plný sprite jako čára
            visuals[i] = go.AddComponent<SpriteRenderer>();
            visuals[i].sprite = Texture2D.whiteTexture.ToSprite();
            visuals[i].color = borderColor;
            visuals[i].sortingOrder = sortingOrder;
        }
    }

    void BuildBorder()
    {
        var r = playArea.GetWorldRect();
        float t = borderThickness;
        Vector2 c = r.center;

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

        for (int i = 0; i < 4; i++)
        {
            borders[i].offset = Vector2.zero;
            borders[i].size = sizes[i];
            borders[i].transform.position = centers[i];

            visuals[i].drawMode = SpriteDrawMode.Sliced;
            visuals[i].size = sizes[i];
        }
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

// Pomocná ext metoda na převod Texture2D na Sprite
static class Texture2DExt
{
    public static Sprite ToSprite(this Texture2D tex) =>
        Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f));
}
