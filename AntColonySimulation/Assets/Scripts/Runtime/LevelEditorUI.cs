using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class LevelEditorUI : MonoBehaviour
{
    public LevelEditor editor;

    [Header("UI refs")]
    public GameObject editorMenuPanel;
    public Slider foodAmountSlider;
    public TMP_InputField foodAmountInput;

    bool isUpdatingUI;

    void Start()
    {
        if (foodAmountSlider)
        {
            foodAmountSlider.wholeNumbers = true;
            foodAmountSlider.value = editor.foodAmount;
            foodAmountSlider.onValueChanged.AddListener(v => OnSliderChanged(Mathf.RoundToInt(v)));
        }
        if (foodAmountInput)
        {
            foodAmountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            foodAmountInput.text = editor.foodAmount.ToString();
            foodAmountInput.onEndEdit.AddListener(OnInputEndEdit);
            foodAmountInput.onSubmit.AddListener(OnInputEndEdit);
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame && editorMenuPanel)
            editorMenuPanel.SetActive(!editorMenuPanel.activeSelf);
    }

    public void OnFoodBtn() => editor.SelectFood();
    public void OnNestBtn() => editor.SelectNest();
    public void OnDirtBtn() => editor.SelectDirt();
    public void OnStartBtn() => editor.StartSimulation();

    void OnSliderChanged(int val)
    {
        if (isUpdatingUI) return;
        isUpdatingUI = true;
        editor.SetFoodAmount(val);
        if (foodAmountInput) foodAmountInput.text = val.ToString();
        isUpdatingUI = false;
    }

    void OnInputEndEdit(string text)
    {
        if (isUpdatingUI) return;
        if (!int.TryParse(text, out var val)) { foodAmountInput.text = editor.foodAmount.ToString(); return; }

        if (foodAmountSlider)
            val = Mathf.Clamp(val, (int)foodAmountSlider.minValue, (int)foodAmountSlider.maxValue);

        isUpdatingUI = true;
        editor.SetFoodAmount(val);
        if (foodAmountSlider) foodAmountSlider.value = val;
        if (foodAmountInput)  foodAmountInput.text  = val.ToString();
        isUpdatingUI = false;
    }
}
