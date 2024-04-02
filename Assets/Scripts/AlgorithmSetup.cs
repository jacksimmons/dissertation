using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// A Unity script which handles algorithm preference setup.
/// In short, handles the Algorithm Setup panel.
/// </summary>
public class AlgorithmSetup : SetupBehaviour
{
    private static Preferences Prefs => Preferences.Instance;


    // ENutrient fields (each object's index represents its ENutrient enum value)
    [SerializeField]
    private GameObject[] m_nutrientFields;

    // Alg Settings
    [SerializeField]
    private TMP_InputField m_popSizeInput;
    [SerializeField]
    private TMP_InputField m_numStartingPortionsPerDayInput;
    [SerializeField]
    private TMP_InputField m_minStartMassInput;
    [SerializeField]
    private TMP_InputField m_maxStartMassInput;
    [SerializeField]
    private TMP_Text m_addFitnessForMassBtnTxt;
    [SerializeField]
    private TMP_Text m_algTypeText;
    [SerializeField]
    private Button m_fitnessApproachBtn;
    [SerializeField]
    private TMP_Text m_fitnessApproachTxt;


    // GA Settings
    [SerializeField]
    private Button m_selectionMethodBtn;
    [SerializeField]
    private TMP_Text m_selectionMethodTxt;


    // ACO Settings
    [SerializeField]
    private TMP_InputField m_pheroImportanceInput;
    [SerializeField]
    private TMP_InputField m_pheroEvapRateInput;
    [SerializeField]
    private TMP_InputField m_alphaInput;
    [SerializeField]
    private TMP_InputField m_betaInput;
    [SerializeField]
    private TMP_InputField m_colonyPortionsInput;
    [SerializeField]
    private TMP_InputField m_stagnationItersInput;


    // PSO Settings
    [SerializeField]
    private TMP_InputField m_pbestAccInput;
    [SerializeField]
    private TMP_InputField m_gbestAccInput;

    [SerializeField]
    private GameObject[] m_algSetupCategoryPanels;
    private int m_currentPanelIndex = 0;


    private void Start()
    {
        //
        // Set up listeners for all UI elements in the menu.
        //

        // Iteration variable should be not be used in AddListener calls, hence the variable name.
        for (int _i = 0; _i < m_nutrientFields.Length; _i++)
        {
            GameObject go;
            ENutrient nutrient;

            // _i usage scoped
            {
                go = m_nutrientFields[_i];

                // Must store the nutrient as a local variable, for use in the listener declarations.
                // Using iter var directly would lead to := m_nutrientFields.Length whenever a listener event occurred.
                nutrient = (ENutrient)_i;
            }

            TMP_InputField goalInput = go.transform.Find("GoalInput").GetComponent<TMP_InputField>();
            goalInput.onEndEdit.AddListener((string value) => OnGoalInputChanged(nutrient, value));

            TMP_InputField minInput = go.transform.Find("MinInput").GetComponent<TMP_InputField>();
            minInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.constraints[(int)nutrient].Min, value));

