using System.Collections.Generic;
using UnityEngine;

public class PheromoneField : MonoBehaviour
{
    [Header("Field Settings")]
    public Vector2 fieldSize = new Vector2(50, 50);
    public float cellSize = 1f;
    public float evaporationTime = 30f;

    private List<Pheromone>[,] grid;
    private int width, height;

    void Awake()
    {
        width = Mathf.CeilToInt(fieldSize.x / cellSize);
        height = Mathf.CeilToInt(fieldSize.y / cellSize);
        grid = new List<Pheromone>[width, height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                grid[x, y] = new List<Pheromone>();
    }

    public void AddPheromone(Vector2 worldPos, float strength)
    {
        Vector2Int cell = WorldToCell(worldPos);
        if (IsInsideGrid(cell))
        {
            grid[cell.x, cell.y].Add(new Pheromone
            {
                position = worldPos,
                timeCreated = Time.time,
                strength = strength
            });
        }
    }

    public float SampleStrength(Vector2 worldPos, float radius)
    {
        float total = 0;
        float radiusSqr = radius * radius;
        Vector2Int center = WorldToCell(worldPos);
        int cellRadius = Mathf.CeilToInt(radius / cellSize);

        for (int y = -cellRadius; y <= cellRadius; y++)
        {
            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                Vector2Int cell = new Vector2Int(center.x + x, center.y + y);
                if (!IsInsideGrid(cell)) continue;

                var cellList = grid[cell.x, cell.y];
                for (int i = cellList.Count - 1; i >= 0; i--)
                {
                    var p = cellList[i];
                    float age = Time.time - p.timeCreated;
                    if (age > evaporationTime)
                    {
                        cellList.RemoveAt(i);
                        continue;
                    }

                    float distSqr = (p.position - worldPos).sqrMagnitude;
                    if (distSqr < radiusSqr)
                    {
                        float decay = 1f - (age / evaporationTime);
                        total += p.strength * decay;
                    }
                }
            }
        }

        return total;
    }

    private Vector2Int WorldToCell(Vector2 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x + fieldSize.x * 0.5f) / cellSize);
        int y = Mathf.FloorToInt((worldPos.y + fieldSize.y * 0.5f) / cellSize);
        return new Vector2Int(x, y);
    }

    private bool IsInsideGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
    }

    private struct Pheromone
    {
        public Vector2 position;
        public float timeCreated;
        public float strength;
    }
}

