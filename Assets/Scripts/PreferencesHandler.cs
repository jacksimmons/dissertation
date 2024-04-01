using System.Collections.Generic;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PreferencesHandler : SetupBehaviour
{
    [SerializeField]
    private TMP_Dropdown m_weightGoalDropdown;
    [SerializeField]
    private TMP_InputField m_weightInputField;
    [SerializeField]
    private TMP_InputField m_heightInputField;
    [SerializeField]
    private TextMeshProUGUI m_assignedSexText;
    [SerializeField]
    private TextMeshProUGUI m_isPregnantText;
    [SerializeField]
    private TextMeshProUGUI m_needsVitDText;

    [SerializeField]
    private TextMeshProUGUI m_landMeatPrefBtnTxt;
    [SerializeField]
    private TextMeshProUGUI m_seafoodPrefBtnTxt;
    [SerializeField]
    private TextMeshProUGUI m_animalProductsPrefBtnTxt;
    [SerializeField]
    private TextMeshProUGUI m_lactosePrefBtnTxt;

    [SerializeField]
    private GameObject[] m_preferenceCategoryPanels;
    private int m_currentPanelIndex = 0;

    private List<Food> m_allFoods = new();
    [SerializeField]
    private GameObject m_enabledFoodsContent;
    [SerializeField]
    private GameObject m_btnPropertyTemplate;
    [SerializeField]
    private TMP_Text m_enabledFoodsCountTxt;

    [SerializeField]
    private GameObject m_missingNutrientsContent;


    private void Awake()
    {
        // For each UI element, display existing user preferences.
        SetDietPreferenceUI();
        SetBodyPreferenceUI();
        SetMissingDataUI();

        m_weightInputField.onEndEdit.AddListener((string val) => OnFloatInputChanged(ref Preferences.Instance.weightKg, val));
        m_heightInputField.onEndEdit.AddListener((string val) => OnFloatInputChanged(ref Preferences.Instance.heightCm, val));

        SetEnabledFoodsUI();
    }


    /// <summary>
    /// Handles navigation between menus, specifically for the Preferences menu.
    /// </summary>
    /// <param name="right">`true` if navigating right, `false` if navigating left.</param>
    public void OnPreferencesNavBtnPressed(bool right)
    {
        UITools.OnNavBtnPressed(right, m_preferenceCategoryPanels, ref m_currentPanelIndex);
    }


    /// <summary>
    /// Set the labels corresponding to body preferences to the values saved in preferences.
    /// </summary>
    private void SetBodyPreferenceUI()
    {
        m_assignedSexText.text = "Assigned sex: " + (Preferences.Instance.isMale ? "Male" : "Female");
        m_isPregnantText.text = "Pregnancy: " + (Preferences.Instance.isPregnant ? "True" : "False");
        m_needsVitDText.text = "Vit D: " + (Preferences.Instance.needsVitD ? "True" : "False");
        m_weightInputField.text = Preferences.Instance.weightKg.ToString();
        m_heightInputField.text = Preferences.Instance.heightCm.ToString();
    }


    //
    // Body button listener methods
    //
    public void OnAssignedSexBtnPressed()
    {
        OnBoolInputChanged(ref Preferences.Instance.isMale);

        // Ensure conflicting parameters are set to correct values
        if (Preferences.Instance.isMale) OnBoolInputChanged(ref Preferences.Instance.isPregnant, false);

        SetBodyPreferenceUI();
    }


    public void OnIsPregnantBtnPressed()
    {
        // Changing this parameter causes conflicts; don't change
        if (Preferences.Instance.isMale) return;

        OnBoolInputChanged(ref Preferences.Instance.isPregnant);

        SetBodyPreferenceUI();
    }


    public void OnToggleVitDBtnPressed()
    {
        OnBoolInputChanged(ref Preferences.Instance.needsVitD);
        SetBodyPreferenceUI();
    }


    /// <summary>
    /// Calculates algorithm constraints based on your preferences (male/female, weight, height, etc.)
    /// </summary>
    public void CalculateConstraints()
    {
        Preferences.Instance.CalculateDefaultConstraints();
        Saving.SavePreferences();
    }


    /// <summary>
    /// Update the button labels from the Saved preferences so that the enabled
    /// settings are labelled "x" and the disabled settings "".
    /// </summary>
    private void SetDietPreferenceUI()
    {
        Preferences p = Preferences.Instance;

        m_landMeatPrefBtnTxt.text = p.eatsLandMeat ? "x" : "";
        m_seafoodPrefBtnTxt.text = p.eatsSeafood ? "x" : "";
        m_animalProductsPrefBtnTxt.text = p.eatsAnimalProduce ? "x" : "";
        m_lactosePrefBtnTxt.text = p.eatsLactose ? "x" : "";
    }


    /// <summary>
    /// Method for handling a diet preference change.
    /// </summary>
    /// <param name="pref">A reference to the diet preference to flip.</param>
    private void OnToggleFoodPreference(ref bool pref)
    {
        OnBoolInputChanged(ref pref);
        SetDietPreferenceUI();
    }


    //
    // Diet button listener methods
    //
    public void OnToggleMeatPreference()
    {
        OnToggleFoodPreference(ref Preferences.Instance.eatsLandMeat);
    }


    public void OnToggleSeafoodPreference()
    {
        OnToggleFoodPreference(ref Preferences.Instance.eatsSeafood);
    }


    public void OnToggleAnimalProducePreference()
    {
        OnToggleFoodPreference(ref Preferences.Instance.eatsAnimalProduce);
    }


    public void OnToggleLactosePreference()
    {
        OnToggleFoodPreference(ref Preferences.Instance.eatsLactose);
    }


    public void SetEnabledFoodsUI()
    {
        // Clear all existing buttons
        DatasetReader dr = new(Preferences.Instance);
        m_allFoods = dr.ProcessFoods();
        m_enabledFoodsCountTxt.text = $"Enabled foods: {m_allFoods.Count}/{DatasetReader.FOOD_ROWS}";

        // Remove any existing buttons
        UITools.DestroyAllChildren(m_enabledFoodsContent.transform);

        // Add disabled food buttons at the top
        for (int _i = 0; _i < Preferences.Instance.bannedFoodKeys.Count; _i++)
        {
            GameObject go = Instantiate(m_btnPropertyTemplate, m_enabledFoodsContent.transform);
            string key = Preferences.Instance.bannedFoodKeys[_i];

            TMP_Text tmpText = go.GetComponentInChildren<TMP_Text>(); // Depth-first search, so it will find OnOffBtn.TMP_Text
            tmpText.text = "";

            go.GetComponentInChildren<Button>().onClick.AddListener(() => OnFoodEnableToggle(key, tmpText));

            go.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = key;
        }

        // Add enabled food buttons underneath
        for (int _i = 0; _i < m_allFoods.Count; _i++)
        {
            GameObject go;
            Food food;

            food = m_allFoods[_i];
            go = Instantiate(m_btnPropertyTemplate, m_enabledFoodsContent.transform);

            TMP_Text tmpText = go.GetComponentInChildren<TMP_Text>(); // Depth-first search, so it will find OnOffBtn.TMP_Text
            tmpText.text = "x";

            go.GetComponentInChildren<Button>().onClick.AddListener(() => OnFoodEnableToggle(food.CompositeKey, tmpText));

            go.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = food.Name;
        }
    }


    private void OnFoodEnableToggle(string foodKey, TMP_Text tmpText)
    {
        // If food disabled, enable it.
        if (Preferences.Instance.bannedFoodKeys.Contains(foodKey))
        {
            Preferences.Instance.bannedFoodKeys.Remove(foodKey);
            tmpText.text = "x";
        }
        else
        {
            Preferences.Instance.bannedFoodKeys.Add(foodKey);
            tmpText.text = "";
        }
        Saving.SavePreferences();
    }


    private void SetMissingDataUI()
    {
        // Remove any existing buttons
        UITools.DestroyAllChildren(m_missingNutrientsContent.transform);

        // Readd all buttons
        for (int _i = 0; _i < Nutrient.Count; _i++)
        {
            ENutrient nutrient = (ENutrient)_i;

            GameObject go = Instantiate(m_btnPropertyTemplate, m_missingNutrientsContent.transform);

            TMP_Text tmpText = go.GetComponentInChildren<TMP_Text>(); // GetComponentInChildren is DFS
            tmpText.text = Preferences.Instance.acceptMissingNutrientValue[_i] ? "x" : "";

            go.GetComponentInChildren<Button>().onClick.AddListener(() => OnMissingNutrientToggle(nutrient, tmpText));

            go.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = nutrient.ToString();
        }
    }


    private void OnMissingNutrientToggle(ENutrient nutrient, TMP_Text tmpText)
    {
        OnBoolInputChanged(ref Preferences.Instance.acceptMissingNutrientValue[(int)nutrient]);
        tmpText.text = Preferences.Instance.acceptMissingNutrientValue[(int)nutrient] ? "x" : "";
    }
}