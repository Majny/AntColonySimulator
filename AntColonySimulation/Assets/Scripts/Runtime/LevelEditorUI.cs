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

    [Header("Teams UI")]
    public TMP_Dropdown teamDropdown;

    [Header("Game Rules")]
    public Toggle simulationOfLifeToggle;
    public Toggle upgradedAntsToggle;
    public TMP_InputField foodPerNewAntInput;

    bool isUpdatingUI;

    void Start()
    {
        if (teamDropdown && editor != null)
        {
            teamDropdown.ClearOptions();
            var opts = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            for (int i = 0; i < TeamManager.MaxTeams; i++)
                opts.Add(new TMP_Dropdown.OptionData($"{TeamManager.TeamNames[i]} ({i})"));
            teamDropdown.AddOptions(opts);
            teamDropdown.onValueChanged.AddListener(OnTeamDropdownChanged);
            teamDropdown.value = Mathf.Clamp(editor.currentTeamIndex, 0, TeamManager.MaxTeams - 1);
        }

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

        InitGameRulesUI();

        if (editor != null)
            editor.OnDirtRadiusChanged += SyncDirtSliderFromEditor;
    }

    void OnDestroy()
    {
        if (editor != null) editor.OnDirtRadiusChanged -= SyncDirtSliderFromEditor;

        if (simulationOfLifeToggle) simulationOfLifeToggle.onValueChanged.RemoveListener(OnSimulationOfLifeChanged);
        if (upgradedAntsToggle) upgradedAntsToggle.onValueChanged.RemoveListener(OnUpgradedAntsChanged);
        if (foodPerNewAntInput)
        {
            foodPerNewAntInput.onEndEdit.RemoveListener(OnFoodPerNewAntEndEdit);
            foodPerNewAntInput.onSubmit.RemoveListener(OnFoodPerNewAntEndEdit);
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
    public void OnRubberBtn() => editor.SelectRubber();
    public void OnStartBtn() => editor.StartSimulation();

    void OnTeamDropdownChanged(int idx) => editor.SelectTeam(idx);

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
        if (foodAmountInput) foodAmountInput.text = val.ToString();
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

    void InitGameRulesUI()
    {
        var gr = GameRules.Instance;
        bool hasGR = (gr != null);

        if (simulationOfLifeToggle)
        {
            simulationOfLifeToggle.interactable = hasGR;
            if (hasGR) simulationOfLifeToggle.isOn = gr.simulationOfLife;
            simulationOfLifeToggle.onValueChanged.AddListener(OnSimulationOfLifeChanged);
        }

        if (upgradedAntsToggle)
        {
            upgradedAntsToggle.interactable = hasGR;
            if (hasGR) upgradedAntsToggle.isOn = gr.upgradedAnts;
            upgradedAntsToggle.onValueChanged.AddListener(OnUpgradedAntsChanged);
        }

        if (foodPerNewAntInput)
        {
            foodPerNewAntInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            foodPerNewAntInput.interactable = hasGR;
            if (hasGR) foodPerNewAntInput.text = Mathf.Max(1, gr.foodPerNewAnt).ToString();
            foodPerNewAntInput.onEndEdit.AddListener(OnFoodPerNewAntEndEdit);
            foodPerNewAntInput.onSubmit.AddListener(OnFoodPerNewAntEndEdit);
        }
    }

    void OnSimulationOfLifeChanged(bool v)
    {
        var gr = GameRules.Instance;
        if (gr != null) gr.simulationOfLife = v;
    }

    void OnUpgradedAntsChanged(bool v)
    {
        var gr = GameRules.Instance;
        if (gr != null) gr.upgradedAnts = v;
    }

    void OnFoodPerNewAntEndEdit(string text)
    {
        var gr = GameRules.Instance;
        if (gr == null) return;

        if (!int.TryParse(text, out var val) || val < 1)
            val = 1;

        gr.foodPerNewAnt = val;
        if (foodPerNewAntInput) foodPerNewAntInput.text = val.ToString();
    }
    
    public void OnResetBtn()
    {
        if (editor) editor.ResetLevel();
    }
}
