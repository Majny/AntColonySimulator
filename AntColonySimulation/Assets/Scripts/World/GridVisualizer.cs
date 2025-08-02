using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class GridVisualizer : MonoBehaviour
{
    public PheromoneGrid grid;
    public Gradient foodGradient;
    public Gradient pheromoneGradient;

    private Texture2D texture;
    private Color[] pixels;
    private int width, height;

    void Start()
    {
        if (grid == null) grid = PheromoneGrid.Instance;
        width = grid.settings.width;
        height = grid.settings.height;
        texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Point;
        pixels = new Color[width * height];

        Renderer rend = GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Unlit/Texture"));
        rend.material.mainTexture = texture;
    }

    void Update()
    {
        float[,] foodTrail = grid.FoodTrail;
        float[,] homeTrail = grid.Home;
        int[,] food = grid.FoodSource;
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                Color col = Color.black;
                if (food[x, y] > 0)
                {
                    col = Color.green;
                }
                else 
                {
                    float toHome = homeTrail[x, y];
                    float toFood = foodTrail[x, y];
                    float blue = Mathf.Clamp01(toHome);
                    float red  = Mathf.Clamp01(toFood);
                    col = new Color(red, 0f, blue);
                }
                pixels[y * width + x] = col;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();  
    }
}
