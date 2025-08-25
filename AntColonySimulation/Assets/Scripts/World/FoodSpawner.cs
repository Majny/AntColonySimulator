using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Konfigurace

    public float radius = 3f;                 // Poloměr oblasti spawnu
    public int amount = 20;                   // Cílový počet kusů jídla
    public bool maintainAmount = true;        // Udržovat průběžně cílový počet
    public float timeBetweenSpawns = 2f;      // Interval mezi auto-spawny
    public GameObject foodPrefab;             // Prefab položky jídla

    [Header("Clustering")]
    public int blobCount = 1;                 // Počet shluků v rámci oblasti
    public int seed = 0;                      // Seed pro deterministické rozmístění
    
        [Header("Spawn rules")]
    public float spawnClearanceRadius = 0.25f; // Odstup od hlíny
    public LayerMask obstacleMask;             // Nastav na vrstvu "Obstacle"
    public PheromoneField playArea;            // Stejná area jako v LevelEditoru
    public int maxSpawnTries = 6;              // Kolikrát zkusit najít validní místo


    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VNITŘNÍ STAV
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Vnitřní stav

    System.Random prng;       // Nezávislý PRNG pro výběr blobu
    Vector3[] blobs;          // Pole shluků: (x, y, r)
    float nextSpawnTime;      // Čas dalšího automatického spawnu

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity lifecycle

    // Připraví shluky (centra a jejich poloměry) podle seedu a radiusu.
    void Awake()
    {
        BuildBlobs();
    }

    // Naplní spawner počátečním množstvím a nastaví čas spawnu.
    void Start()
    {
        Rebuild();
    }

    // Průběžně doplňuje chybějící kusy jídla podle intervalu.
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

    // Editorová akce: smaže všechny potomky a znovu je naspawnuje dle 'amount'.
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

    // Vytvoří seznam shluků.
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

    // Vytvoří jeden kus jídla v rámci náhodně zvoleného shluku.
    void SpawnFood()
    {
        if (foodPrefab == null) return;

        Vector3 blob = blobs[prng.Next(0, blobs.Length)];
        Vector2 p = (Vector2)blob + Random.insideUnitCircle.normalized * blob.z * Mathf.Min(Random.value, Random.value);

        var go = Instantiate(foodPrefab, p, Quaternion.identity, transform);

        int foodLayer = LayerMask.NameToLayer("Food");
        if (foodLayer >= 0) go.layer = foodLayer;
    }

    #if UNITY_EDITOR
    // V editoru vykreslí pomocnou kružnici oblasti spawnu.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
    #endif

    #endregion
}
