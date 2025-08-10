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

    public TMP_InputField nestAntsInput;

    public Slider dirtRadiusSlider;

    bool isUpdatingUI;

    void Start()
    {
        if (foodAmountSlider)
        {
            foodAmountSlider.wholeNumbers = true;
            foodAmountSlider.value = editor.foodAmount;
            foodAmountSlider.onValueChanged.AddListener(v => OnFoodSliderChanged(Mathf.RoundToInt(v)));
        }
        if (foodAmountInput)
        {
            foodAmountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            foodAmountInput.text = editor.foodAmount.ToString();
            foodAmountInput.onEndEdit.AddListener(OnFoodInputEndEdit);
            foodAmountInput.onSubmit.AddListener(OnFoodInputEndEdit);
        }

        if (nestAntsInput)
        {
            nestAntsInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            nestAntsInput.text = editor.nestInitialAgents.ToString();
            nestAntsInput.onEndEdit.AddListener(OnNestAntsEndEdit);
            nestAntsInput.onSubmit.AddListener(OnNestAntsEndEdit);
        }

        if (dirtRadiusSlider)
        {
            dirtRadiusSlider.minValue = editor.GetDirtRadiusMin();
            dirtRadiusSlider.maxValue = editor.GetDirtRadiusMax();
            dirtRadiusSlider.wholeNumbers = false;
            dirtRadiusSlider.value = editor.GetDirtRadius();
            dirtRadiusSlider.onValueChanged.AddListener(OnDirtRadiusSliderChanged);
        }

        editor.OnDirtRadiusChanged += SyncDirtSliderFromEditor;
    }

    void OnDestroy()
    {
        if (editor != null) editor.OnDirtRadiusChanged -= SyncDirtSliderFromEditor;
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

    void OnFoodSliderChanged(int val)
    {
        if (isUpdatingUI) return;
        isUpdatingUI = true;
        editor.SetFoodAmount(val);
        if (foodAmountInput) foodAmountInput.text = val.ToString();
        isUpdatingUI = false;
    }

    void OnFoodInputEndEdit(string text)
    {
        if (isUpdatingUI) return;
        if (!int.TryParse(text, out var val))
        {
            if (foodAmountInput) foodAmountInput.text = editor.foodAmount.ToString();
            return;
        }

        if (foodAmountSlider)
            val = Mathf.Clamp(val, (int)foodAmountSlider.minValue, (int)foodAmountSlider.maxValue);

        isUpdatingUI = true;
        editor.SetFoodAmount(val);
        if (foodAmountSlider) foodAmountSlider.value = val;
        if (foodAmountInput)  foodAmountInput.text  = val.ToString();
        isUpdatingUI = false;
    }

    void OnNestAntsEndEdit(string text)
    {
        if (isUpdatingUI) return;
        if (!int.TryParse(text, out var val))
        {
            if (nestAntsInput) nestAntsInput.text = editor.nestInitialAgents.ToString();
            return;
        }

        val = Mathf.Max(0, val);

        isUpdatingUI = true;
        editor.SetNestInitialAgents(val);
        if (nestAntsInput) nestAntsInput.text = val.ToString();
        isUpdatingUI = false;
    }

    void OnDirtRadiusSliderChanged(float v)
    {
        if (isUpdatingUI) return;
        isUpdatingUI = true;
        editor.SetDirtRadius(v);
        isUpdatingUI = false;
    }

    void SyncDirtSliderFromEditor(float current)
    {
        if (isUpdatingUI) return;
        isUpdatingUI = true;
        if (dirtRadiusSlider) dirtRadiusSlider.value = current;
        isUpdatingUI = false;
    }
}