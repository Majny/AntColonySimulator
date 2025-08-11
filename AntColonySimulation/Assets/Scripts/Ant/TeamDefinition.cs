using UnityEngine;

[CreateAssetMenu(menuName = "Ants/Team Definition")]
public class TeamDefinition : ScriptableObject
{
    public int teamId = 1;
    public string teamName = "Team";
    public Color teamColor = Color.cyan;

    [Header("Pheromone colors (optional)")]
    public Color homeColor = new Color(0.2f, 1f, 0.6f);
    public Color foodColor = new Color(1f, 0.5f, 0.2f);
}