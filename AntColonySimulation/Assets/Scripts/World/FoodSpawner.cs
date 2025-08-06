using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public FoodPile pilePrefab;
    public int unitsPerPile = 50;
    public float spawnInterval = 30f;
    public int maxPiles = 10;

    [Header("Area")]
    public PheromoneField playArea;
    public float edgeMargin = 0.6f;

    float nextSpawn;

    void Start()
    {
        SpawnPile();
        nextSpawn = Time.time + spawnInterval;
    }

    void Update()
    {
        if (Time.time >= nextSpawn)
        {
            SpawnPile();
            nextSpawn += spawnInterval;
        }
    }

    void SpawnPile()
    {
        if (pilePrefab == null) return;
        if (maxPiles > 0 && transform.childCount >= maxPiles) return;

        Vector2 pos;
        if (playArea != null)
        {
            var r = playArea.GetWorldRect(); 
            float x = Random.Range(r.xMin + edgeMargin, r.xMax - edgeMargin);
            float y = Random.Range(r.yMin + edgeMargin, r.yMax - edgeMargin);
            pos = new Vector2(x, y);
        }
        else
        {
            pos = (Vector2)transform.position + Random.insideUnitCircle * 5f;
        }

        var pile = Instantiate(pilePrefab, pos, Quaternion.identity, transform);
        pile.SetInitialUnits(unitsPerPile);

        int foodLayer = LayerMask.NameToLayer("Food");
        if (foodLayer != -1) pile.gameObject.layer = foodLayer;

        Debug.Log($"[FoodSpawner] Spawned pile at {pos} with {unitsPerPile} units.");
    }
}