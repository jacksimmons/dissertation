using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class AlgorithmSetup : MonoBehaviour
{
    // Nutrient fields (each object's index represents its Nutrient enum value)
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

    public static readonly string[] ALG_TYPES =
    { 
        typeof(AlgSFGA).FullName!,
        typeof(AlgPDGA).FullName!,
        typeof(AlgACO).FullName!,
    };


    public static readonly string[] CONSTRAINT_TYPES =
    {
        typeof(HardConstraint).FullName!,
        typeof(MinimiseConstraint).FullName!,
        typeof(ConvergeConstraint).FullName!,
    };


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
    private GameObject[] m_algSetupCategoryPanels;
    private int m_currentPanelIndex = 0;


    private void Start()
    {
        //
        // Set up listeners for all UI elements in the menu.
        //

        // Iteration variable should be used sparingly in listener declarations! Hence the variable name.
        for (int _i = 0; _i < m_nutrientFields.Length; _i++)
        {
            GameObject go;
            Nutrient nutrient;

            // _i usage scoped
            {
                go = m_nutrientFields[_i];

                // Must store the nutrient as a local variable, for use in the listener declarations.
                // Using iter var directly would lead to := m_nutrientFields.Length whenever a listener event occurred.
                nutrient = (Nutrient)_i;
            }

            TMP_InputField goalInput = go.transform.Find("GoalInput").GetComponent<TMP_InputField>();
            goalInput.onEndEdit.AddListener((string value) => OnGoalInputChanged(nutrient, value));

            TMP_InputField minInput = go.transform.Find("MinInput").GetComponent<TMP_InputField>();
            minInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.constraints[(int)nutrient].min, value));

            TMP_InputField maxInput = go.transform.Find("MaxInput").GetComponent<TMP_InputField>();
            maxInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.constraints[(int)nutrient].max, value));

            Button constraintTypeBtn = go.transform.Find("ConstraintTypeBtn").GetComponent<Button>();
            constraintTypeBtn.onClick.AddListener(() => OnCycleConstraintType(nutrient, constraintTypeBtn));
        }

        m_popSizeInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Preferences.Instance.populationSize, value));
        m_numStartingPortionsPerDayInput.onEndEdit.AddListener(
            (string value) => OnIntInputChanged(ref Preferences.Instance.numStartingPortionsPerDay, value));
        m_minStartMassInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Preferences.Instance.portionMinStartMass, value));
        m_maxStartMassInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Preferences.Instance.portionMaxStartMass, value));

        m_pheroImportanceInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.pheroImportance, value));
        m_pheroEvapRateInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.pheroEvapRate, value));
        m_alphaInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.acoAlpha, value));
        m_betaInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.acoBeta, value));

        UpdateUI();
    }


    private void OnGoalInputChanged(Nutrient nutrient, string value)
    {
        OnFloatInputChanged(ref Preferences.Instance.constraints[(int)nutrient].goal, value);
        OnGoalChanged(nutrient);
        UpdateUI();
    }


    /// <summary>
    /// Parses user input, then stores it in the given preference reference.
    /// </summary>
    /// <param name="pref">Reference to the relevant preference (to update).</param>
    /// <param name="value">The unparsed value. Guaranteed to contain a valid float, due to
    /// Unity's input field sanitation. Hence no need for TryParse.</param>
    private void OnFloatInputChanged(ref float pref, string value)
    {
        pref = float.Parse(value);
        Saving.SavePreferences();
    }


    /// <summary>
    /// OnFloatInputChanged, but for an integer preference and input field value.
    /// </summary>
    private void OnIntInputChanged(ref int pref, string value)
    {
        pref = int.Parse(value);
        Saving.SavePreferences();
    }


    private void OnGoalChanged(Nutrient nutrient)
    {
        switch (nutrient)
        {
            case Nutrient.Protein: case Nutrient.Fat: case Nutrient.Carbs:
                MacrosToCalories();
                break;
            case Nutrient.Kcal:
                CaloriesToMacros();
                break;
        }
        Saving.SavePreferences();
    }


    /// <summary>
    /// Calculate and assign kcal goal based on macro goals (kcal = 9 * fat + 4 * protein + 4 * carbs).
    /// The equation means that 1g fat = 9kcal, 1g protein = 4kcal, 1g carbs = 4kcal.
    /// </summary>
    private void MacrosToCalories()
    {
        Preferences.Instance.constraints[(int)Nutrient.Kcal].goal =
            Preferences.Instance.constraints[(int)Nutrient.Protein].goal * 4
            + Preferences.Instance.constraints[(int)Nutrient.Fat].goal * 9
            + Preferences.Instance.constraints[(int)Nutrient.Carbs].goal * 4;
    }


    /// <summary>
    /// Calculate and assign macro goals based on kcal goal.
    /// If total macros are non-zero, multiplies each macro by a multiplier that ensures they
    /// satisfy the ratio (kcal = 9 * fat + 4 * protein + 4 * carbs) [See MacrosToCalories for why].
    /// If total macros are zero, need to assign macros based on a recommended ratio [From research].
    /// </summary>
    private void CaloriesToMacros()
    {
        ref float kcal    = ref Preferences.Instance.constraints[(int)Nutrient.Kcal].goal;
        ref float protein = ref Preferences.Instance.constraints[(int)Nutrient.Protein].goal;
        ref float fat     = ref Preferences.Instance.constraints[(int)Nutrient.Protein].goal;
        ref float carbs   = ref Preferences.Instance.constraints[(int)Nutrient.Carbs].goal;

        // Calories calculated from the macros (not necessarily correct, goal is to correct it).
        float ratioCalories = 9 * fat + 4 * protein + 4 * carbs;

        // Div by zero case - assign recommended macro ratios from the given kcal.
        if (Mathf.Approximately(ratioCalories, 0))
        {
            float proteinCalories = 0.2f * kcal;
            float fatCalories = 0.3f * kcal;
            float carbCalories = 0.5f * kcal;

            // Convert calories to grams by dividing by each macro's respective ratio
            protein = proteinCalories / 4;
            fat     = fatCalories / 9;
            carbs   = carbCalories / 4;

            return;
        }

        // Default case - multiply macros so they satisfy the ratio kcal = 9fat + 4protein + 4carbs
        float multiplier = kcal / ratioCalories;
        
        protein *= multiplier;
        fat     *= multiplier;
        carbs   *= multiplier;
    }


    private void OnCycleConstraintType(Nutrient nutrient, Button btn)
    {
        // Get original type
        string type = Preferences.Instance.constraints[(int)nutrient].type;

        int indexOfType = Array.IndexOf(CONSTRAINT_TYPES, type);
        if (indexOfType == -1) Logger.Log($"Provided constraint type ({type}) was not valid.", Severity.Error);

        // Set the new type
        string newType = Preferences.Instance.constraints[(int)nutrient].type = ArrayTools.CircularNextElement(CONSTRAINT_TYPES, indexOfType, true);

        // Update the button label to reflect the change (need to do this as this input
        // is not done through an input field)
        btn.GetComponentInChildren<TMP_Text>().text = newType;
        Saving.SavePreferences();
    }


    public void OnToggleAddFitnessForMass()
    {
        Preferences.Instance.addFitnessForMass = !Preferences.Instance.addFitnessForMass;
        Saving.SavePreferences();
        m_addFitnessForMassBtnTxt.text = Preferences.Instance.addFitnessForMass ? "X" : "";
    }


    public void OnCycleAlgorithm()
    {
        Preferences.Instance.algorithmType =
            ALG_TYPES[ArrayTools.CircularNextIndex(
                Array.IndexOf(ALG_TYPES, Preferences.Instance.algorithmType, 0, ALG_TYPES.Length),
                ALG_TYPES.Length,
                true
            )];
        Saving.SavePreferences();
        m_algTypeText.text = $"Algorithm:\n{Preferences.Instance.algorithmType}";
    }


    private void UpdateUI()
    {
        Preferences p = Preferences.Instance;
        ConstraintData[] constraints = p.constraints;

        for (int _i = 0; _i < m_nutrientFields.Length; _i++)
        {
            GameObject go;
            Nutrient nutrient;

            {
                go = m_nutrientFields[_i];

                // Must store the nutrient as a local variable, for use in the listener declarations.
                // Using iter var directly would lead to := m_nutrientFields.Length whenever a listener event occurred.
                nutrient = (Nutrient)_i;
            }

            go.transform.Find("GoalInput").GetComponent<TMP_InputField>().text = constraints[(int)nutrient].goal.ToString();
            go.transform.Find("MinInput").GetComponent<TMP_InputField>().text = constraints[(int)nutrient].min.ToString();
            go.transform.Find("MaxInput").GetComponent<TMP_InputField>().text = constraints[(int)nutrient].max.ToString();

            go.transform.Find("ConstraintTypeBtn").GetComponentInChildren<TMP_Text>().text = constraints[(int)nutrient].type;
        }

        m_popSizeInput.text = p.populationSize.ToString();
        m_numStartingPortionsPerDayInput.text = p.numStartingPortionsPerDay.ToString();
        m_minStartMassInput.text = p.portionMinStartMass.ToString();
        m_maxStartMassInput.text = p.portionMaxStartMass.ToString();
        m_addFitnessForMassBtnTxt.text = Preferences.Instance.addFitnessForMass ? "X" : "";
        m_algTypeText.text = $"Algorithm:\n{Preferences.Instance.algorithmType}";

        m_pheroEvapRateInput.text = p.pheroEvapRate.ToString();
        m_pheroImportanceInput.text = p.pheroImportance.ToString();
        m_alphaInput.text = p.acoAlpha.ToString();
        m_betaInput.text = p.acoBeta.ToString();
    }


    public void OnAlgSetupNavBtnPressed(bool right)
    {
        UITools.OnNavBtnPressed(right, m_algSetupCategoryPanels, ref m_currentPanelIndex);
    }
}