            TMP_InputField maxInput = go.transform.Find("MaxInput").GetComponent<TMP_InputField>();
            maxInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.constraints[(int)nutrient].Max, value));

            TMP_InputField weightInput = go.transform.Find("WeightInput").GetComponent<TMP_InputField>();
            weightInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.constraints[(int)nutrient].Weight, value));

            Button constraintTypeBtn = go.transform.Find("ConstraintTypeBtn").GetComponent<Button>();
            constraintTypeBtn.onClick.AddListener(() => OnCycleArrayWithLabel(ref Prefs.constraints[(int)nutrient].Type, Preferences.CONSTRAINT_TYPES,
                true, constraintTypeBtn.GetComponentInChildren<TMP_Text>()));
        }

        m_popSizeInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Prefs.populationSize, value));
        m_numStartingPortionsPerDayInput.onEndEdit.AddListener(
            (string value) => OnIntInputChanged(ref Prefs.numStartingPortionsPerDay, value));
        m_minStartMassInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Prefs.minPortionMass, value));
        m_maxStartMassInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Prefs.maxPortionMass, value));

        // Get guaranteed text component through prefab hierarchy
        m_fitnessApproachBtn.onClick.AddListener(() => OnCycleEnumWithLabel(ref Prefs.fitnessApproach, true, m_fitnessApproachTxt));
        m_selectionMethodBtn.onClick.AddListener(() => OnCycleEnumWithLabel(ref Prefs.selectionMethod, true, m_selectionMethodTxt));

        m_pheroImportanceInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.pheroImportance, value));
        m_pheroEvapRateInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.pheroEvapRate, value));
        m_alphaInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.acoAlpha, value));
        m_betaInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.acoBeta, value));
        m_colonyPortionsInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Prefs.colonyPortions, value));
        m_stagnationItersInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Prefs.colonyStagnationIters, value));

        m_pbestAccInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.pAccCoefficient, value));
        m_gbestAccInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.gAccCoefficient, value));

        UpdateUI();
    }


    private void OnGoalInputChanged(ENutrient nutrient, string value)
    {
        OnFloatInputChanged(ref Prefs.constraints[(int)nutrient].Goal, value);
        OnGoalChanged(nutrient);
        UpdateUI();
    }


    private static void OnGoalChanged(ENutrient nutrient)
    {
        switch (nutrient)
        {
            case ENutrient.Protein:
            case ENutrient.Fat:
            case ENutrient.Carbs:
                MacrosToCalories();
                break;
            case ENutrient.Kcal:
                CaloriesToMacros();
                break;
        }
        Saving.SavePreferences();
    }


    private static void MacrosToCalories()
    {
        MathTools.MacrosToCalories(
            ref Prefs.constraints[(int)ENutrient.Kcal].Goal,
            Prefs.constraints[(int)ENutrient.Protein].Goal,
            Prefs.constraints[(int)ENutrient.Fat].Goal,
            Prefs.constraints[(int)ENutrient.Carbs].Goal
        );
    }


    private static void CaloriesToMacros()
    {
        MathTools.CaloriesToMacros(
            Prefs.constraints[(int)ENutrient.Kcal].Goal,
            ref Prefs.constraints[(int)ENutrient.Protein].Goal,
            ref Prefs.constraints[(int)ENutrient.Fat].Goal,
            ref Prefs.constraints[(int)ENutrient.Carbs].Goal
        );
    }


    public void OnToggleAddFitnessForMass()
    {
        Prefs.addFitnessForMass = !Prefs.addFitnessForMass;
        Saving.SavePreferences();
        m_addFitnessForMassBtnTxt.text = Prefs.addFitnessForMass ? "X" : "";
    }


    public void OnCycleAlgorithm()
    {
        Prefs.algorithmType =
            Preferences.ALG_TYPES[ArrayTools.CircularNextIndex(
                Array.IndexOf(Preferences.ALG_TYPES, Prefs.algorithmType, 0, Preferences.ALG_TYPES.Length),
                Preferences.ALG_TYPES.Length,
                true
            )];
        Saving.SavePreferences();
        m_algTypeText.text = Prefs.algorithmType;
    }


    private void UpdateUI()
    {
        Preferences p = Prefs;
        ConstraintData[] constraints = p.constraints;

        for (int _i = 0; _i < m_nutrientFields.Length; _i++)
        {
            GameObject go;
            ENutrient nutrient;

            {
                go = m_nutrientFields[_i];

                // Must store the nutrient as a local variable, for use in the listener declarations.
                // Using iter var directly would lead to := m_nutrientFields.Length whenever a listener event occurred.
                nutrient = (ENutrient)_i;
            }

            go.transform.Find("GoalInput").GetComponent<TMP_InputField>().text = constraints[(int)nutrient].Goal.ToString();
            go.transform.Find("MinInput").GetComponent<TMP_InputField>().text = constraints[(int)nutrient].Min.ToString();
            go.transform.Find("MaxInput").GetComponent<TMP_InputField>().text = constraints[(int)nutrient].Max.ToString();
            go.transform.Find("WeightInput").GetComponent<TMP_InputField>().text = constraints[(int)nutrient].Weight.ToString();

            go.transform.Find("ConstraintTypeBtn").GetComponentInChildren<TMP_Text>().text = constraints[(int)nutrient].Type;
        }

        m_popSizeInput.text = p.populationSize.ToString();
        m_numStartingPortionsPerDayInput.text = p.numStartingPortionsPerDay.ToString();
        m_minStartMassInput.text = p.minPortionMass.ToString();
        m_maxStartMassInput.text = p.maxPortionMass.ToString();
        m_addFitnessForMassBtnTxt.text = Prefs.addFitnessForMass ? "X" : "";
        m_algTypeText.text = Prefs.algorithmType;

        m_fitnessApproachTxt.text = $"{Prefs.fitnessApproach}";
        m_selectionMethodTxt.text = $"{Prefs.selectionMethod}";

        m_pheroImportanceInput.text = p.pheroImportance.ToString();
        m_pheroEvapRateInput.text = p.pheroEvapRate.ToString();
        m_alphaInput.text = p.acoAlpha.ToString();
        m_betaInput.text = p.acoBeta.ToString();
        m_colonyPortionsInput.text = p.colonyPortions.ToString();
        m_stagnationItersInput.text = p.colonyStagnationIters.ToString();

        m_pbestAccInput.text = p.pAccCoefficient.ToString();
        m_gbestAccInput.text = p.gAccCoefficient.ToString();
    }


    public void OnAlgSetupNavBtnPressed(bool right)
    {
        UITools.OnNavBtnPressed(right, m_algSetupCategoryPanels, ref m_currentPanelIndex);
    }
}