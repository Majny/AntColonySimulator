using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LevelEditor : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // TYPY A UDÁLOSTI
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Typy a události

    public enum Tool { None, Food, Nest, Dirt, Rubber }

    // Notifikace pro UI slider apod. při změně poloměru štětce.
    public event Action<float> OnDirtRadiusChanged;

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Konfigurace z Inspectoru

    [Header("Prefabs")]
    public GameObject foodSpawnerPrefab;     // Spawner jídla
    public GameObject nestPrefab;            // Hnízdo
    public GameObject dirtSegmentPrefab;     // Segment hlíny

    [Header("Teams (fixed 0..4)")]
    [Range(0, TeamManager.MaxTeams - 1)]
    public int currentTeamIndex = 0;         // Aktivní tým pro nástroj Nest

    [Header("Food settings")]
    public int foodAmount = 20;              // Množství jídla pro nově položený FoodSpawner

    [Header("Nest settings")]
    public int nestInitialAgents = 10;       // Počet agentů pro nově položené hnízdo

    [Header("Dirt drawing")]
    public float initialDirtRadius = .6f;    // Výchozí poloměr štětce
    public float minDirtRadius = .2f, maxDirtRadius = 2f;

    [Header("Dirt collision")]
    public string dirtObstacleLayerName = "Obstacle"; // Vrstva pro Dirt, kvůli kolizím v simulaci
    public bool dirtIsTrigger = false;                // Pokud true, Dirt nekoliduje

    [Header("Dirt brush tuning")]
    public float strokeStepFactor = 0.5f;    // Hustota segmentů po dráze tahu
    public float colliderOverlap = 1.05f;    // Naddimenzování radiusu vůči scalingu

    [Header("Runtime-UI preview")]
    public Sprite previewCircleSprite;       // Volitelná sprite pro náhled štětce
    

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VNITŘNÍ STAV 
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Vnitřní stav

    Tool currentTool = Tool.None;                    // Aktivní nástroj editace

    Camera cam;                                      // Aktivní kamera
    SpriteRenderer preview;                          // Kruh náhledu pro Dirt/Rubber
    float dirtRadius;                                // Aktuální poloměr štětce
    readonly List<GameObject> spawnedInEdit = new(); // Sledování objektů položených během editace

    bool drawing;                                    // Probíhá aktuálně tah myší?
    Vector2 lastDirtPoint;                           // Poslední bod tahu

    static readonly Collider2D[] eraseBuffer = new Collider2D[64]; // pro mazání Dirt

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity lifecycle

    // Inicializuje kameru, připraví náhled a nastaví výchozí poloměr štětce.
    void Awake()
    {
        cam = Camera.main;
        EnsurePreview();
        SetDirtRadius(initialDirtRadius); // Nastaví radius + pošle event
        if (preview) preview.enabled = false;
        UpdatePreviewScale();
    }

    // Po znovu-aktivaci komponenty zajistí, že náhled existuje a je skrytý.
    void OnEnable()
    {
        if (preview) preview.enabled = false;
    }

    // Řídí vstup z myši, vykreslování náhledu a průběh kreslení/mazání během editace.
    void Update()
    {
        if (cam == null) return;

        // Pozice kurzoru
        Vector2 screen = Mouse.current.position.ReadValue();
        Vector2 worldPos = cam.ScreenToWorldPoint(screen);

        // Scroll, změna poloměru u Dirt/Rubber
        if (currentTool == Tool.Dirt || currentTool == Tool.Rubber)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                SetDirtRadius(dirtRadius + scroll * .1f);
        }

        // Náhled štětce
        if (preview) preview.transform.position = worldPos;

        // Začátek tahu / umístění objektu
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

        // Průběh tahu
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

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VEŘEJNÉ API PRO UI
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Veřejné API (UI)

    // Nastaví aktivní tým pro nově pokládaná hnízda.
    public void SelectTeam(int idx) =>
        currentTeamIndex = Mathf.Clamp(idx, 0, TeamManager.MaxTeams - 1);

    // Vrátí aktuální poloměr štětce pro Dirt/Rubber.
    public float GetDirtRadius() => dirtRadius;

    // Vrátí minimální povolený poloměr štětce.
    public float GetDirtRadiusMin() => minDirtRadius;

    // Vrátí maximální povolený poloměr štětce.
    public float GetDirtRadiusMax() => maxDirtRadius;

    // Nastaví poloměr štětce a notifikuje UI poslechy změny. 
    public void SetDirtRadius(float value)
    {
        float clamped = Mathf.Clamp(value, minDirtRadius, maxDirtRadius);
        if (Mathf.Approximately(clamped, dirtRadius)) return;
        dirtRadius = clamped;
        UpdatePreviewScale();
        OnDirtRadiusChanged?.Invoke(dirtRadius);
    }

    // Nastaví počet kusů jídla pro nově vytvořený spawner.
    public void SetFoodAmount(int amount) => foodAmount = amount;

    // Nastaví počet počátečních agentů pro nově vytvořené hnízdo.
    public void SetNestInitialAgents(int count) => nestInitialAgents = Mathf.Max(0, count);

    // Přepne aktivní nástroj na pokládání spawnerů jídla.
    public void SelectFood() => SetTool(Tool.Food);

    // Přepne aktivní nástroj na pokládání hnízd.
    public void SelectNest() => SetTool(Tool.Nest);

    // Přepne aktivní nástroj na kreslení hlíny.
    public void SelectDirt() => SetTool(Tool.Dirt);

    // Přepne aktivní nástroj na gumu
    public void SelectRubber() => SetTool(Tool.Rubber);

    // Přepnutí z editace do simulace, aktivujeme hnízda.
    public void StartSimulation()
    {
        currentTool = Tool.None;
        if (preview) preview.enabled = false;

        // Povolit všechna hnízda
        foreach (var nest in FindObjectsByType<NestController>(FindObjectsSortMode.None))
            nest.enabled = true;
    }

    // Reset aktuální scény.
    public void ResetLevel()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // LOGIKA VÝBĚRU NÁSTROJE A PREVIEW
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Nástroj a preview

    // Nastaví aktuální nástroj a upraví viditelnost náhledu štětce.
    void SetTool(Tool t)
    {
        currentTool = t;
        if (preview)
            preview.enabled = ((t == Tool.Dirt) || (t == Tool.Rubber)) && preview.sprite != null;

        UpdatePreviewScale();
    }

    // Vytvoří a nastaví SpriteRenderer sloužící jako náhled štětce.
    void EnsurePreview()
    {
        if (preview != null) return;

        var go = GameObject.Find("PreviewCircle");
        if (!go) go = new GameObject("PreviewCircle");

        preview = go.GetComponent<SpriteRenderer>();
        if (!preview) preview = go.AddComponent<SpriteRenderer>();

        if (previewCircleSprite) preview.sprite = previewCircleSprite;
        preview.sortingOrder = 900;
        preview.enabled = false;
    }

    // Aktualizuje velikost náhledu podle aktuálního poloměru štětce.
    void UpdatePreviewScale()
    {
        if (!preview) return;
        preview.transform.localScale = Vector3.one * dirtRadius * 2f;
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UMISŤOVÁNÍ OBJEKTŮ
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Umisťování (Place)

    // Inicializuje zadaný prefab.
    void Place(GameObject prefab, Vector2 pos)
    {
        if (!prefab) return;

        var go = Instantiate(prefab, pos, Quaternion.identity);
        spawnedInEdit.Add(go);

        // Pokud je to Nest, kontrola Dirtu
        if (go.TryGetComponent<NestController>(out var nc))
        {
            float checkRadius = nc.spawnRadius * 1.1f;
            int dirtLayer = LayerMask.NameToLayer(dirtObstacleLayerName);

            if (Physics2D.OverlapCircle(pos, checkRadius, 1 << dirtLayer))
            {
                // Nest koliduje s Dirt, zrušíme ho
                Destroy(go);
                return;
            }

            int tid = Mathf.Clamp(currentTeamIndex, 0, TeamManager.MaxTeams - 1);
            nc.teamId = tid;
            nc.teamColor = TeamManager.TeamColors[tid];
            nc.initialAgents = nestInitialAgents;

            if (TeamManager.Instance) TeamManager.Instance.RegisterNest(tid, nc);
            nc.enabled = false; // Aktivuje se až při simulaci
        }

        // Pokud je to FoodSpawner
        if (go.TryGetComponent<FoodSpawner>(out var fs))
            fs.amount = foodAmount;
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // KRESLENÍ A MAZÁNÍ DIRTU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Dirt (kreslení/mazání)

    // Začne kreslení hlíny a ihned položí první segment.
    void BeginDirtStroke(Vector2 start)
    {
        drawing = true;
        lastDirtPoint = start;
        SpawnDirtSegment(start);
    }

    // Začne mazací tah a ihned vymaže hlínu v počátečním bodě.
    void BeginEraseStroke(Vector2 start)
    {
        drawing = true;
        lastDirtPoint = start;
        EraseDirtAt(start);
    }

    void SpawnDirtSegment(Vector2 pos)
    {
        if (!dirtSegmentPrefab) return;

        // Zkontroluj kolizi s Food nebo Nest
        int n = Physics2D.OverlapCircleNonAlloc(pos, dirtRadius, eraseBuffer, ~0);
        for (int i = 0; i < n; i++)
        {
            var c = eraseBuffer[i];
            if (!c) continue;

            // Pokud je to FoodSpawner, tak smaž
            if (c.GetComponentInParent<FoodSpawner>())
            {
                Destroy(c.GetComponentInParent<FoodSpawner>().gameObject);
                continue;
            }

            // Pokud je to Nest, tak smaž
            if (c.GetComponentInParent<NestController>())
            {
                Destroy(c.GetComponentInParent<NestController>().gameObject);
                continue;
            }
        }

        // Polož Dirt segment
        var seg = Instantiate(dirtSegmentPrefab, pos, Quaternion.identity);

        // Vrstva pro kolize s mravenci
        int layer = LayerMask.NameToLayer(dirtObstacleLayerName);
        if (layer >= 0) seg.layer = layer;

        // Zajistím CircleCollider a nastavím vlastnosti
        var col = seg.GetComponent<CircleCollider2D>();
        if (!col) col = seg.AddComponent<CircleCollider2D>();
        col.isTrigger = dirtIsTrigger;

        // Vizuální měřítko
        seg.transform.localScale = Vector3.one;
        float scale = dirtRadius * 2f;
        seg.transform.localScale = new Vector3(scale, scale, 1f);

        // Reálný kolizní radius
        float overlap = Mathf.Max(1f, colliderOverlap);
        col.radius = 0.5f * overlap;

        // Marker komponenta
        if (!seg.GetComponent<Dirt>()) seg.AddComponent<Dirt>();

        spawnedInEdit.Add(seg);
    }


    // Vymaže všechny Dirt objekty v okolí dané pozice v rozsahu poloměru štětce.
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

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UTILITY
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Utility

    // Vrací true, pokud je aktuální ukazatel myši nad UI (EventSystem), aby se neklikal do scény.
    static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    #endregion
}


// ─────────────────────────────────────────────────────────────────────────────
// MARKER KOMPONENTA PRO DIRT OBJEKTY
// ─────────────────────────────────────────────────────────────────────────────
[DisallowMultipleComponent]
public class Dirt : MonoBehaviour {}
