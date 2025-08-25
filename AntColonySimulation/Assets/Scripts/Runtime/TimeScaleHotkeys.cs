using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[DisallowMultipleComponent]
public class TimeScaleHotkeys : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Konfigurace z Inspectoru

    [Header("Hotkeys (New Input System)")]
    public Key slowerKey = Key.U;   // -0.5×
    public Key fasterKey = Key.I;   // +0.5×

    [Header("Speed settings")]
    public float step = 0.5f;
    public float min = 0f;
    public float max = 10f;
    public float initial = 1f;
    public bool setInitialOnAwake = true;

    [Header("HUD")]
    public TMP_Text label;
    public bool showSuffixX = true;
    public int decimals = 1;

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity lifecycle

    void Awake()
    {
        // Nastavíme počáteční rychlost
        if (setInitialOnAwake)
            SetScale(initial);

        // Aktualizujeme HUD na startu
        RefreshLabel();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Hotkeys
        if (kb[slowerKey].wasPressedThisFrame) Bump(-step);
        if (kb[fasterKey].wasPressedThisFrame) Bump(+step);
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // OVLÁDÁNÍ RYCHLOSTI
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Ovládání rychlosti

    // Změní timeScale o daný delta krok
    public void Bump(float delta)
    {
        SetScale(Mathf.Clamp(Time.timeScale + delta, min, max));
    }

    // Nastaví konkrétní hodnotu timeScale
    public void SetScale(float s)
    {
        Time.timeScale = Mathf.Clamp(s, min, max);
        RefreshLabel();
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UI / HUD
    // ─────────────────────────────────────────────────────────────────────────────
    #region — HUD

    // Překreslí textový štítek podle aktuálního Time.timeScale
    void RefreshLabel()
    {
        if (!label) return;

        string fmt = (decimals <= 0) ? "0" : "0." + new string('#', decimals);
        string num = Time.timeScale.ToString(fmt, CultureInfo.InvariantCulture);
        label.text = showSuffixX ? (num + "×") : num;
    }

    #endregion
}
