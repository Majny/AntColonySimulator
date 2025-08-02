using UnityEngine;

public class Colony : MonoBehaviour
{
    [Header("Ant Settings")]
    public GameObject antPrefab;
    public int numberOfAnts = 20;
    public float spawnRadius = 1.5f;

    void Start()
    {
        for (int i = 0; i < numberOfAnts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector2 spawnPos = (Vector2)transform.position + offset;
            Instantiate(antPrefab, spawnPos, Quaternion.identity);
        }
    }
}