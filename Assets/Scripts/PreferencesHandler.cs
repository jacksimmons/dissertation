// Commented 20/4
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Preferences;


/// <summary>
/// A Unity script for the Preferences menu.
/// </summary>
public sealed class PreferencesHandler : SetupBehaviour
{
    /// <summary>
    /// An array of all the Preferences panels (which can be cycled through with the
    /// arrow buttons).
    /// </summary>
    [SerializeField]
    private GameObject[] m_preferenceCategoryPanels;
    /// <summary>
    /// Index of the current panel in the above array.
    /// </summary>
    private int m_currentPanelIndex = 0;

    // Energy goal UI elements
    [SerializeField]
    private TMP_InputField m_dailyExerciseKcalInput;
    [SerializeField]
    private TMP_Text m_gainElseLoseWeightBtnTxt;
    [SerializeField]
    private TMP_InputField m_dailyLoseOrGainKcalInput;

    // Body data UI elements
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

    // Dietary needs UI elements
    [SerializeField]
    private TMP_Text m_landMeatPrefBtnTxt;
    [SerializeField]
    private TMP_Text m_seafoodPrefBtnTxt;
    [SerializeField]
    private TMP_Text m_animalProductsPrefBtnTxt;
    [SerializeField]
    private TMP_Text m_lactosePrefBtnTxt;

    // Enabled foods UI elements
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

    // Missing data acceptance UI elements
    [SerializeField]
    private GameObject m_missingDataSettingTemplate;
    [SerializeField]
    private GameObject m_missingNutrientsContent;


    private void Awake()
    {
        Init();
    }


