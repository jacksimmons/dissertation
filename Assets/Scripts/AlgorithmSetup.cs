// Commented 8/4
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// A Unity script which handles algorithm preference setup.
/// In short, handles the Algorithm Setup panel.
/// </summary>
public sealed class AlgorithmSetup : SetupBehaviour
{
    private static Preferences Prefs => Preferences.Instance;

    // EConstraintType fields (each object's index represents its EConstraintType enum value)
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
    private Button m_addFitnessForMassBtn;
    [SerializeField]
    private TMP_Text m_addFitnessForMassBtnTxt;
    [SerializeField]
    private TMP_Text m_algTypeText;


    // GA Settings
    [SerializeField]
    private TMP_InputField m_mutationMassChangeMinInput;
    [SerializeField]
    private TMP_InputField m_mutationMassChangeMaxInput;
    [SerializeField]
    private TMP_InputField m_selectionPressureInput;
    [SerializeField]
    private TMP_InputField m_numCrossoverPointsInput;
    [SerializeField]
    private Button m_selectionMethodCycleBtn;
    [SerializeField]
    private TMP_Text m_selectionMethodTxt;
    [SerializeField]
    private Button m_fitnessApproachCycleBtn;
    [SerializeField]
    private TMP_Text m_fitnessApproachTxt;
    [SerializeField]
    private TMP_InputField m_changePortionMassMutationProbInput;
    [SerializeField]
    private TMP_InputField m_addOrRemovePortionMutationProbInput;


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
    private TMP_InputField m_inertialWeightInput;


    [SerializeField]
    private GameObject[] m_algSetupCategoryPanels;
    private int m_currentPanelIndex = 0;


