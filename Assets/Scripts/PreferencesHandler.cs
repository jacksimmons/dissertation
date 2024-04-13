using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Preferences;


public class PreferencesHandler : SetupBehaviour
{
    [SerializeField]
    private TMP_InputField m_dailyExerciseKcalInput;
    [SerializeField]
    private TMP_Text m_gainElseLoseWeightBtnTxt;
    [SerializeField]
    private TMP_InputField m_dailyLoseOrGainKcalInput;

    [SerializeField]
    private TMP_InputField m_ageInputField;
    [SerializeField]
    private TMP_InputField m_weightInputField;
    [SerializeField]
    private TMP_InputField m_heightInputField;
    [SerializeField]
    private TMP_Text m_assignedSexText;
    [SerializeField]
    private TMP_Text m_isPregnantText;
    [SerializeField]
    private TMP_Text m_needsVitDText;

    [SerializeField]
    private TMP_Text m_landMeatPrefBtnTxt;
    [SerializeField]
    private TMP_Text m_seafoodPrefBtnTxt;
    [SerializeField]
    private TMP_Text m_animalProductsPrefBtnTxt;
    [SerializeField]
    private TMP_Text m_lactosePrefBtnTxt;

    [SerializeField]
    private GameObject[] m_preferenceCategoryPanels;
    private int m_currentPanelIndex = 0;

    /// <summary>
    /// A list of foods that are specifically enabled by the user. By default, all are specifically enabled, but this list
    /// only contains *permitted* enabled foods. A permitted food is one that is permitted by dietary preferences (e.g. vegan).
    /// </summary>
    private List<Food> m_enabledFoods = new();
    [SerializeField]
    private GameObject m_foodSettingTemplate;
    [SerializeField]
    private GameObject m_enabledFoodsContent;
    [SerializeField]
    private TMP_Text m_enabledFoodsCountTxt;

    [SerializeField]
    private GameObject m_missingDataSettingTemplate;
    [SerializeField]
    private GameObject m_missingNutrientsContent;


