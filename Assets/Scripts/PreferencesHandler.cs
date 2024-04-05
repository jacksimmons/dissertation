using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// A list of foods that are specifically enabled by the user. By default, all are specifically enabled, but this list
    /// only contains *permitted* enabled foods. A permitted food is one that is permitted by dietary preferences (e.g. vegan).
    /// </summary>
    private List<Food> m_enabledFoods = new();
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
        m_enabledFoods = dr.ProcessFoods();
        m_enabledFoodsCountTxt.text = $"Enabled foods: {m_enabledFoods.Count}/{DatasetReader.FOOD_ROWS}";

        // Remove any existing buttons
        UITools.DestroyAllChildren(m_enabledFoodsContent.transform);

        void SetupCostInputField(GameObject go, string key)
        {
            TMP_InputField inp = go.transform.Find("FloatInputProperty").GetComponentInChildren<TMP_InputField>();
            Preferences.CustomFoodSettings cfs;
            try
            {
                cfs = Preferences.Instance.TryGetSettings(key);
            }
            catch (KeyNotFoundException)
            {
                cfs = new() { Key = key, Banned = false, Cost = 0 };
                Preferences.Instance.customFoodSettings.Add(cfs);
            }

            int index = Preferences.Instance.customFoodSettings.IndexOf(cfs);
            inp.text = $"{cfs.Cost}";
            inp.onEndEdit.AddListener((string text) => OnFloatInputChanged(ref Preferences.Instance.customFoodSettings[index].Cost, text));
        }

        // Add custom settings foods at the top
        for (int _i = 0; _i < Preferences.Instance.customFoodSettings.Count; _i++)
        {
            GameObject go = Instantiate(m_btnPropertyTemplate, m_enabledFoodsContent.transform);
            var cfs = Preferences.Instance.customFoodSettings[_i];
            string key = cfs.Key;

            TMP_Text tmpText = go.GetComponentInChildren<TMP_Text>(); // Depth-first search, so it will find OnOffBtn.TMP_Text
            tmpText.text = cfs.Banned ? "" : "x";

            go.GetComponentInChildren<Button>().onClick.AddListener(() => OnFoodEnableToggle(key, tmpText));

            go.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = key;

            SetupCostInputField(go, key);
        }

        // Add other food buttons underneath
        for (int _i = 0; _i < m_enabledFoods.Count; _i++)
        {
            Food food = m_enabledFoods[_i];

            try
            {
                // Skip this food if it has a setting (it has already been added)
                Preferences.Instance.TryGetSettings(food.CompositeKey);
                continue;
            }

            catch (KeyNotFoundException) { }
            
            GameObject go;
            go = Instantiate(m_btnPropertyTemplate, m_enabledFoodsContent.transform);

            TMP_Text tmpText = go.GetComponentInChildren<TMP_Text>(); // Depth-first search, so it will find OnOffBtn.TMP_Text
            tmpText.text = "x";

            go.GetComponentInChildren<Button>().onClick.AddListener(() => OnFoodEnableToggle(food.CompositeKey, tmpText));

            go.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = food.Name;

            SetupCostInputField(go, food.CompositeKey);
        }
    }


    private void OnFoodEnableToggle(string foodKey, TMP_Text tmpText)
    {
        Preferences.Instance.ToggleFoodBanned(foodKey);

        // Update the UI for this food
        tmpText.text = Preferences.Instance.IsFoodBanned(foodKey) ? "" : "x";

        Saving.SavePreferences();
    }


    /// <summary>
    /// Listener called when Enable All Foods or Disable All Foods buttons are pressed.
    /// </summary>
    public void OnEnableOrDisableAllBtnPressed(bool enableElseDisable)
    {
        // Construct all permitted food keys
        List<string> permittedKeys = Preferences.Instance.customFoodSettings.Select(x => x.Key).ToList();
        permittedKeys.AddRange(m_enabledFoods.Select(x => x.CompositeKey));

        foreach (string key in permittedKeys)
        {
            if (Preferences.Instance.IsFoodBanned(key))
            {
                // If enabling all, need to unban (toggle) the food
                if (enableElseDisable)
                {
                    Preferences.Instance.ToggleFoodBanned(key);
                }

                // If disabling all, leave the food banned.
                continue;
            }
            else
            {
                // If disabling all, need to ban (toggle) the food
                if (!enableElseDisable)
                {
                    Preferences.Instance.ToggleFoodBanned(key);
                }

                // If enabling all, leave the food unbanned.
                continue;
            }
        }
    }


    private void SetMissingDataUI()
    {
        // Remove any existing buttons
        UITools.DestroyAllChildren(m_missingNutrientsContent.transform);

        // Readd all buttons
        for (int _i = 0; _i < Constraint.Count; _i++)
        {
            EConstraintType nutrient = (EConstraintType)_i;

            GameObject go = Instantiate(m_btnPropertyTemplate, m_missingNutrientsContent.transform);

            TMP_Text tmpText = go.GetComponentInChildren<TMP_Text>(); // GetComponentInChildren is DFS
            tmpText.text = Preferences.Instance.acceptMissingNutrientValue[_i] ? "x" : "";

            go.GetComponentInChildren<Button>().onClick.AddListener(() => OnMissingNutrientToggle(nutrient, tmpText));

            go.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = nutrient.ToString();
        }
    }


    private void OnMissingNutrientToggle(EConstraintType nutrient, TMP_Text tmpText)
    {
        OnBoolInputChanged(ref Preferences.Instance.acceptMissingNutrientValue[(int)nutrient]);
        tmpText.text = Preferences.Instance.acceptMissingNutrientValue[(int)nutrient] ? "x" : "";
    }
}