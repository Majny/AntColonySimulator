using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class LevelEditor : MonoBehaviour
{
    public enum Tool { None, Food, Nest, Dirt }

    [Header("Prefabs")]
    public GameObject foodSpawnerPrefab;
    public GameObject nestPrefab;
    public GameObject dirtSegmentPrefab;

    [Header("Scene refs (pheromones)")]
    public PheromoneField homeField;
    public PheromoneField foodField;

    [Header("Food settings")]
    public int foodAmount = 20;

    [Header("Dirt drawing")]
    public float initialDirtRadius = .6f;
    public float minDirtRadius = .2f, maxDirtRadius = 2f;

    [Header("Runtime-UI preview")]
    public Sprite previewCircleSprite;

    Tool currentTool = Tool.None;
    bool editing = true;

    Camera cam;
    SpriteRenderer preview;
    float dirtRadius;
    readonly List<GameObject> spawnedInEdit = new();

    bool drawing;
    Vector2 lastDirtPoint;

    void Awake()
    {
        cam = Camera.main ? Camera.main : FindFirstObjectByType<Camera>();
        dirtRadius = initialDirtRadius;

        var go = new GameObject("PreviewCircle");
        preview = go.AddComponent<SpriteRenderer>();
        if (previewCircleSprite) preview.sprite = previewCircleSprite;
        preview.sortingOrder = 9000;
        preview.enabled = false;
        UpdatePreviewScale();
    }

    void Update()
    {
        if (!editing || cam == null) return;

        Vector2 screen = Mouse.current.position.ReadValue();
        Vector2 worldPos = cam.ScreenToWorldPoint(screen);

        if (currentTool == Tool.Dirt)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                dirtRadius = Mathf.Clamp(dirtRadius + scroll * .1f, minDirtRadius, maxDirtRadius);
                UpdatePreviewScale();
            }
        }

        preview.transform.position = worldPos;

        if (Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUI())
        {
            switch (currentTool)
            {
                case Tool.Food: Place(foodSpawnerPrefab, worldPos); break;
                case Tool.Nest: Place(nestPrefab,        worldPos); break;
                case Tool.Dirt: BeginDirtStroke(worldPos);          break;
            }
        }

        if (currentTool == Tool.Dirt && drawing)
        {
            float step = dirtRadius * .6f;
            if (Vector2.Distance(worldPos, lastDirtPoint) >= step)
            {
                SpawnDirtSegment(worldPos);
                lastDirtPoint = worldPos;
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
                drawing = false;
        }
    }

    public void SetFoodAmount(int amount)
    {
        foodAmount = amount;
    }

    public void SelectFood() => SetTool(Tool.Food);
    public void SelectNest() => SetTool(Tool.Nest);
    public void SelectDirt() => SetTool(Tool.Dirt);

    public void StartSimulation()
    {
        editing = false;
        currentTool = Tool.None;
        preview.enabled = false;

        foreach (var nest in FindObjectsByType<NestController>(FindObjectsSortMode.None))
        {
            if (homeField) nest.homeMarkersField = homeField;
            if (foodField) nest.foodMarkersField = foodField;
            nest.enabled = true;
        }
    }

    void SetTool(Tool t)
    {
        currentTool = t;
        preview.enabled = (t == Tool.Dirt) && preview.sprite != null;
        UpdatePreviewScale();
    }

    void UpdatePreviewScale() => preview.transform.localScale = Vector3.one * dirtRadius * 2f;

    void Place(GameObject prefab, Vector2 pos)
    {
        if (!prefab) return;

        var go = Instantiate(prefab, pos, Quaternion.identity);
        spawnedInEdit.Add(go);

        if (go.TryGetComponent<NestController>(out var nc))
        {
            if (homeField) nc.homeMarkersField = homeField;
            if (foodField) nc.foodMarkersField = foodField;
            nc.enabled = false;
        }

        if (go.TryGetComponent<FoodSpawner>(out var fs))
        {
            fs.amount = foodAmount;
        }
    }

    void BeginDirtStroke(Vector2 start)
    {
        drawing = true;
        lastDirtPoint = start;
        SpawnDirtSegment(start);
    }

    void SpawnDirtSegment(Vector2 pos)
    {
        if (!dirtSegmentPrefab) return;

        var seg = Instantiate(dirtSegmentPrefab, pos, Quaternion.identity);
        seg.transform.localScale = Vector3.one * dirtRadius * 2f;

        if (seg.TryGetComponent(out CircleCollider2D col))
            col.radius = dirtRadius;

        spawnedInEdit.Add(seg);
    }

    static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}