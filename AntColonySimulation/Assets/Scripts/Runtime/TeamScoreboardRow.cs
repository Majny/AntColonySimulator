using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamScoreboardRow : MonoBehaviour
{
    public Image colorSwatch;
    public TMP_Text nameText;
    public TMP_Text antsText;
    public TMP_Text foodText;

    public void Set(Color c, string name, int ants, int food)
    {
        if (colorSwatch) colorSwatch.color = c;
        if (nameText) nameText.text = name;
        if (antsText) antsText.text = ants.ToString();
        if (foodText) foodText.text = food.ToString();
    }
}