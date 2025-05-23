// Commented 20/4
using System;
using TMPro;
using UnityEngine;


/// <summary>
/// An abstract Unity script for handling UI-based setup, covering type-based input fields and
/// toggle/cycle button helper methods.
/// </summary>
public abstract class SetupBehaviour : MonoBehaviour
{
    /// <summary>
    /// Whether this instance should save preferences when one of its input fields is changed.
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
    /// <param name="newValue">The new boolean value to set pref to.</param>
    protected void OnBoolInputChanged(ref bool pref, bool newValue)
    {
        pref = newValue;

        if (m_saveOnInputChange)
            Saving.SavePreferences();
    }


    /// <summary>
    /// Parses user input, then stores it in the given preference reference. Only accepts
    /// positive values, so that error handling is greatly simplified.
    /// </summary>
    /// <param name="pref">Reference to the relevant preference (to update).</param>
    /// <param name="value">The unparsed value. Guaranteed to contain a valid float, due to
    /// Unity's input field sanitation. Hence no need for TryParse.</param>
    protected void OnFloatInputChanged(ref float pref, string value)
    {
        // Reject empty input
        if (value == "")
            return;

        // Floats below 0 are rejected by this method.
        float newPref = float.TryParse(value, out newPref) ? newPref : -1;
        if (newPref < 0)
        {
            PreferenceErrorPopup();
            return;
        }

        // Update the preference, if an error hasn't occurred.
        pref = newPref;

        if (m_saveOnInputChange)
            Saving.SavePreferences();
    }


    /// <summary>
    /// OnFloatInputChanged, but for an integer preference and input field value.
    /// </summary>
    protected void OnIntInputChanged(ref int pref, string value)
    {
        // Reject empty input
        if (value == "")
            return;

        // Ints below 0 are rejected by this method.
        int newPref = int.TryParse(value, out newPref) ? newPref : -1;
        if (newPref < 0)
        {
            PreferenceErrorPopup();
            return;
        }

        // Update the preference, if an error hasn't occurred.
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


    //
    // UI helper methods
    //

    /// <summary>
    /// Handles a boolean toggle button being pressed.
    /// </summary>
    /// <param name="pref">Reference to its preference to toggle.</param>
    /// <param name="textToUpdate">The preference indicator to update with "x" if it is now `true` or "" if `false`.</param>
    protected void OnToggleBtnPressed(ref bool pref, TMP_Text textToUpdate)
    {
        OnBoolInputChanged(ref pref);
        textToUpdate.text = pref ? "x" : "";
    }


    /// <summary>
    /// Shorthand for displaying a generic preference error popup.
    /// </summary>
    private static void PreferenceErrorPopup() => Logger.Warn($"Invalid preference value (must be non-negative and not excessively large).");
}