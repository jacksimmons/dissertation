using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class AlgorithmSetup : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField m_kcalInputField;
    [SerializeField]
    private TMP_InputField m_proteinInputField;
    [SerializeField]
    private TMP_InputField m_fatInputField;
    [SerializeField]
    private TMP_InputField m_carbsInputField;


    private void Start()
    {
        m_kcalInputField.onEndEdit.AddListener((string value) => OnGoalInputChanged(Nutrient.Kcal, value, m_kcalInputField));
        m_proteinInputField.onEndEdit.AddListener((string value) => OnGoalInputChanged(Nutrient.Protein, value, m_proteinInputField));
        m_fatInputField.onEndEdit.AddListener((string value) => OnGoalInputChanged(Nutrient.Fat, value, m_fatInputField));
        m_carbsInputField.onEndEdit.AddListener((string value) => OnGoalInputChanged(Nutrient.Carbs, value, m_carbsInputField));

        UpdateGoalPreferencesButtons();
    }


    private void OnGoalInputChanged(Nutrient nutrient, string value, TMP_InputField input)
    {
        float goal = 0;
        if (PreferencesHandler.ParseDecimalInputField(value, ref goal, input))
        {
            Preferences.Instance.goals[(int)nutrient] = goal;
            OnGoalChanged(nutrient);
            PreferencesHandler.SavePreferences();
            UpdateGoalPreferencesButtons();
        }
    }


    public void OnGoalChanged(Nutrient nutrient)
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


    private void UpdateGoalPreferencesButtons()
    {
        Preferences p = Preferences.Instance;

        m_kcalInputField.text = $"{p.goals[(int)Nutrient.Kcal]}";
        m_proteinInputField.text = $"{p.goals[(int)Nutrient.Protein]}";
        m_fatInputField.text = $"{p.goals[(int)Nutrient.Fat]}";
        m_carbsInputField.text = $"{p.goals[(int)Nutrient.Carbs]}";
    }
}