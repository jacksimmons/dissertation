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
        // For each setting UI element, fill in the user's current settings.
        UpdateDietPreferenceButtons();
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
        Saving.SavePreferences();
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


    public void OnPreferencesNavBtnPressed(bool right)
    {
        UITools.OnNavBtnPressed(right, m_preferenceCategoryPanels, ref m_currentPanelIndex);
    }
}