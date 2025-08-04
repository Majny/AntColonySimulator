using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject foodPrefab;
    public float spawnRadius = 3f;
    public int initialAmount = 10;
    public bool maintainAmount = true;
    public float spawnDelay = 2f;

    private float nextSpawnTime;

    void Start()
    {
        for (int i = 0; i < initialAmount; i++)
        {
            SpawnOneFood();
        }
    }

    void Update()
    {
        if (maintainAmount && Time.time >= nextSpawnTime && transform.childCount < initialAmount)
        {
            SpawnOneFood();
            nextSpawnTime = Time.time + spawnDelay;
        }
    }

    private void SpawnOneFood()
    {
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector2 spawnPos = (Vector2)transform.position + offset;
        Instantiate(foodPrefab, spawnPos, Quaternion.identity, transform);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}