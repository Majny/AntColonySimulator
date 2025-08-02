using UnityEngine;

[CreateAssetMenu(fileName="GridSettings", menuName="AntSim/Grid Settings")]
public class GridSettings : ScriptableObject
{
    [Header("Grid Size (world units)")]
    public int width  = 256;
    public int height = 256;
    public float cellSize = 1f;

    [Header("Pheromone Physics")]
    public float depositAmount = 1f;
    public float evaporationRate = 0.02f;
    public float diffusionRate   = 0.20f; 
}