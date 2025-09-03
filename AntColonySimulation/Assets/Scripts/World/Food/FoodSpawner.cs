using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Konfigurace

    [Header("Basic")]
    public float radius = 3f;                 // Poloměr hlavního kruhu spawnu
    public int amount = 20;                   // Cílový počet kusů jídla
    public bool maintainAmount = true;        // Udržovat průběžně cílový počet
    public float timeBetweenSpawns = 2f;      // Interval mezi auto-spawny
    public GameObject foodPrefab;             // Prefab položky jídla

    [Header("Clustering")]
    public int blobCount = 1;                 // Počet dodatečných shluků
    public int seed = 0;                      // Seed pro deterministické rozmístění

    [Header("Spawn rules")]
    public float clearance = 0.25f;           // Minimální odstup od překážek
    public LayerMask dirtMask;                // Vrstva pro Dirt
    public LayerMask nestMask;                // Vrstva pro Nest
    public int maxSpawnTries = 6;             // Kolikrát zkusit najít validní místo

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VNITŘNÍ STAV
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Vnitřní stav

    System.Random prng;
    Vector3[] blobs;
    float nextSpawnTime;

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity lifecycle

    void Awake()
    {
        BuildBlobs();
    }

    void Start()
    {
        Rebuild();
    }

    void Update()
    {
        if (!maintainAmount || foodPrefab == null) return;

        if (transform.childCount < amount && Time.time >= nextSpawnTime)
        {
            SpawnFood();
            nextSpawnTime = Time.time + timeBetweenSpawns;
        }
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VEŘEJNÉ AKCE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Veřejné akce

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        for (int i = 0; i < amount; i++)
            SpawnFood();

        nextSpawnTime = Time.time + timeBetweenSpawns;
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // POMOCNÉ FUNKCE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Helpers

    void BuildBlobs()
    {
        Random.InitState(seed);
        prng = new System.Random(seed);

        blobs = new Vector3[blobCount + 1];
        blobs[0] = new Vector3(transform.position.x, transform.position.y, radius);

        for (int i = 0; i < blobCount; i++)
        {
            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * radius;
            float r = Mathf.Lerp(radius * 0.2f, radius * 0.5f, Random.value);
            blobs[i + 1] = new Vector3(pos.x, pos.y, r);
        }
    }

    // Zkontroluje, zda pozice není v Dirt ani v Nest
    bool IsValidSpawn(Vector2 position)
    {
        if (Physics2D.OverlapCircle(position, clearance, dirtMask) != null)
            return false;

        if (Physics2D.OverlapCircle(position, clearance, nestMask) != null)
            return false;

        return true;
    }

    void SpawnFood()
    {
        if (foodPrefab == null || blobs == null || blobs.Length == 0) return;

        for (int attempt = 0; attempt < maxSpawnTries; attempt++)
        {
            Vector3 blob = blobs[prng.Next(0, blobs.Length)];
            Vector2 p = (Vector2)blob + Random.insideUnitCircle.normalized * blob.z * Mathf.Min(Random.value, Random.value);

            if (!IsValidSpawn(p))
                continue;

            var go = Instantiate(foodPrefab, p, Quaternion.identity, transform);

            int foodLayer = LayerMask.NameToLayer("Food");
            if (foodLayer >= 0) go.layer = foodLayer;

            return;
        }
    }

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
    #endif

    #endregion
}