    private void Awake()
    {
        // For each UI element, display existing user preferences.
        SetDietPreferenceUI();
        SetBodyPreferenceUI();
        SetMissingDataUI();

        m_dailyExerciseKcalInput.onEndEdit.AddListener((string val) => OnFloatInputChanged(ref Instance.dailyExerciseKcal, val));
        m_dailyLoseOrGainKcalInput.onEndEdit.AddListener((string val) => OnFloatInputChanged(ref Instance.dailyLoseOrGainKcal, val));
        m_ageInputField.onEndEdit.AddListener((string val) => OnIntInputChanged(ref Instance.ageYears, val));
        m_weightInputField.onEndEdit.AddListener((string val) => OnFloatInputChanged(ref Instance.weightKg, val));
        m_heightInputField.onEndEdit.AddListener((string val) => OnFloatInputChanged(ref Instance.heightCm, val));

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
    /// Calculates algorithm constraints based on your preferences (male/female, weight, height, etc.)
    /// </summary>
    public void CalculateConstraints()
    {
        Instance.CalculateDefaultConstraints();
        Saving.SavePreferences();
    }


    /// <summary>
    /// Update the button labels from the Saved preferences so that the enabled
    /// settings are labelled "x" and the disabled settings "".
    /// </summary>
    private void SetDietPreferenceUI()
    {
        m_dailyExerciseKcalInput.text = $"{Instance.dailyExerciseKcal}";
        m_gainElseLoseWeightBtnTxt.text = Instance.gainElseLoseWeight ? "x" : "";
        m_dailyLoseOrGainKcalInput.text = $"{Instance.dailyLoseOrGainKcal}";

        m_landMeatPrefBtnTxt.text = Instance.eatsLandMeat ? "x" : "";
        m_seafoodPrefBtnTxt.text = Instance.eatsSeafood ? "x" : "";
        m_animalProductsPrefBtnTxt.text = Instance.eatsAnimalProduce ? "x" : "";
        m_lactosePrefBtnTxt.text = Instance.eatsLactose ? "x" : "";
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
        OnToggleFoodPreference(ref Instance.eatsLandMeat);
    }


    public void OnToggleSeafoodPreference()
    {
        OnToggleFoodPreference(ref Instance.eatsSeafood);
    }


    public void OnToggleAnimalProducePreference()
    {
        OnToggleFoodPreference(ref Instance.eatsAnimalProduce);
    }


    public void OnToggleLactosePreference()
    {
        OnToggleFoodPreference(ref Instance.eatsLactose);
    }


    public void OnToggleCalorieGoalPreference()
    {
        OnToggleFoodPreference(ref Instance.gainElseLoseWeight);
    }


    /// <summary>
    /// Set the labels corresponding to body preferences to the values saved in preferences.
    /// </summary>
    private void SetBodyPreferenceUI()
    {
        m_assignedSexText.text = Instance.maleElseFemale ? "Male" : "Female";
        m_isPregnantText.text = Instance.isPregnant ? "True" : "False";
        m_needsVitDText.text = Instance.needsVitD ? "True" : "False";

        m_ageInputField.text = Instance.ageYears.ToString();
        m_weightInputField.text = Instance.weightKg.ToString();
        m_heightInputField.text = Instance.heightCm.ToString();
    }


    //
    // Body button listener methods
    //
    public void OnAssignedSexBtnPressed()
    {
        OnBoolInputChanged(ref Instance.maleElseFemale);

        // Ensure conflicting parameters are set to correct values
        if (Instance.maleElseFemale) OnBoolInputChanged(ref Instance.isPregnant, false);

        SetBodyPreferenceUI();
    }


    public void OnIsPregnantBtnPressed()
    {
        // Changing this parameter causes conflicts; don't change
        if (Instance.maleElseFemale) return;

        OnBoolInputChanged(ref Instance.isPregnant);

        SetBodyPreferenceUI();
    }


    public void OnToggleVitDBtnPressed()
    {
        OnBoolInputChanged(ref Instance.needsVitD);
        SetBodyPreferenceUI();
    }


    public void SetEnabledFoodsUI()
    {
        // Clear all existing buttons
        DatasetReader dr = new(Instance);
        m_enabledFoods = dr.ProcessFoods();
        m_enabledFoodsCountTxt.text = $"Enabled foods: {m_enabledFoods.Count}/{DatasetReader.FOOD_ROWS}";

        // Remove any existing buttons
        UITools.DestroyAllChildren(m_enabledFoodsContent.transform);

        void SetupFoodSettings(string key)
        {
            GameObject go = Instantiate(m_foodSettingTemplate, m_enabledFoodsContent.transform);

            TMP_InputField inp = go.transform.Find("FloatInputProperty").GetComponentInChildren<TMP_InputField>();
            CustomFoodSettings cfs;
            try
            {
                cfs = Instance.TryGetSettings(key);
                inp.text = $"{cfs.Cost}";
            }
            catch (KeyNotFoundException)
            {
                cfs = new() { Key = key, Banned = false, Cost = 0 };
                Instance.customFoodSettings.Add(cfs);
            }

            TMP_Text tmpText = go.GetComponentInChildren<TMP_Text>(); // Depth-first search, so it will find OnOffBtn.TMP_Text
            tmpText.text = cfs.Banned ? "" : "x";

            go.GetComponentInChildren<Button>().onClick.AddListener(() => OnFoodEnableToggle(key, tmpText));

            go.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = key;

            int index = Instance.customFoodSettings.IndexOf(cfs);
            inp.text = $"{cfs.Cost}";
            inp.onEndEdit.AddListener((string text) => OnFloatInputChanged(ref Instance.customFoodSettings[index].Cost, text));
        }

        // Add all custom food settings
        for (int i = 0; i < Instance.customFoodSettings.Count; i++)
        {
            string key = Instance.customFoodSettings[i].Key;
            SetupFoodSettings(key);
        }

        // Add all enabled foods, if not already added
        for (int i = 0; i < m_enabledFoods.Count; i++)
        {
            string key = m_enabledFoods[i].CompositeKey;
            try
            {
                Instance.TryGetSettings(key);

                // Above succeeds => Custom food setting exists for this food,
                // therefore avoid adding duplicate food property.
                return;
            }
            catch (KeyNotFoundException)
            {
                SetupFoodSettings(key);
            }
        }
    }


    public void OnAllFoodsToggle(bool enableElseDisable)
    {
        if (!enableElseDisable)
        {
            foreach (Food food in m_enabledFoods)
            {
                bool isBanned = Instance.IsFoodBanned(food.CompositeKey);
                if (!isBanned)
                    Instance.ToggleFoodBanned(food.CompositeKey);
            }
        }
        else
        {
            foreach (CustomFoodSettings settings in Instance.customFoodSettings)
            {
                settings.Banned = false;
            }
        }

        SetEnabledFoodsUI();
        Saving.SavePreferences();
    }


    private void OnFoodEnableToggle(string foodKey, TMP_Text tmpText)
    {
        Instance.ToggleFoodBanned(foodKey);

        // Update the UI for this food
        tmpText.text = Instance.IsFoodBanned(foodKey) ? "" : "x";

        Saving.SavePreferences();
    }


    private void SetMissingDataUI()
    {
        // Remove any existing buttons
        UITools.DestroyAllChildren(m_missingNutrientsContent.transform);

        // Readd all buttons
        for (int _i = 0; _i < Constraint.Count; _i++)
        {
            EConstraintType nutrient = (EConstraintType)_i;

            GameObject go = Instantiate(m_missingDataSettingTemplate, m_missingNutrientsContent.transform);

            TMP_Text tmpText = go.GetComponentInChildren<TMP_Text>(); // GetComponentInChildren is DFS
            tmpText.text = Instance.acceptMissingNutrientValue[_i] ? "x" : "";

            go.GetComponentInChildren<Button>().onClick.AddListener(() => OnMissingNutrientToggle(nutrient, tmpText));

            go.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = nutrient.ToString();
        }
    }


    private void OnMissingNutrientToggle(EConstraintType nutrient, TMP_Text tmpText)
    {
        OnBoolInputChanged(ref Instance.acceptMissingNutrientValue[(int)nutrient]);

        // Set the corresponding constraint type to Null so that it isn't considered (if toggled on).
        if (Instance.acceptMissingNutrientValue[(int)nutrient])
            Instance.constraints[(int)nutrient].Type = typeof(NullConstraint).FullName;

        tmpText.text = Instance.acceptMissingNutrientValue[(int)nutrient] ? "x" : "";
    }
}