using UnityEngine;
using UnityEngine.InputSystem;


public class FoodSpawnerGrid : MonoBehaviour
{
    [Tooltip("Nastavení mřížky")]
    public GridSettings settings;

    [Tooltip("Kolik jednotek jídla se přidá jedním kliknutím.")]
    public int amountPerClick = 50;

    public Camera cam;

    PheromoneGrid grid;

    void Awake()
    {
        if (cam == null)          cam = Camera.main;
        if (settings == null)     settings = Resources.Load<GridSettings>("GridSettings");

        grid = PheromoneGrid.Instance;

    }

    void Update()
    {
        if (grid == null || Mouse.current == null || cam == null) return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector2 mouseScreen = Mouse.current.position.ReadValue();

            Vector3 world3 = cam.ScreenToWorldPoint(
                new Vector3(mouseScreen.x, mouseScreen.y, -cam.transform.position.z));

            Vector2 world = new Vector2(world3.x, world3.y);

            int gx = Mathf.FloorToInt(world.x / settings.cellSize) + settings.width  / 2;
            int gy = Mathf.FloorToInt(world.y / settings.cellSize) + settings.height / 2;

            grid.AddFood(gx, gy, amountPerClick);
        }
    }
}
