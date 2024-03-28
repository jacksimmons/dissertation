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


    private static void PreferenceErrorPopup() => Logger.Warn($"Invalid preference value (must be >= 0).");
}