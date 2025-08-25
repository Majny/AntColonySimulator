using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamScoreboardRow : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // ODKAZY NA UI PRVKY
    // ─────────────────────────────────────────────────────────────────────────────
    #region — UI refs

    public Image colorSwatch;   // Barevný štítek týmu
    public TMP_Text nameText;   // Název týmu
    public TMP_Text antsText;   // Počet mravenců
    public TMP_Text foodText;   // Nasbírané jídlo

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VEŘEJNÉ API
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Veřejné API

    // Naplní řádek skóre daty (barva, název týmu, počty).
    public void Set(Color c, string name, int ants, int food)
    {
        if (colorSwatch) colorSwatch.color = c;
        if (nameText)    nameText.text    = name;
        if (antsText)    antsText.text    = ants.ToString();
        if (foodText)    foodText.text    = food.ToString();
    }

    #endregion
}