using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class FoodSpawnerGrid : MonoBehaviour
{
    [Tooltip("Nastavení mřížky (šířka, výška, cellSize).")]
    public GridSettings settings;

    [Tooltip("Kolik jednotek jídla připíšu do každé buňky.")]
    public int amountPerCell = 5;

    [Tooltip("Poloměr kruhu (v buňkách) – 0 = jedna buňka.")]
    public int radius = 2;

    [Tooltip("Kamera, z níž se dělá Screen → World.")]
    public Camera cam;

    PheromoneGrid grid;

    void Start()
    {
        if (!cam)          cam      = Camera.main;
        if (!settings)     settings = Resources.Load<GridSettings>("GridSettings");
        grid = PheromoneGrid.Instance;

        if (!grid)
            Debug.LogError("FoodSpawnerGrid: Ve scéně chybí PheromoneGrid!");
    }

    void Update()
    {
        if (grid == null || Mouse.current == null) return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector2 mouseScr = Mouse.current.position.ReadValue();
            Vector3 world3 = cam.ScreenToWorldPoint(
                new Vector3(mouseScr.x, mouseScr.y, -cam.transform.position.z));

            int gx = Mathf.FloorToInt(world3.x / settings.cellSize) + settings.width  / 2;
            int gy = Mathf.FloorToInt(world3.y / settings.cellSize) + settings.height / 2;

            if (gx < 0 || gx >= settings.width || gy < 0 || gy >= settings.height)
                return;

            for (int dx = -radius; dx <= radius; ++dx)
            for (int dy = -radius; dy <= radius; ++dy)
            {
                if (dx * dx + dy * dy > radius * radius) continue; 
                grid.AddFood(gx + dx, gy + dy, amountPerCell);
            }

        }
    }
}
