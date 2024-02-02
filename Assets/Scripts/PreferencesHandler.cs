using TMPro;
using UnityEngine;

public class PreferencesHandler : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI m_landMeatPrefBtnTxt;
    [SerializeField]
    private TextMeshProUGUI m_seafoodPrefBtnTxt;
    [SerializeField]
    private TextMeshProUGUI m_animalProductsPrefBtnTxt;
    [SerializeField]
    private TextMeshProUGUI m_lactosePrefBtnTxt;
    [SerializeField]
    private TMP_Dropdown m_weightGoalDropdown;
    [SerializeField]
    private TMP_InputField m_weightInputField;
    [SerializeField]
    private TMP_InputField m_heightInputField;
    [SerializeField]
    private TextMeshProUGUI m_assignedSexText;

    [SerializeField]
    private GameObject[] m_preferenceCategoryPanels;
    private int m_currentPreferenceCategoryIndex = 0;


    private void Awake()
    {
        // Add UI element event listeners.
        m_weightGoalDropdown.onValueChanged.AddListener((int value) => OnWeightGoalChanged(value));
        m_weightInputField.onSubmit.AddListener((string value) => OnWeightInputChanged(value));
        m_heightInputField.onSubmit.AddListener((string value) => OnHeightInputChanged(value));

        // For each setting UI element, fill in the user's current settings.
        UpdateDietPreferenceButtons();
        UpdateBodyPreferenceButtons();
    }


    public static bool ParseDecimalInputField(string value, ref float prop, TMP_InputField inputField)
    {
        if (float.TryParse(value, out float floatVal))
        {
            if (floatVal <= 0)
            {
                inputField.text = "Value must be greater than zero.";
            }

            prop = floatVal;
            return true;
        }
        else
        {
            inputField.text = "Not a valid decimal!";
        }
        return false;
    }


    public static void SavePreferences()
    {
        Saving.SaveToFile(Preferences.Saved, "Preferences.dat");
    }


    /// <summary>
    /// Update the button labels from the Saved preferences so that the enabled
    /// settings are labelled "x" and the disabled settings "".
    /// </summary>
    public void UpdateDietPreferenceButtons()
    {
        Preferences p = Preferences.Saved;

        m_landMeatPrefBtnTxt.text = p.eatsLandMeat ? "x" : "";
        m_seafoodPrefBtnTxt.text = p.eatsSeafood ? "x" : "";
        m_animalProductsPrefBtnTxt.text = p.eatsAnimalProduce ? "x" : "";
        m_lactosePrefBtnTxt.text = p.eatsLactose ? "x" : "";
    }


    private void OnToggleFoodPreference(ref bool pref)
    {
        pref = !pref;
        UpdateDietPreferenceButtons();
        SavePreferences();
    }


    public void OnToggleMeatPreference()
    {
        OnToggleFoodPreference(ref Preferences.Saved.eatsLandMeat);
    }


    public void OnToggleSeafoodPreference()
    {
        OnToggleFoodPreference(ref Preferences.Saved.eatsSeafood);

    }


    public void OnToggleAnimalProducePreference()
    {
        OnToggleFoodPreference(ref Preferences.Saved.eatsAnimalProduce);
    }


    public void OnToggleLactosePreference()
    {
        OnToggleFoodPreference(ref Preferences.Saved.eatsLactose);
    }


    public void UpdateBodyPreferenceButtons()
    {
        Preferences p = Preferences.Saved;

        m_weightGoalDropdown.value = (int)p.weightGoal;
        m_weightInputField.text = $"{p.weightInKG}";
        m_heightInputField.text = $"{p.heightInCM}";
        m_assignedSexText.text = $"Assigned Sex: {p.assignedSex}";
    }


    private void OnBodyPreferenceChanged()
    {
        UpdateBodyPreferenceButtons();
        SavePreferences();
    }


    private void OnWeightGoalChanged(int value)
    {
        Preferences.Saved.weightGoal = (WeightGoal)value;
        OnBodyPreferenceChanged();
    }


    private void OnWeightInputChanged(string value)
    {
        if (ParseDecimalInputField(value, ref Preferences.Saved.weightInKG, m_weightInputField))
            SavePreferences();
    }


    private void OnHeightInputChanged(string value)
    {
        if (ParseDecimalInputField(value, ref Preferences.Saved.heightInCM, m_heightInputField))
            SavePreferences();
    }


    public void OnCycleAssignedSex()
    {
        Preferences.Saved.assignedSex = (AssignedSex)(1 - (int)Preferences.Saved.assignedSex);
        OnBodyPreferenceChanged();
    }


    private int NextArrayIndex(int current, int length, bool right)
    {
        if (right)
        {
            // Going right
            if (current + 1 >= length) return 0;
            return current + 1;
        }

        // Going left
        if (current - 1 < 0) return length - 1;
        return current - 1;
    }


    /// <summary>
    /// When the left/right button is pressed in the preferences menu (to navigate
    /// between preference categories).
    /// Disables the previous panel, and enables the new one.
    /// </summary>
    /// <param name="right">`true` if the right button was pressed, `false` if the
    /// left button was pressed.</param>
    public void OnPreferencesMenuNavBtnPressed(bool right)
    {
        m_preferenceCategoryPanels[m_currentPreferenceCategoryIndex].SetActive(false);

        m_currentPreferenceCategoryIndex =
            NextArrayIndex(m_currentPreferenceCategoryIndex, m_preferenceCategoryPanels.Length, right);

        m_preferenceCategoryPanels[m_currentPreferenceCategoryIndex].SetActive(true);
    }
}