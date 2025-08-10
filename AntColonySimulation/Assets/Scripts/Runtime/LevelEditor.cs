using System;
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

    [Header("Nest settings")]
    [Tooltip("Kolik mravenců se má spawnout v každém NOVĚ položeném hnízdě.")]
    public int nestInitialAgents = 10;

    [Header("Dirt drawing")]
    public float initialDirtRadius = .6f;
    public float minDirtRadius = .2f, maxDirtRadius = 2f;

    [Header("Dirt collision")]
    [Tooltip("Vrstva, na které budou dirt segmenty. Musí být zahrnutá v AntAgent.obstacleMask.")]
    public string dirtObstacleLayerName = "Obstacle";
    [Tooltip("Pokud true, dirt je pouze trigger (mravenci projdou). Pro pevnou zeď nech false.")]
    public bool dirtIsTrigger = false;

    [Header("Dirt brush tuning")]
    [Tooltip("Krok mezi body při tažení (násobek poloměru). Menší = hustší tah.")]
    public float strokeStepFactor = 0.5f;
    [Tooltip("Kolik % navíc zvětšit collider vůči vizuálnímu poloměru.")]
    public float colliderOverlap = 1.05f;

    [Header("Runtime-UI preview")]
    public Sprite previewCircleSprite;

    public event Action<float> OnDirtRadiusChanged;

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
        EnsurePreview();
        SetDirtRadius(initialDirtRadius);
        if (preview) preview.enabled = false;
        UpdatePreviewScale();
    }

    void OnEnable()
    {
        EnsurePreview();
        if (preview) preview.enabled = false;
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
                SetDirtRadius(dirtRadius + scroll * .1f);
        }

        EnsurePreview();
        if (preview) preview.transform.position = worldPos;

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
            float step = dirtRadius * Mathf.Max(0.05f, strokeStepFactor);
            if (Vector2.Distance(worldPos, lastDirtPoint) >= step)
            {
                SpawnDirtSegment(worldPos);
                lastDirtPoint = worldPos;
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
                drawing = false;
        }
    }

    public float GetDirtRadius() => dirtRadius;
    public float GetDirtRadiusMin() => minDirtRadius;
    public float GetDirtRadiusMax() => maxDirtRadius;

    public void SetDirtRadius(float value)
    {
        float clamped = Mathf.Clamp(value, minDirtRadius, maxDirtRadius);
        if (Mathf.Approximately(clamped, dirtRadius)) return;
        dirtRadius = clamped;
        UpdatePreviewScale();
        OnDirtRadiusChanged?.Invoke(dirtRadius);
    }

    public void SetFoodAmount(int amount) => foodAmount = amount;

    public void SetNestInitialAgents(int count)
    {
        nestInitialAgents = Mathf.Max(0, count);
    }

    public void SelectFood() => SetTool(Tool.Food);
    public void SelectNest() => SetTool(Tool.Nest);
    public void SelectDirt() => SetTool(Tool.Dirt);

    public void StartSimulation()
    {
        editing = false;
        currentTool = Tool.None;
        if (preview) preview.enabled = false;

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
        EnsurePreview();
        if (preview) preview.enabled = (t == Tool.Dirt) && preview.sprite != null;
        UpdatePreviewScale();
    }

    void UpdatePreviewScale()
    {
        if (!preview) return;
        preview.transform.localScale = Vector3.one * dirtRadius * 2f;
    }

    void EnsurePreview()
    {
        if (preview != null) return;
        var go = GameObject.Find("PreviewCircle");
        if (!go) go = new GameObject("PreviewCircle");
        preview = go.GetComponent<SpriteRenderer>();
        if (!preview) preview = go.AddComponent<SpriteRenderer>();
        if (previewCircleSprite) preview.sprite = previewCircleSprite;
        preview.sortingOrder = 9000;
        preview.enabled = false;
    }

    void Place(GameObject prefab, Vector2 pos)
    {
        if (!prefab) return;

        var go = Instantiate(prefab, pos, Quaternion.identity);
        spawnedInEdit.Add(go);

        if (go.TryGetComponent<NestController>(out var nc))
        {
            if (homeField) nc.homeMarkersField = homeField;
            if (foodField) nc.foodMarkersField = foodField;

            nc.initialAgents = nestInitialAgents;

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

        int layer = LayerMask.NameToLayer(dirtObstacleLayerName);
        if (layer >= 0) seg.layer = layer;

        var col = seg.GetComponent<CircleCollider2D>();
        if (!col) col = seg.AddComponent<CircleCollider2D>();
        col.isTrigger = dirtIsTrigger;
        col.radius = dirtRadius * Mathf.Max(1f, colliderOverlap);

        seg.transform.localScale = Vector3.one * dirtRadius * 2f;
        spawnedInEdit.Add(seg);
    }

    static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}