    private void Init()
    {
        // For each UI element, display existing user preferences.
        SetDietPreferenceUI();
        SetBodyPreferenceUI();
        SetMissingDataUI();

        // Add listeners to input fields manually (buttons' listeners are added in the Unity scene)
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


    /// <summary>
    /// Sets up the enabled foods UI (where the user can enable/disable foods, and set their costs).
    /// </summary>
    public void SetEnabledFoodsUI()
    {
        m_enabledFoods = new(DatasetReader.Instance.Output);
        m_enabledFoodsCountTxt.text = $"Enabled foods: {m_enabledFoods.Count}/{DatasetReader.FOOD_ROWS}";

        // Remove any existing buttons
        UITools.DestroyAllChildren(m_enabledFoodsContent.transform);

        // Construct an individual food settings UI element
        void SetupFoodSettings(string key)
        {
            GameObject go = Instantiate(m_foodSettingTemplate, m_enabledFoodsContent.transform);

            // Get the cost input field
            TMP_InputField inp = go.transform.Find("FloatInputProperty").GetComponentInChildren<TMP_InputField>();
            CustomFoodSettings cfs;

            // If the food already has a custom settings, display the saved cost.
            // If not, KeyNotFoundException is thrown at TryGetSettings.
            try
            {
                cfs = Instance.TryGetSettings(key);
                inp.text = $"{cfs.Cost}";
            }
            // Setup a new food settings as one doesn't exist for this food yet.
            catch (KeyNotFoundException)
            {
                cfs = new() { Key = key, Banned = false, Cost = 0 };
                Instance.customFoodSettings.Add(cfs);
            }

            // Load the banned label, and set its display value correspondingly
            TMP_Text tmpText = go.GetComponentInChildren<TMP_Text>(); // Depth-first search, so it will find OnOffBtn.TMP_Text
            tmpText.text = cfs.Banned ? "" : "x";

            // Setup the enable/disable button listener
            go.GetComponentInChildren<Button>().onClick.AddListener(() => OnFoodEnableToggle(key, tmpText));

            // Setup the food info label (so user knows what food they are editing settings for)
            go.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = key;

            // Get the index for the cost input field listener...
            int index = Instance.customFoodSettings.IndexOf(cfs);
            inp.text = $"{cfs.Cost}"; // In case a new settings was created, update the text to reflect the settings' cost

            // ... set the cost input field listener by reference to the element at this index.
            inp.onEndEdit.AddListener((string text) => OnFloatInputChanged(ref Instance.customFoodSettings[index].Cost, text));
        }

        // Add all custom food settings
        for (int i = 0; i < Instance.customFoodSettings.Count; i++)
        {
            string key = Instance.customFoodSettings[i].Key;
            SetupFoodSettings(key);
        }

        // All foods without custom food settings must be enabled. So can add the rest by checking which
        // enabled foods do NOT have a custom food settings.
        for (int i = 0; i < m_enabledFoods.Count; i++)
        {
            string key = m_enabledFoods[i].CompositeKey;

            // Get if the enabled food has a custom food settings. If so, add it to the UI.
            try
            {
                Instance.TryGetSettings(key);

                // If this point is reached, then a custom settings exists for this food. Hence we don't
                // need to add it to the UI again.
                continue;
            }
            catch (KeyNotFoundException)
            {
                // If this point is reached, then no custom settings exists for this food. Hence we need
                // to add this one to the UI, as it is not there already.
                SetupFoodSettings(key); // Add it to the UI.
            }
        }
    }


    /// <summary>
    /// Toggle all foods enabled or disabled.
    /// </summary>
    /// <param name="enableElseDisable">`true` if enabling all, `false` if disabling all.</param>
    //public void OnAllFoodsToggle(bool enableElseDisable)
    //{
    //    // If disable all was selected...
    //    if (!enableElseDisable)
    //    {
    //        // ... need to ensure every food has a food settings, and that it is set to disabled.
    //        foreach (Food food in m_enabledFoods)
    //        {
    //            // Ensure this food is disabled, by checking if it is enabled, and if it is, toggling
    //            // its enabled status.
    //            bool isBanned = Instance.IsFoodBanned(food.CompositeKey);
    //            if (!isBanned)
    //                Instance.ToggleFoodBanned(food.CompositeKey);
    //        }
    //    }

    //    // If enable all was selected...
    //    else
    //    {
    //        // ... simply set all custom settings to enabled. All foods without custom settings
    //        // are enabled anyway.
    //        foreach (CustomFoodSettings settings in Instance.customFoodSettings)
    //        {
    //            settings.Banned = false;
    //        }
    //    }

    //    // Update the UI, and save these food settings to disk.
    //    SetEnabledFoodsUI();
    //    Saving.SavePreferences();
    //}


    /// <summary>
    /// [Listener Method]
    /// Called when a user toggles the enabled status of a food through a food setting UI element.
    /// </summary>
    /// <param name="foodKey">The key of the food.</param>
    /// <param name="tmpText">The enabled/disabled display label for the food.</param>
    private void OnFoodEnableToggle(string foodKey, TMP_Text tmpText)
    {
        // Toggle the food's enabled status
        Instance.ToggleFoodBanned(foodKey);

        // Update the UI for this food
        tmpText.text = Instance.IsFoodBanned(foodKey) ? "" : "x";

        // Save
        Saving.SavePreferences();
    }


    /// <summary>
    /// Updates the UI for the missing data allowance panel. Here the user can decide which constraints
    /// can be missing from foods in the dataset, and which can't.
    /// </summary>
    private void SetMissingDataUI()
    {
        // Remove any existing buttons
        UITools.DestroyAllChildren(m_missingNutrientsContent.transform);

        // Readd all buttons
        for (int _i = 0; _i < Constraint.Count; _i++)
        {
            EConstraintType nutrient = (EConstraintType)_i;

            // Clone the constraint UI element.
            GameObject go = Instantiate(m_missingDataSettingTemplate, m_missingNutrientsContent.transform);

            // Get and set the allowance display label.
            TMP_Text tmpText = go.GetComponentInChildren<TMP_Text>(); // GetComponentInChildren is DFS
            tmpText.text = Instance.acceptMissingNutrientValue[_i] ? "x" : "";

            // Setup the toggle button listener.
            go.GetComponentInChildren<Button>().onClick.AddListener(() => OnMissingNutrientToggle(nutrient, tmpText));

            // Set the constraint type label, so the user knows which constraint they are toggling.
            go.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = nutrient.ToString();
        }
    }


    /// <summary>
    /// [Listener Method]
    /// Called when user toggles a missing constraint allowance setting.
    /// </summary>
    /// <param name="nutrient">The constraint type to toggle it being allowed to be missing from foods.</param>
    /// <param name="tmpText">The label showing whether the constraint type is allowed missing or not.</param>
    private void OnMissingNutrientToggle(EConstraintType nutrient, TMP_Text tmpText)
    {
        OnBoolInputChanged(ref Instance.acceptMissingNutrientValue[(int)nutrient]);

        // Set the corresponding constraint type to Null so that it isn't considered (if toggled on).
        if (Instance.acceptMissingNutrientValue[(int)nutrient])
            Instance.constraints[(int)nutrient].Type = typeof(NullConstraint).FullName;

        // Update allowance label accordingly.
        tmpText.text = Instance.acceptMissingNutrientValue[(int)nutrient] ? "x" : "";
    }


    /// <summary>
    /// Deletes your data from your system.
    /// </summary>
    public void DeletePreferences()
    {
        string fn = Application.persistentDataPath + "/Preferences.json";
        try
        {
            if (File.Exists(fn))
            {
                File.Delete(fn);
            }
        }
        catch
        {
            Logger.Warn("A filesystem error occurred when trying to delete your preferences." +
                $"\nTo delete them manually, you can go to '{fn}'.");
        }

        // Regenerate default Preferences then reload UI.
        Saving.LoadPreferences();
        Init();
    }
}