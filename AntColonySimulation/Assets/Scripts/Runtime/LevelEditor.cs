using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LevelEditor : MonoBehaviour
{
    public enum Tool
    {
        None, 
        Food, 
        Nest, 
        Dirt, 
        Rubber
    }

    [Header("Prefabs")]
    public GameObject foodSpawnerPrefab;
    public GameObject nestPrefab;
    public GameObject dirtSegmentPrefab;

    [Header("Teams (fixed 0..4)")]
    [Range(0, TeamManager.MaxTeams - 1)]
    public int currentTeamIndex = 0;

    [Header("Food settings")]
    public int foodAmount = 20;

    [Header("Nest settings")]
    public int nestInitialAgents = 10;

    [Header("Dirt drawing")]
    public float initialDirtRadius = .6f;
    public float minDirtRadius = .2f, maxDirtRadius = 2f;

    [Header("Dirt collision")]
    public string dirtObstacleLayerName = "Obstacle";
    public bool dirtIsTrigger = false;

    [Header("Dirt brush tuning")]
    public float strokeStepFactor = 0.5f;
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

    static readonly Collider2D[] eraseBuffer = new Collider2D[64];

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

        if (currentTool == Tool.Dirt || currentTool == Tool.Rubber)
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
                case Tool.Nest: Place(nestPrefab, worldPos); break;
                case Tool.Dirt: BeginDirtStroke(worldPos); break;
                case Tool.Rubber: BeginEraseStroke(worldPos); break;
            }
        }

        if (drawing)
        {
            float step = dirtRadius * Mathf.Max(0.05f, strokeStepFactor);
            if (Vector2.Distance(worldPos, lastDirtPoint) >= step)
            {
                if (currentTool == Tool.Dirt) SpawnDirtSegment(worldPos);
                if (currentTool == Tool.Rubber) EraseDirtAt(worldPos);
                lastDirtPoint = worldPos;
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
                drawing = false;
        }
    }

    public void SelectTeam(int idx)
    {
        currentTeamIndex = Mathf.Clamp(idx, 0, TeamManager.MaxTeams - 1);
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
    public void SetNestInitialAgents(int count) => nestInitialAgents = Mathf.Max(0, count);

    public void SelectFood() => SetTool(Tool.Food);
    public void SelectNest() => SetTool(Tool.Nest);
    public void SelectDirt() => SetTool(Tool.Dirt);
    public void SelectRubber() => SetTool(Tool.Rubber);

    public void StartSimulation()
    {
        editing = false;
        currentTool = Tool.None;
        if (preview) preview.enabled = false;

        foreach (var nest in FindObjectsByType<NestController>(FindObjectsSortMode.None))
            nest.enabled = true;
    }

    void SetTool(Tool t)
    {
        currentTool = t;
        EnsurePreview();
        if (preview) preview.enabled = ((t == Tool.Dirt) || (t == Tool.Rubber)) && preview.sprite != null;
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
            int tid = Mathf.Clamp(currentTeamIndex, 0, TeamManager.MaxTeams - 1);
            nc.teamId = tid;
            nc.teamColor = TeamManager.TeamColors[tid];
            nc.initialAgents = nestInitialAgents;

            if (TeamManager.Instance) TeamManager.Instance.RegisterNest(tid, nc);
            nc.enabled = false;
        }

        if (go.TryGetComponent<FoodSpawner>(out var fs))
            fs.amount = foodAmount;
    }

    void BeginDirtStroke(Vector2 start)
    {
        drawing = true;
        lastDirtPoint = start;
        SpawnDirtSegment(start);
    }

    void BeginEraseStroke(Vector2 start)
    {
        drawing = true;
        lastDirtPoint = start;
        EraseDirtAt(start);
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

        seg.transform.localScale = Vector3.one;
        float scale = dirtRadius * 2f;
        seg.transform.localScale = new Vector3(scale, scale, 1f);

        float overlap = Mathf.Max(1f, colliderOverlap);
        col.radius = 0.5f * overlap;

        if (!seg.GetComponent<Dirt>()) seg.AddComponent<Dirt>();

        spawnedInEdit.Add(seg);
    }

    void EraseDirtAt(Vector2 pos)
    {
        int n = Physics2D.OverlapCircleNonAlloc(pos, dirtRadius, eraseBuffer, ~0);
        for (int i = 0; i < n; i++)
        {
            var c = eraseBuffer[i];
            if (!c) continue;

            var marker = c.GetComponentInParent<Dirt>();
            if (!marker) continue;

            var go = marker.gameObject;
            spawnedInEdit.Remove(go);
            Destroy(go);
        }
    }

    static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
    
    public void ResetLevel()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
    
    
}


[DisallowMultipleComponent]
public class Dirt : MonoBehaviour {}

