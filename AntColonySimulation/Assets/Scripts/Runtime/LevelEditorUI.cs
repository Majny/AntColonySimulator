using UnityEngine;

public class LevelEditorUI : MonoBehaviour
{
    public LevelEditor editor;

    public void OnFoodBtn() => editor.SelectFood();
    public void OnNestBtn() => editor.SelectNest();
    public void OnDirtBtn() => editor.SelectDirt();
    public void OnStartBtn() => editor.StartSimulation();
}