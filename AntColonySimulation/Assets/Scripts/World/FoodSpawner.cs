using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    public float radius = 3f;
    public int amount = 20; 
    public bool maintainAmount = true;
    public float timeBetweenSpawns = 2f;
    public GameObject foodPrefab;

    [Header("Clustering")]
    public int blobCount = 3;
    public int seed = 0;

    System.Random prng;
    Vector3[] blobs;
    float nextSpawnTime;

    void Awake()
    {
        BuildBlobs();

        for (int i = 0; i < amount; i++)
            SpawnFood();

        nextSpawnTime = Time.time + timeBetweenSpawns;
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
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
