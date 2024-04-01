using System;
using TMPro;
using UnityEngine;


/// <summary>
/// A Unity script for handling preference changes in a more generic way.
/// </summary>
public class SetupBehaviour : MonoBehaviour
{
    /// <summary>
    /// Whether this class should save preferences when one of its input fields is changed.
    /// </summary>
    protected bool m_saveOnInputChange = true;


    /// <summary>
    /// Swaps the boolean value of a preference.
    /// </summary>
    /// <param name="pref">Reference to the preference to set.</param>
    protected void OnBoolInputChanged(ref bool pref)
    {
        pref = !pref;

        if (m_saveOnInputChange)
            Saving.SavePreferences();
    }


    /// <summary>
    /// Sets the boolean value of a preference.
    /// </summary>
    /// <param name="pref">Reference to the preference to set.</param>
    protected void OnBoolInputChanged(ref bool pref, bool newValue)
    {
        pref = newValue;

        if (m_saveOnInputChange)
            Saving.SavePreferences();
    }


    /// <summary>
    /// Parses user input, then stores it in the given preference reference.
    /// </summary>
    /// <param name="pref">Reference to the relevant preference (to update).</param>
    /// <param name="value">The unparsed value. Guaranteed to contain a valid float, due to
    /// Unity's input field sanitation. Hence no need for TryParse.</param>
    protected void OnFloatInputChanged(ref float pref, string value)
    {
        if (value == "")
            return;

        float newPref = float.Parse(value);
        if (newPref < 0)
        {
            PreferenceErrorPopup();
            return;
        }

        pref = newPref;

        if (m_saveOnInputChange)
            Saving.SavePreferences();
    }


    /// <summary>
    /// OnFloatInputChanged, but for an integer preference and input field value.
    /// </summary>
    protected void OnIntInputChanged(ref int pref, string value)
    {
        if (value == "")
            return;

        int newPref = int.Parse(value);
        if (newPref < 0)
        {
            PreferenceErrorPopup();
            return;
        }

        pref = newPref;

        if (m_saveOnInputChange)
            Saving.SavePreferences();
    }


    /// <summary>
    /// Handles cycling of an array element to the next or previous element in the array.
    /// </summary>
    /// <typeparam name="T">The type of the preference.</typeparam>
    /// <param name="pref">The preference to cycle.</param>
    /// <param name="arr">The array to cycle through.</param>
    /// <param name="right">`true` if going right, `false` if going left.</param>
    protected void OnCycleArray<T>(ref T pref, T[] arr, bool right) 
    {
        pref = ArrayTools.CircularNextElement(arr, Array.IndexOf(arr, pref), right);

        if (m_saveOnInputChange)
            Saving.SavePreferences();
    }


    /// <summary>
    /// Handles cycling of an Enum value to the next or previous value in the
    /// array of possible values of that Enum.
    /// </summary>
    /// <typeparam name="T">The type of Enum.</typeparam>
    /// <param name="pref">The enum preference to cycle.</param>
    /// <param name="right">`true` if going right, `false` if going left.</param>
    protected void OnCycleEnum<T>(ref T pref, bool right) where T : Enum
    {
        OnCycleArray(ref pref, (T[])Enum.GetValues(typeof(T)), right);
    }


    /// <summary>
    /// Calls OnCycleArray, then updates `textToUpdate` with the resulting preference value.
    /// </summary>
    /// <param name="textToUpdate">The text component to update.</param>
    protected void OnCycleArrayWithLabel<T>(ref T pref, T[] arr, bool right, TMP_Text textToUpdate)
    {
        OnCycleArray(ref pref, arr, right);
        textToUpdate.text = pref.ToString();
    }


    /// <summary>
    /// Calls OnCycleEnum, then updates `textToUpdate` with the resulting preference value.
    /// </summary>
    /// <param name="textToUpdate">The text component to update.</param>
    protected void OnCycleEnumWithLabel<T>(ref T pref, bool right, TMP_Text textToUpdate) where T : Enum
    {
        OnCycleEnum(ref pref, right);
        textToUpdate.text = pref.ToString();
    }


    private static void PreferenceErrorPopup() => Logger.Warn($"Invalid preference value (must be >= 0).");
}