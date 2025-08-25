using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamScoreboard : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Konfigurace z Inspectoru

    [Header("UI Refs")]
    public RectTransform contentRoot;      // Kořen obsahu tabulky (řádky + header)
    public TeamScoreboardRow rowPrefab;    // Prefab jednoho řádku

    [Header("Options")]
    public bool showOnlyTeamsWithNests = false; // Zobrazit jen týmy, které mají alespoň jedno hnízdo
    public bool autoRefresh = true;             // Pravidelně obnovovat obsah
    public float refreshInterval = 0.5f;        // Interval auto-refresh (s)

    [Header("Auto size (panel height)")]
    public bool autoSize = true;                // Automaticky upravovat výšku panelu
    [Range(0.2f, 1f)] public float maxHeightRatio = 0.65f; // Max. poměr výšky vůči rodiči
    public float extraVerticalPadding = 8f;     // Dodatečný vnitřní padding (px)

    [Header("Anchoring / Layout")]
    public bool lockTopRight = true;            // Ukotvit panel do pravého horního rohu
    public Vector2 edgeOffset = new(-20f, -20f);// Posun od okraje (px)

    [Header("Header")]
    public int headerFont = 16;                 // Velikost písma hlavičky
    public float headerStatMinWidth = 44f;      // Min. šířka číselných sloupců v hlavičce
    public float headerSpacing = 6f;            // Svislé mezery mezi řádky (ovlivní i layout)

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VNITŘNÍ STAV
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Vnitřní stav

    readonly Dictionary<int, TeamScoreboardRow> rows = new(); // Mapa teamId → instancovaný řádek
    float refreshTimer;                                       // Akumulátor pro auto-refresh

    RectTransform panel;    // Vlastní RT panelu
    RectTransform headerRoot; // RT uzlu hlavičky

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity lifecycle

    // Připraví ukotvení panelu, zajistí kořen obsahu a vytvoří hlavičku.
    void Awake()
    {
        panel = GetComponent<RectTransform>();

        if (lockTopRight && panel)
        {
            panel.anchorMin = panel.anchorMax = new Vector2(1, 1);
            panel.pivot = new Vector2(1, 1);
            panel.anchoredPosition = edgeOffset;
        }

        EnsureContentRoot();
        EnsureHeader();
    }

    // Při zapnutí komponenty provede okamžitý rebuild tabulky.
    void OnEnable() => TryRebuild();

    // Řídí periodické obnovování tabulky podle intervalu.
    void Update()
    {
        if (!autoRefresh) return;
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            TryRebuild();
        }
    }

    // Udržuje správné ukotvení/pozici panelu a obsahu při změnách v editoru.
    void OnValidate()
    {
        if (!panel) panel = GetComponent<RectTransform>();
        if (panel && lockTopRight)
        {
            panel.anchorMin = panel.anchorMax = new Vector2(1, 1);
            panel.pivot = new Vector2(1, 1);
            panel.anchoredPosition = edgeOffset;
        }
        if (contentRoot) AnchorContent(contentRoot);
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VEŘEJNÉ API
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Veřejné API

    // Znovu sestaví seznam týmů, vytvoří/aktualizuje řádky a přepočítá výšku panelu.
    public void TryRebuild()
    {
        if (TeamManager.Instance == null || !contentRoot) return;

        var desired = new List<int>();
        foreach (var kv in TeamManager.Instance.GetAll())
        {
            int id = kv.Key;
            var data = kv.Value;
            if (!showOnlyTeamsWithNests || (data.nests != null && data.nests.Count > 0))
                desired.Add(id);
        }
        desired.Sort();

        foreach (int id in desired)
        {
            int food = TeamManager.Instance.GetTeamFoodCount(id);
            int ants = TeamManager.Instance.GetTeamAntCount(id);

            TeamScoreboardRow row;
            if (!rows.TryGetValue(id, out row) || row == null)
            {
                row = Instantiate(rowPrefab, contentRoot);
                rows[id] = row;

                if (headerRoot)
                    row.transform.SetSiblingIndex(headerRoot.GetSiblingIndex() + 1);
            }

            row.Set(
                TeamManager.Instance.GetTeamColor(id),
                TeamManager.Instance.GetTeamName(id),
                ants,
                food
            );
        }

        var toRemove = new List<int>();
        foreach (var kv in rows)
            if (!desired.Contains(kv.Key)) toRemove.Add(kv.Key);

        foreach (var id in toRemove)
        {
            if (rows[id]) Destroy(rows[id].gameObject);
            rows.Remove(id);
        }

        if (autoSize) RecalculateHeight();
    }

    // Kontextové menu v Inspectoru: okamžitě vynutí rebuild tabulky.
    [ContextMenu("Force Rebuild Now")]
    public void ForceRebuildNow() => TryRebuild();

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // POMOCNÉ FUNKCE (LAYOUT / UI)
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Helpers

    // Spočítá preferovanou výšku obsahu a upraví výšku panelu s ohledem na limity.
    void RecalculateHeight()
    {
        if (!panel || !contentRoot) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        float preferred = LayoutUtility.GetPreferredHeight(contentRoot);
        float target = preferred + extraVerticalPadding;

        var parentRT = panel.parent as RectTransform;
        if (parentRT)
        {
            float maxH = parentRT.rect.height * maxHeightRatio;
            if (target > maxH) target = maxH;
        }

        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, target);
    }

    // Zajistí existenci kořene obsahu a nastaví mu layout/fit parametry.
    void EnsureContentRoot()
    {
        if (!contentRoot)
        {
            var rt = new GameObject("ScoreboardContent",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            contentRoot = rt;
        }

        AnchorContent(contentRoot);

        var vlg = contentRoot.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.spacing = headerSpacing;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(8, 8, 8, 8);

        var csf = contentRoot.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // Vytvoří a nakonfiguruje hlavičku tabulky (Team / Ants / Food).
    void EnsureHeader()
    {
        if (!contentRoot) return;

        var existing = contentRoot.Find("Header");
        if (existing)
        {
            headerRoot = existing as RectTransform;
            return;
        }

        headerRoot = new GameObject("Header", typeof(RectTransform)).GetComponent<RectTransform>();
        headerRoot.SetParent(contentRoot, false);
        headerRoot.SetSiblingIndex(0);

        var hlg = headerRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = headerSpacing;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(headerRoot, false);
        var spacerLe = spacer.GetComponent<LayoutElement>();
        spacerLe.minWidth = spacerLe.preferredWidth = 18f;

        var teamGO = new GameObject("Team", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        teamGO.transform.SetParent(headerRoot, false);
        var teamTMP = teamGO.GetComponent<TextMeshProUGUI>();
        teamTMP.text = "Team";
        teamTMP.fontSize = headerFont;
        teamTMP.alpha = 0.85f;
        teamTMP.enableWordWrapping = false;
        teamGO.GetComponent<LayoutElement>().flexibleWidth = 1;

        var antsGO = new GameObject("Ants", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        antsGO.transform.SetParent(headerRoot, false);
        var antsTMP = antsGO.GetComponent<TextMeshProUGUI>();
        antsTMP.text = "Ants";
        antsTMP.fontSize = headerFont;
        antsTMP.alpha = 0.85f;
        antsTMP.alignment = TextAlignmentOptions.Right;
        var antsLE = antsGO.GetComponent<LayoutElement>();
        antsLE.minWidth = antsLE.preferredWidth = headerStatMinWidth;

        var foodGO = new GameObject("Food", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        foodGO.transform.SetParent(headerRoot, false);
        var foodTMP = foodGO.GetComponent<TextMeshProUGUI>();
        foodTMP.text = "Food";
        foodTMP.fontSize = headerFont;
        foodTMP.alpha = 0.85f;
        foodTMP.alignment = TextAlignmentOptions.Right;
        var foodLE = foodGO.GetComponent<LayoutElement>();
        foodLE.minWidth = foodLE.preferredWidth = headerStatMinWidth;

        var leRow = headerRoot.gameObject.AddComponent<LayoutElement>();
        leRow.preferredHeight = 22f;
    }

    // Nastaví ukotvení a rozměry tak, aby se obsah lepil na horní okraj a roztahoval na šířku.
    static void AnchorContent(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 0f);
        rt.localScale = Vector3.one;
    }

    #endregion
}
