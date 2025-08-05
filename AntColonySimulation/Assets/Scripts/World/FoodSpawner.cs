using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject foodPrefab;
    public int amount = 30;
    public bool maintainAmount = true;
    public float timeBetweenSpawns = 0.25f;

    public float radius = 4f;
    public int blobCount = 3;
    public int seed = 0;

    [Header("Refs")]
    public PheromoneField playArea;

    System.Random prng;
    Vector3[] blobs;
    float nextSpawnTime;

    void Awake()
    {
        Random.InitState(seed);
        prng = new System.Random(seed);

        blobs = new Vector3[blobCount + 1];
        blobs[0] = new Vector3(transform.position.x, transform.position.y, radius);
        for (int i = 0; i < blobCount; i++)
        {
            Vector2 newPos = (Vector2)transform.position + Random.insideUnitCircle * radius;
            float newRad = Mathf.Lerp(radius * 0.2f, radius * 0.5f, Random.value);
            blobs[i + 1] = new Vector3(newPos.x, newPos.y, newRad);
        }

        for (int i = 0; i < amount; i++)
            SpawnOne();
    }

    void Update()
    {
        if (!maintainAmount) return;

        if (transform.childCount < amount && Time.time >= nextSpawnTime)
        {
            SpawnOne();
            nextSpawnTime = Time.time + timeBetweenSpawns;
        }
    }

    void SpawnOne()
    {
        if (foodPrefab == null) return;

        Vector3 blob = blobs[prng.Next(0, blobs.Length)];
        Vector2 dir = Random.insideUnitCircle.normalized;
        float r = blob.z * Mathf.Min(Random.value, Random.value);
        Vector2 pos = (Vector2)blob + dir * r;

        if (playArea != null)
            pos = playArea.ClampToArea(pos);

        var go = Instantiate(foodPrefab, pos, Quaternion.identity, transform);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
