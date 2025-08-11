using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PheromoneVisibilityCycler : MonoBehaviour
{
    [Header("Hotkey (forced to F at runtime)")]
    public Key hotkey = Key.F;

    [Header("Timing")]
    public float initialScanDelay = 0.35f;

    public float rescanEvery = 0.5f;

    readonly List<int> teamOrder = new();
    int currentIndex = -1;
    float rescanTimer;

    void OnEnable()
    {
        hotkey = Key.F;
    }

    void Start()
    {
        currentIndex = -1;
        Invoke(nameof(InitialScanAndApply), initialScanDelay);
    }

    void Update()
    {
        var kbd = Keyboard.current;
        if (kbd != null)
        {
            var key = kbd[hotkey];
            if (key != null && key.wasPressedThisFrame)
                Advance();
        }

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

    void InitialScanAndApply()
    {
        BuildOrder();
        ApplyVisibility();
    }

    void Advance()
    {
        int count = BuildOrder();
        if (count == 0) return;

        if (currentIndex == -1)
            currentIndex = 0;
        else
        {
            currentIndex++;
            if (currentIndex >= count) currentIndex = -1;
        }
        ApplyVisibility();
    }

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
}