    private void Start()
    {
        //
        // Set up listeners for all UI elements in the menu.
        //

        // Setup listener for each of Goal, Min, Max and Weight input fields, for every nutrient UI block.
        for (int _i = 0; _i < m_nutrientFields.Length; _i++)
        {
            GameObject go;
            EConstraintType nutrient;

            // Scoped _i usage
            {
                go = m_nutrientFields[_i];

                // Must store the nutrient as a local variable, for use in the listener declarations.
                // Using iter var directly would lead to := m_nutrientFields.Length whenever a listener event occurred.
                nutrient = (EConstraintType)_i;
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

        // Setup all non-repeating input field listeners
        m_popSizeInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Prefs.populationSize, value));
        m_numStartingPortionsPerDayInput.onEndEdit.AddListener(
            (string value) => OnIntInputChanged(ref Prefs.numStartingPortionsPerDay, value));
        m_minStartMassInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Prefs.minPortionMass, value));
        m_maxStartMassInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Prefs.maxPortionMass, value));
        m_addFitnessForMassBtn.onClick.AddListener(() => OnToggleBtnPressed(ref Prefs.addFitnessForMass, m_addFitnessForMassBtnTxt));

        m_mutationMassChangeMinInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Prefs.mutationMassChangeMin, value));
        m_mutationMassChangeMaxInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Prefs.mutationMassChangeMax, value));
        m_selectionPressureInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.selectionPressure, value));
        m_numCrossoverPointsInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Prefs.numCrossoverPoints, value));
        m_fitnessApproachCycleBtn.onClick.AddListener(() => OnCycleEnumWithLabel(ref Prefs.fitnessApproach, true, m_fitnessApproachTxt));
        m_selectionMethodCycleBtn.onClick.AddListener(() => OnCycleEnumWithLabel(ref Prefs.selectionMethod, true, m_selectionMethodTxt));
        m_changePortionMassMutationProbInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.changePortionMassMutationProb, value));
        m_addOrRemovePortionMutationProbInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.addOrRemovePortionMutationProb, value));

        m_pheroImportanceInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.pheroImportance, value));
        m_pheroEvapRateInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.pheroEvapRate, value));
        m_alphaInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.acoAlpha, value));
        m_betaInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.acoBeta, value));
        m_colonyPortionsInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Prefs.colonyPortions, value));
        m_stagnationItersInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Prefs.colonyStagnationIters, value));

        m_pbestAccInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.pAccCoefficient, value));
        m_gbestAccInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.gAccCoefficient, value));
        m_inertialWeightInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Prefs.inertialWeight, value));

        UpdateUI();
    }


    /// <summary>
    /// Specific listener method for when a Goal is changed.
    /// The Goal field has extra features, such as when Kcal's Goal is updated, the program updates Protein, Fat and Carbs' goals accordingly.
    /// The inverse is also true.
    /// </summary>
    /// <param name="nutrient">The nutrient this is the goal for.</param>
    /// <param name="value">The unparsed value from input.</param>
    private void OnGoalInputChanged(EConstraintType nutrient, string value)
    {
        OnFloatInputChanged(ref Prefs.constraints[(int)nutrient].Goal, value);
        OnGoalChanged(nutrient);
        UpdateUI();
    }


    /// <summary>
    /// Handles extra Goal-related features. If Protein, Fat or Carbs get their goal updated, Kcal must get its goal updated too, and vice
    /// versa. Then saves the result.
    /// </summary>
    /// <param name="nutrient">The nutrient the goal was updated for.</param>
    private static void OnGoalChanged(EConstraintType nutrient)
    {
        switch (nutrient)
        {
            case EConstraintType.Protein:
            case EConstraintType.Fat:
            case EConstraintType.Carbs:
                MacrosToCalories();
                break;
            case EConstraintType.Kcal:
                CaloriesToMacros();
                break;
        }
        Saving.SavePreferences();
    }


    /// <summary>
    /// Shorthand for the MathTools MacrosToCalories method.
    /// </summary>
    private static void MacrosToCalories()
    {
        MathTools.MacrosToCalories(
            ref Prefs.constraints[(int)EConstraintType.Kcal].Goal,
            Prefs.constraints[(int)EConstraintType.Protein].Goal,
            Prefs.constraints[(int)EConstraintType.Fat].Goal,
            Prefs.constraints[(int)EConstraintType.Carbs].Goal
        );
    }


    /// <summary>
    /// Shorthand for the MathTools CaloriesToMacros method.
    /// </summary>
    private static void CaloriesToMacros()
    {
        MathTools.CaloriesToMacros(
            Prefs.constraints[(int)EConstraintType.Kcal].Goal,
            ref Prefs.constraints[(int)EConstraintType.Protein].Goal,
            ref Prefs.constraints[(int)EConstraintType.Fat].Goal,
            ref Prefs.constraints[(int)EConstraintType.Carbs].Goal
        );
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


    /// <summary>
    /// Sets all of the UI elements' text components, to match the saved preferences.
    /// </summary>
    private void UpdateUI()
    {
        Preferences p = Prefs;
        ConstraintData[] constraints = p.constraints;

        for (int _i = 0; _i < m_nutrientFields.Length; _i++)
        {
            GameObject go;
            EConstraintType nutrient;

            {
                go = m_nutrientFields[_i];

                // Must store the nutrient as a local variable, for use in the listener declarations.
                // Using iter var directly would lead to := m_nutrientFields.Length whenever a listener event occurred.
                nutrient = (EConstraintType)_i;
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
        m_addFitnessForMassBtnTxt.text = p.addFitnessForMass ? "X" : "";
        m_algTypeText.text = Prefs.algorithmType;

        m_mutationMassChangeMinInput.text = $"{Prefs.mutationMassChangeMin}";
        m_mutationMassChangeMaxInput.text = $"{Prefs.mutationMassChangeMax}";
        m_selectionPressureInput.text = $"{Prefs.selectionPressure}";
        m_numCrossoverPointsInput.text = $"{Prefs.numCrossoverPoints}";
        m_fitnessApproachTxt.text = $"{Prefs.fitnessApproach}";
        m_selectionMethodTxt.text = $"{Prefs.selectionMethod}";
        m_changePortionMassMutationProbInput.text = $"{Prefs.changePortionMassMutationProb}";
        m_addOrRemovePortionMutationProbInput.text = $"{Prefs.addOrRemovePortionMutationProb}";

        m_pheroImportanceInput.text = p.pheroImportance.ToString();
        m_pheroEvapRateInput.text = p.pheroEvapRate.ToString();
        m_alphaInput.text = p.acoAlpha.ToString();
        m_betaInput.text = p.acoBeta.ToString();
        m_colonyPortionsInput.text = p.colonyPortions.ToString();
        m_stagnationItersInput.text = p.colonyStagnationIters.ToString();

        m_pbestAccInput.text = p.pAccCoefficient.ToString();
        m_gbestAccInput.text = p.gAccCoefficient.ToString();
        m_inertialWeightInput.text = p.inertialWeight.ToString();
    }


    public void OnAlgSetupNavBtnPressed(bool right)
    {
        UITools.OnNavBtnPressed(right, m_algSetupCategoryPanels, ref m_currentPanelIndex);
    }
}