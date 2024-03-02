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
    private int m_currentPanelIndex = 0;


    private void Awake()
    {
        //
        // Add UI element event listeners for all input fields (buttons are done in Unity editor).
        //
        m_weightGoalDropdown.onValueChanged.AddListener((int value) => OnWeightGoalChanged(value));

        // When the user finishes (whether it be enter, or mouse click out)
        // onEndEdit unifies onSubmit and onDeselect.
        m_weightInputField.onEndEdit.AddListener((string value) => OnWeightInputChanged(value));
        m_heightInputField.onEndEdit.AddListener((string value) => OnHeightInputChanged(value));

        // For each setting UI element, fill in the user's current settings.
        UpdateDietPreferenceButtons();
        UpdateBodyPreferenceButtons();
    }


    /// <summary>
    /// Update the button labels from the Saved preferences so that the enabled
    /// settings are labelled "x" and the disabled settings "".
    /// </summary>
    public void UpdateDietPreferenceButtons()
    {
        Preferences p = Preferences.Instance;

        m_landMeatPrefBtnTxt.text = p.eatsLandMeat ? "x" : "";
        m_seafoodPrefBtnTxt.text = p.eatsSeafood ? "x" : "";
        m_animalProductsPrefBtnTxt.text = p.eatsAnimalProduce ? "x" : "";
        m_lactosePrefBtnTxt.text = p.eatsLactose ? "x" : "";
    }


    private void OnToggleFoodPreference(ref bool pref)
    {
        pref = !pref;
        UpdateDietPreferenceButtons();
        Static.SavePreferences();
    }


    public void OnToggleMeatPreference()
    {
        OnToggleFoodPreference(ref Preferences.Instance.eatsLandMeat);
    }


    public void OnToggleSeafoodPreference()
    {
        OnToggleFoodPreference(ref Preferences.Instance.eatsSeafood);
    }


    public void OnToggleAnimalProducePreference()
    {
        OnToggleFoodPreference(ref Preferences.Instance.eatsAnimalProduce);
    }


    public void OnToggleLactosePreference()
    {
        OnToggleFoodPreference(ref Preferences.Instance.eatsLactose);
    }


    public void UpdateBodyPreferenceButtons()
    {
        Preferences p = Preferences.Instance;

        m_weightGoalDropdown.value = (int)p.weightGoal;
        m_weightInputField.text = $"{p.weightInKG}";
        m_heightInputField.text = $"{p.heightInCM}";
        m_assignedSexText.text = $"Assigned Sex: {p.assignedSex}";
    }


    private void OnBodyPreferenceChanged()
    {
        UpdateBodyPreferenceButtons();
        Static.SavePreferences();
    }


    private void OnWeightGoalChanged(int value)
    {
        Preferences.Instance.weightGoal = (EWeightGoal)value;
        OnBodyPreferenceChanged();
    }


    private void OnWeightInputChanged(string value)
    {
        Preferences.Instance.weightInKG = float.Parse(value);
        Static.SavePreferences();
    }


    private void OnHeightInputChanged(string value)
    {
        Preferences.Instance.heightInCM = float.Parse(value);
        Static.SavePreferences();
    }


    public void OnCycleAssignedSex()
    {
        Preferences.Instance.assignedSex = (EAssignedSex)(1 - (int)Preferences.Instance.assignedSex);
        OnBodyPreferenceChanged();
    }


    public void OnPreferencesNavBtnPressed(bool right)
    {
        Static.OnNavBtnPressed(right, m_preferenceCategoryPanels, ref m_currentPanelIndex);
    }
}