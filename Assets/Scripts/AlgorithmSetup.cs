using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class AlgorithmSetup : MonoBehaviour
{
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
            minInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.constraints[(int)nutrient].Min, value));

            TMP_InputField maxInput = go.transform.Find("MaxInput").GetComponent<TMP_InputField>();
            maxInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.constraints[(int)nutrient].Max, value));

            Button constraintTypeBtn = go.transform.Find("ConstraintTypeBtn").GetComponent<Button>();
            constraintTypeBtn.onClick.AddListener(() => OnCycleConstraintType(nutrient, constraintTypeBtn));
        }

        m_popSizeInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Preferences.Instance.populationSize, value));
        m_numStartingPortionsPerDayInput.onEndEdit.AddListener(
            (string value) => OnIntInputChanged(ref Preferences.Instance.numStartingPortionsPerDay, value));
        m_minStartMassInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Preferences.Instance.minPortionMass, value));
        m_maxStartMassInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Preferences.Instance.maxPortionMass, value));

        m_pheroImportanceInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.pheroImportance, value));
        m_pheroEvapRateInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.pheroEvapRate, value));
        m_alphaInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.acoAlpha, value));
        m_betaInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.acoBeta, value));

        UpdateUI();
    }


    private void OnGoalInputChanged(ENutrient nutrient, string value)
    {
        OnFloatInputChanged(ref Preferences.Instance.constraints[(int)nutrient].Goal, value);
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


    private void OnGoalChanged(ENutrient nutrient)
    {
        switch (nutrient)
        {
            case ENutrient.Protein: case ENutrient.Fat: case ENutrient.Carbs:
                MacrosToCalories();
                break;
            case ENutrient.Kcal:
                CaloriesToMacros();
                break;
        }
        Saving.SavePreferences();
    }


    private void MacrosToCalories()
    {
        CalorieMassConverter.MacrosToCalories(
            ref Preferences.Instance.constraints[(int)ENutrient.Kcal].Goal,
            Preferences.Instance.constraints[(int)ENutrient.Protein].Goal,
            Preferences.Instance.constraints[(int)ENutrient.Fat].Goal,
            Preferences.Instance.constraints[(int)ENutrient.Carbs].Goal
        );
    }


    private void CaloriesToMacros()
    {
        CalorieMassConverter.CaloriesToMacros(
            Preferences.Instance.constraints[(int)ENutrient.Kcal].Goal,
            ref Preferences.Instance.constraints[(int)ENutrient.Protein].Goal,
            ref Preferences.Instance.constraints[(int)ENutrient.Fat].Goal,
            ref Preferences.Instance.constraints[(int)ENutrient.Carbs].Goal
        );
    }


    private void OnCycleConstraintType(ENutrient nutrient, Button btn)
    {
        // Get original type
        string type = Preferences.Instance.constraints[(int)nutrient].Type;

        int indexOfType = Array.IndexOf(Preferences.CONSTRAINT_TYPES, type);
        if (indexOfType == -1) Logger.Log($"Provided constraint type ({type}) was not valid.", Severity.Error);

        // Set the new type
        string newType = Preferences.Instance.constraints[(int)nutrient].Type = ArrayTools.CircularNextElement(Preferences.CONSTRAINT_TYPES, indexOfType, true);

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
            Preferences.ALG_TYPES[ArrayTools.CircularNextIndex(
                Array.IndexOf(Preferences.ALG_TYPES, Preferences.Instance.algorithmType, 0, Preferences.ALG_TYPES.Length),
                Preferences.ALG_TYPES.Length,
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

            go.transform.Find("ConstraintTypeBtn").GetComponentInChildren<TMP_Text>().text = constraints[(int)nutrient].Type;
        }

        m_popSizeInput.text = p.populationSize.ToString();
        m_numStartingPortionsPerDayInput.text = p.numStartingPortionsPerDay.ToString();
        m_minStartMassInput.text = p.minPortionMass.ToString();
        m_maxStartMassInput.text = p.maxPortionMass.ToString();
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