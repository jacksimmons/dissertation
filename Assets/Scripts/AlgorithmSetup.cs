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
        m_kcalInputField.onEndEdit.AddListener((string value) => OnGoalInputChanged(Proximate.Kcal, value, m_kcalInputField));
        m_proteinInputField.onEndEdit.AddListener((string value) => OnGoalInputChanged(Proximate.Protein, value, m_proteinInputField));
        m_fatInputField.onEndEdit.AddListener((string value) => OnGoalInputChanged(Proximate.Fat, value, m_fatInputField));
        m_carbsInputField.onEndEdit.AddListener((string value) => OnGoalInputChanged(Proximate.Carbs, value, m_carbsInputField));

        UpdateGoalPreferencesButtons();
    }


    private void OnGoalInputChanged(Proximate proximate, string value, TMP_InputField input)
    {
        float goal = 0;
        if (PreferencesHandler.ParseDecimalInputField(value, ref goal, input))
        {
            Preferences.Instance.goals[(int)proximate] = goal;
            OnGoalChanged(proximate);
            PreferencesHandler.SavePreferences();
            UpdateGoalPreferencesButtons();
        }
    }


    public void OnGoalChanged(Proximate proximate)
    {
        switch (proximate)
        {
            case Proximate.Protein: case Proximate.Fat: case Proximate.Carbs:
                MacrosToCalories();
                break;
            case Proximate.Kcal:
                CaloriesToMacros();
                break;
        }
    }


    private void MacrosToCalories()
    {
        Preferences.Instance.goals[(int)Proximate.Kcal] =
            Preferences.Instance.goals[(int)Proximate.Protein] * 4
            + Preferences.Instance.goals[(int)Proximate.Fat] * 9
            + Preferences.Instance.goals[(int)Proximate.Carbs] * 4;
    }


    private void CaloriesToMacros()
    {
        float previousCalories =
            Preferences.Instance.goals[(int)Proximate.Protein] * 4
            + Preferences.Instance.goals[(int)Proximate.Fat] * 9
            + Preferences.Instance.goals[(int)Proximate.Carbs] * 4;

        // Div by zero case - assign recommended macro ratios.
        if (Mathf.Approximately(previousCalories, 0))
        {
            float calories = Preferences.Instance.goals[(int)Proximate.Kcal];
            float proteinCalories = 0.2f * calories;
            float fatCalories = 0.3f * calories;
            float carbCalories = 0.5f * calories;

            // Convert calories to grams by dividing by each macro's respective ratio
            Preferences.Instance.goals[(int)Proximate.Protein] = proteinCalories / 4;
            Preferences.Instance.goals[(int)Proximate.Fat] = fatCalories / 9;
            Preferences.Instance.goals[(int)Proximate.Carbs] = carbCalories / 4;

            return;
        }

        float multiplier = Preferences.Instance.goals[(int)Proximate.Kcal] / previousCalories;
        Preferences.Instance.goals[(int)Proximate.Protein] *= multiplier;
        Preferences.Instance.goals[(int)Proximate.Fat] *= multiplier;
        Preferences.Instance.goals[(int)Proximate.Carbs] *= multiplier;
    }


    private void UpdateGoalPreferencesButtons()
    {
        Preferences p = Preferences.Instance;

        m_kcalInputField.text = $"{p.goals[(int)Proximate.Kcal]}";
        m_proteinInputField.text = $"{p.goals[(int)Proximate.Protein]}";
        m_fatInputField.text = $"{p.goals[(int)Proximate.Fat]}";
        m_carbsInputField.text = $"{p.goals[(int)Proximate.Carbs]}";
    }
}