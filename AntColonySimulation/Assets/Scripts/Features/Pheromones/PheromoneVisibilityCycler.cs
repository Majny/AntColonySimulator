using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PheromoneVisibilityCycler : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Konfigurace

    [Header("Hotkey")]
    public Key hotkey = Key.F;             // Klávesa pro přepínání viditelnosti týmů (v runtime přepíše na F)

    [Header("Timing")]
    public float rescanEvery = 0.5f;       // Jak často přestavět pořadí týmů, spíše pro možné rozšíření o další módy

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VNITŘNÍ STAV
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Vnitřní stav

    readonly List<int> teamOrder = new(); // Seřazený seznam týmů s dostupnými poli
    int currentIndex = -1;                // Index aktuálně zobrazeného týmu (-1 = žádný)
    float rescanTimer;                    // Akumulátor času pro periodický rescan

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity lifecycle
    

    // Připraví výchozí stav a naplánuje první sken týmů po malé prodlevě.
    void Start()
    {
        currentIndex = -1;
        InitialScanAndApply();
    }

    // Naslouchá klávese pro přepnutí a periodicky znovu sestavuje seznam týmů.
    void Update()
    {
        var kbd = Keyboard.current;
        if (kbd != null)
        {
            var key = kbd[hotkey];
            if (key != null && key.wasPressedThisFrame)
                Advance();
        }

        // V momentalní verzi zbytečné, rozšíření pro přidávání/odebíraní týmu za Runtimu
        rescanTimer += Time.deltaTime;
        if (rescanTimer >= rescanEvery)
        {
            rescanTimer = 0f;
            int before = teamOrder.Count;
            BuildOrder();
            if (teamOrder.Count == 0) return;

            if (currentIndex >= teamOrder.Count) currentIndex = -1;
            if (teamOrder.Count != before) ApplyVisibility();
        }
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // CYKLOVÁNÍ VIDITELNOSTI
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Cyklení

    // Provede první sken týmů a aplikuje výsledek na viditelnost polí.
    void InitialScanAndApply()
    {
        BuildOrder();
        ApplyVisibility();
    }

    // Posune výběr na další tým a aplikuje viditelnost.
    void Advance()
    {
        int count = BuildOrder();
        if (count == 0) return;

        if (currentIndex == -1) currentIndex = 0;
        else
        {
            currentIndex++;
            if (currentIndex >= count) currentIndex = -1;
        }
        ApplyVisibility();
    }

    // Znovu sestaví a seřadí seznam týmů, které mají alespoň jedno feromonové pole.
    int BuildOrder()
    {
        teamOrder.Clear();
        if (TeamManager.Instance == null) return 0;

        foreach (var kv in TeamManager.Instance.GetAll())
        {
            int teamId = kv.Key;
            var data = kv.Value;
            if (data.homeField || data.foodField)
                teamOrder.Add(teamId);
        }
        teamOrder.Sort();
        return teamOrder.Count;
    }

    // Zapne viditelnost feromonových polí jen aktivnímu týmu.
    void ApplyVisibility()
    {
        if (TeamManager.Instance == null) return;

        bool showNone = (currentIndex == -1);
        int activeTeamId = (!showNone && teamOrder.Count > 0) ? teamOrder[Mathf.Clamp(currentIndex, 0, teamOrder.Count - 1)] : -1;

        foreach (var kv in TeamManager.Instance.GetAll())
        {
            int id = kv.Key;
            var data = kv.Value;

            bool visible = !showNone && (id == activeTeamId);

            if (data.homeField) data.homeField.SetVisible(visible);
            if (data.foodField) data.foodField.SetVisible(visible);
        }
    }

    #endregion
}
