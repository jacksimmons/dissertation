using System.Collections.Generic;
using TMPro;
using UnityEngine;


/// <summary>
/// Menu handling implementation for the Preferences menu.
/// </summary>
public partial class MenuHandler
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
    private GameObject[] m_preferenceCategoryPanels;
    private int m_currentPreferenceCategoryIndex = 0;


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


    private void OnToggleFoodPreference()
    {
        UpdateDietPreferenceButtons();
        Saving.SaveToFile(Preferences.Saved, "Preferences.dat");
    }


    public void OnToggleMeatPreference()
    {
        Preferences.Saved.eatsLandMeat = !Preferences.Saved.eatsLandMeat;
        OnToggleFoodPreference();
    }


    public void OnToggleSeafoodPreference()
    {
        Preferences.Saved.eatsSeafood = !Preferences.Saved.eatsSeafood;
        OnToggleFoodPreference();
    }


    public void OnToggleAnimalProducePreference()
    {
        Preferences.Saved.eatsAnimalProduce = !Preferences.Saved.eatsAnimalProduce;
        OnToggleFoodPreference();
    }


    public void OnToggleLactosePreference()
    {
        Preferences.Saved.eatsLactose = !Preferences.Saved.eatsLactose;
        OnToggleFoodPreference();
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

        int next = right ? m_currentPreferenceCategoryIndex + 1
                         : m_currentPreferenceCategoryIndex - 1;

        // Assign to the category index, wrapping around the array if necessary
        int temp = next < 0 ? m_preferenceCategoryPanels.Length - 1 : next;
        m_currentPreferenceCategoryIndex = temp > m_preferenceCategoryPanels.Length - 1 ? 0 : temp;

        m_preferenceCategoryPanels[m_currentPreferenceCategoryIndex].SetActive(true);
    }
}