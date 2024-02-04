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
        m_kcalInputField.onSubmit.AddListener((string value) => OnGoalInputChanged(Proximate.Kcal, value, m_kcalInputField));
        m_proteinInputField.onSubmit.AddListener((string value) => OnGoalInputChanged(Proximate.Protein, value, m_proteinInputField));
        m_fatInputField.onSubmit.AddListener((string value) => OnGoalInputChanged(Proximate.Fat, value, m_fatInputField));
        m_carbsInputField.onSubmit.AddListener((string value) => OnGoalInputChanged(Proximate.Carbs, value, m_carbsInputField));

        UpdateGoalPreferencesButtons();
    }


    private void OnGoalInputChanged(Proximate proximate, string value, TMP_InputField input)
    {
        float goal = 0;
        if (PreferencesHandler.ParseDecimalInputField(value, ref goal, input))
        {
            Preferences.Instance.goals[proximate] = goal;
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
        Preferences.Instance.goals[Proximate.Kcal] =
            Preferences.Instance.goals[Proximate.Protein] * 4
            + Preferences.Instance.goals[Proximate.Fat] * 9
            + Preferences.Instance.goals[Proximate.Carbs] * 4;
    }


    private void CaloriesToMacros()
    {
        float previousCalories =
            Preferences.Instance.goals[Proximate.Protein] * 4
            + Preferences.Instance.goals[Proximate.Fat] * 9
            + Preferences.Instance.goals[Proximate.Carbs] * 4;

        float multiplier = Preferences.Instance.goals[Proximate.Kcal] / previousCalories;
        Preferences.Instance.goals[Proximate.Protein] *= multiplier;
        Preferences.Instance.goals[Proximate.Fat] *= multiplier;
        Preferences.Instance.goals[Proximate.Carbs] *= multiplier;
    }


    private void UpdateGoalPreferencesButtons()
    {
        Preferences p = Preferences.Instance;

        m_kcalInputField.text = $"{p.goals[Proximate.Kcal]}";
        m_proteinInputField.text = $"{p.goals[Proximate.Protein]}";
        m_fatInputField.text = $"{p.goals[Proximate.Fat]}";
        m_carbsInputField.text = $"{p.goals[Proximate.Carbs]}";
    }
}