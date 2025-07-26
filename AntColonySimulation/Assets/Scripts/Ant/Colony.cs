using UnityEngine;

public class Colony : MonoBehaviour
{
    [Header("Ant Settings")]
    public GameObject antPrefab;
    public int numberOfAnts = 1;
    public float spawnRadius = 1.5f;

    void Start()
    {
        for (int i = 0; i < numberOfAnts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector2 spawnPos = (Vector2)transform.position + offset;

            GameObject antObj = Instantiate(antPrefab, spawnPos, Quaternion.identity);

            // Předáme mravenci referenci na hnízdo
            AntAgent agent = antObj.GetComponent<AntAgent>();
            if (agent != null)
            {
                agent.nestTransform = this.transform;
            }
        }
    }
}