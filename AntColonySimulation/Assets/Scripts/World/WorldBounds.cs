using UnityEngine;


namespace World
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class WorldBounds : MonoBehaviour
    {
        public Rect worldRect = new Rect(-50, -30, 100, 60);

        [Header("Wall colliders (optional)")]
        public bool buildColliders = true;
        public float wallThickness = 1f;
        public string wallLayerName = "Walls";

        public static WorldBounds Instance { get; private set; }

        private Transform wallsRoot;

        void OnEnable()
        {
            Instance = this;
            if (buildColliders) BuildOrUpdateWalls();
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void OnValidate()
        {
            if (!Application.isPlaying && buildColliders)
                BuildOrUpdateWalls();
        }


        // mby useful public bool Contains(Vector2 p) => worldRect.Contains(p);

        public Vector2 ClampInsideAndGetNormal(Vector2 p, out Vector2 normal, float margin = 0.001f)
        {
            normal = Vector2.zero;
            float xMin = worldRect.xMin + margin;
            float xMax = worldRect.xMax - margin;
            float yMin = worldRect.yMin + margin;
            float yMax = worldRect.yMax - margin;

            Vector2 q = p;
            if (q.x < xMin) { q.x = xMin; normal = Vector2.right; }
            else if (q.x > xMax) { q.x = xMax; normal = Vector2.left; }

            if (q.y < yMin)
            {
                normal = (normal == Vector2.zero) ? Vector2.up : (Vector2.right * normal.x + Vector2.up).normalized;
                q.y = yMin;
            }
            else if (q.y > yMax)
            {
                normal = (normal == Vector2.zero) ? Vector2.down : (Vector2.right * normal.x + Vector2.down).normalized;
                q.y = yMax;
            }
            return q;
        }
        
        public bool RayToBoundary(Vector2 origin, Vector2 dir, out float t)
        {
            t = float.PositiveInfinity;
            if (dir.sqrMagnitude < 1e-8f) return false;
            dir.Normalize();

            float xMin = worldRect.xMin; float xMax = worldRect.xMax;
            float yMin = worldRect.yMin; float yMax = worldRect.yMax;

            if (Mathf.Abs(dir.x) > 1e-8f)
            {
                float tx1 = (xMin - origin.x) / dir.x; 
                float yAtTx1 = origin.y + tx1 * dir.y;
                if (tx1 > 0 && yAtTx1 >= yMin && yAtTx1 <= yMax) t = Mathf.Min(t, tx1);

                float tx2 = (xMax - origin.x) / dir.x; 
                float yAtTx2 = origin.y + tx2 * dir.y;
                if (tx2 > 0 && yAtTx2 >= yMin && yAtTx2 <= yMax) t = Mathf.Min(t, tx2);
            }
            if (Mathf.Abs(dir.y) > 1e-8f)
            {
                float ty1 = (yMin - origin.y) / dir.y; 
                float xAtTy1 = origin.x + ty1 * dir.x;
                if (ty1 > 0 && xAtTy1 >= xMin && xAtTy1 <= xMax) t = Mathf.Min(t, ty1);

                float ty2 = (yMax - origin.y) / dir.y; 
                float xAtTy2 = origin.x + ty2 * dir.x;
                if (ty2 > 0 && xAtTy2 >= xMin && xAtTy2 <= xMax) t = Mathf.Min(t, ty2);
            }

            return float.IsFinite(t);
        }
        
        void BuildOrUpdateWalls()
        {
            if (wallsRoot == null)
            {
                var go = GameObject.Find("__WallsRoot") ?? new GameObject("__WallsRoot");
                wallsRoot = go.transform;
                wallsRoot.SetParent(transform, false);
            }

            int wallLayer = LayerMask.NameToLayer(wallLayerName);
            if (wallLayer == -1) wallLayer = 0;

            CreateOrUpdateWall("LeftWall",  new Vector2(worldRect.xMin - wallThickness * 0.5f, worldRect.center.y), new Vector2(wallThickness, worldRect.height), wallLayer);
            CreateOrUpdateWall("RightWall", new Vector2(worldRect.xMax + wallThickness * 0.5f, worldRect.center.y), new Vector2(wallThickness, worldRect.height), wallLayer);
            CreateOrUpdateWall("BottomWall",new Vector2(worldRect.center.x, worldRect.yMin - wallThickness * 0.5f), new Vector2(worldRect.width + 2f * wallThickness, wallThickness), wallLayer);
            CreateOrUpdateWall("TopWall",   new Vector2(worldRect.center.x, worldRect.yMax + wallThickness * 0.5f), new Vector2(worldRect.width + 2f * wallThickness, wallThickness), wallLayer);
        }

        void CreateOrUpdateWall(string name, Vector2 center, Vector2 size, int layer)
        {
            Transform t = wallsRoot.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                t = go.transform;
                t.SetParent(wallsRoot, false);
                go.layer = layer;
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = false;
            }
            t.position = center;
            var col2D = t.GetComponent<BoxCollider2D>();
            if (col2D) col2D.size = size;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(worldRect.center, worldRect.size);
        }

    }
}

