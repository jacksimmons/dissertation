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

    // GA Settings
    [SerializeField]
    private TMP_Text m_gaTypeText;

    // ACO Settings
    [SerializeField]
    private TMP_InputField m_numAntsInput;
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

            TMP_InputField toleranceInput = go.transform.Find("ToleranceInput").GetComponent<TMP_InputField>();
            toleranceInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.tolerances[(int)nutrient], value));

            TMP_InputField steepnessInput = go.transform.Find("SteepnessInput").GetComponent<TMP_InputField>();
            steepnessInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.steepnesses[(int)nutrient], value));

            Button constraintTypeBtn = go.transform.Find("ConstraintTypeBtn").GetComponent<Button>();
            constraintTypeBtn.onClick.AddListener(() => OnConstraintTypeChanged(nutrient, constraintTypeBtn));
        }

        m_popSizeInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Preferences.Instance.populationSize, value));
        m_numStartingPortionsPerDayInput.onEndEdit.AddListener(
            (string value) => OnIntInputChanged(ref Preferences.Instance.numStartingPortionsPerDay, value));
        m_minStartMassInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Preferences.Instance.portionMinStartMass, value));
        m_maxStartMassInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Preferences.Instance.portionMaxStartMass, value));

        m_numAntsInput.onEndEdit.AddListener((string value) => OnIntInputChanged(ref Preferences.Instance.numAnts, value));
        m_pheroImportanceInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.pheroImportance, value));
        m_pheroEvapRateInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.pheroEvapRate, value));
        m_alphaInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.acoAlpha, value));
        m_betaInput.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref Preferences.Instance.acoBeta, value));

        UpdateUI();
    }


    private void OnGoalInputChanged(Nutrient nutrient, string value)
    {
        OnFloatInputChanged(ref Preferences.Instance.goals[(int)nutrient], value);
        OnGoalChanged(nutrient);
        UpdateUI();
    }


    private void OnFloatInputChanged(ref float pref, string value)
    {
        pref = float.Parse(value);
        Static.SavePreferences();
    }


    private void OnIntInputChanged(ref int pref, string value)
    {
        pref = int.Parse(value);
        Static.SavePreferences();
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
        Static.SavePreferences();
    }


    private void OnConstraintTypeChanged(Nutrient nutrient, Button pressedBtn)
    {
        ConstraintType type = Preferences.Instance.constraintTypes[(int)nutrient];

        // Store the following in an easy to write variable name
        ConstraintType newType =
        // Increment the constraint type by 1 (index is circularly wrapped by an extension method)
        Preferences.Instance.constraintTypes[(int)nutrient] =
            (ConstraintType)Static.NextCircularArrayIndex((int)type, Enum.GetValues(typeof(ConstraintType)).Length, true);

        pressedBtn.GetComponentInChildren<TMP_Text>().text = $"Goal:\n{newType}";
        Static.SavePreferences();
    }


    private void MacrosToCalories()
    {
        Preferences.Instance.goals[(int)Nutrient.Kcal] =
            Preferences.Instance.goals[(int)Nutrient.Protein] * 4
            + Preferences.Instance.goals[(int)Nutrient.Fat] * 9
            + Preferences.Instance.goals[(int)Nutrient.Carbs] * 4;
    }


    private void CaloriesToMacros()
    {
        float previousCalories =
            Preferences.Instance.goals[(int)Nutrient.Protein] * 4
            + Preferences.Instance.goals[(int)Nutrient.Fat] * 9
            + Preferences.Instance.goals[(int)Nutrient.Carbs] * 4;

        // Div by zero case - assign recommended macro ratios.
        if (Mathf.Approximately(previousCalories, 0))
        {
            float calories = Preferences.Instance.goals[(int)Nutrient.Kcal];
            float proteinCalories = 0.2f * calories;
            float fatCalories = 0.3f * calories;
            float carbCalories = 0.5f * calories;

            // Convert calories to grams by dividing by each macro's respective ratio
            Preferences.Instance.goals[(int)Nutrient.Protein] = proteinCalories / 4;
            Preferences.Instance.goals[(int)Nutrient.Fat] = fatCalories / 9;
            Preferences.Instance.goals[(int)Nutrient.Carbs] = carbCalories / 4;

            return;
        }

        float multiplier = Preferences.Instance.goals[(int)Nutrient.Kcal] / previousCalories;
        Preferences.Instance.goals[(int)Nutrient.Protein] *= multiplier;
        Preferences.Instance.goals[(int)Nutrient.Fat] *= multiplier;
        Preferences.Instance.goals[(int)Nutrient.Carbs] *= multiplier;
    }


    public void OnToggleAddFitnessForMass()
    {
        Preferences.Instance.addFitnessForMass = !Preferences.Instance.addFitnessForMass;
        m_addFitnessForMassBtnTxt.text = Preferences.Instance.addFitnessForMass ? "X" : "";
    }


    public void OnCycleAlgorithm()
    {
        Preferences.Instance.algType = (AlgorithmType)
            Static.NextCircularArrayIndex((int)Preferences.Instance.algType, Enum.GetValues(typeof(AlgorithmType)).Length, true);
        Static.SavePreferences();
        m_algTypeText.text = $"Algorithm:\n{Preferences.Instance.algType}";
    }


    public void OnCycleGAType()
    {
        Preferences.Instance.gaType = (GAType)
            Static.NextCircularArrayIndex((int)Preferences.Instance.gaType, Enum.GetValues(typeof(GAType)).Length, true);
        Static.SavePreferences();
        m_gaTypeText.text = $"GA Type:\n{Preferences.Instance.gaType}";
    }


    private void UpdateUI()
    {
        Preferences p = Preferences.Instance;

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

            go.transform.Find("GoalInput").GetComponent<TMP_InputField>().text = p.goals[(int)nutrient].ToString();
            go.transform.Find("ToleranceInput").GetComponent<TMP_InputField>().text = p.tolerances[(int)nutrient].ToString();
            go.transform.Find("SteepnessInput").GetComponent<TMP_InputField>().text = p.steepnesses[(int)nutrient].ToString();

            go.transform.Find("ConstraintTypeBtn").GetComponentInChildren<TMP_Text>().text = $"Goal:\n{p.constraintTypes[(int)nutrient]}";
        }

        m_popSizeInput.text = p.populationSize.ToString();
        m_numStartingPortionsPerDayInput.text = p.numStartingPortionsPerDay.ToString();
        m_minStartMassInput.text = p.portionMinStartMass.ToString();
        m_maxStartMassInput.text = p.portionMaxStartMass.ToString();
        m_addFitnessForMassBtnTxt.text = Preferences.Instance.addFitnessForMass ? "X" : "";
        m_algTypeText.text = $"Algorithm:\n{Preferences.Instance.algType}";

        m_gaTypeText.text = $"GA Type:\n{Preferences.Instance.gaType}";

        m_numAntsInput.text = p.numAnts.ToString();
        m_pheroEvapRateInput.text = p.pheroEvapRate.ToString();
        m_pheroImportanceInput.text = p.pheroImportance.ToString();
        m_alphaInput.text = p.acoAlpha.ToString();
        m_betaInput.text = p.acoBeta.ToString();
    }


    public void OnAlgSetupNavBtnPressed(bool right)
    {
        Static.OnNavBtnPressed(right, m_algSetupCategoryPanels, ref m_currentPanelIndex);
    }
}