using UnityEngine;

public class SetupBehaviour : MonoBehaviour
{
    /// <summary>
    /// Swaps the boolean value of a preference.
    /// </summary>
    /// <param name="pref">Reference to the preference to set.</param>
    protected static void OnBoolInputChanged(ref bool pref)
    {
        pref = !pref;
        Saving.SavePreferences();
    }


    /// <summary>
    /// Sets the boolean value of a preference.
    /// </summary>
    /// <param name="pref">Reference to the preference to set.</param>
    protected static void OnBoolInputChanged(ref bool pref, bool newValue)
    {
        pref = newValue;
        Saving.SavePreferences();
    }


    /// <summary>
    /// Parses user input, then stores it in the given preference reference.
    /// </summary>
    /// <param name="pref">Reference to the relevant preference (to update).</param>
    /// <param name="value">The unparsed value. Guaranteed to contain a valid float, due to
    /// Unity's input field sanitation. Hence no need for TryParse.</param>
    protected static void OnFloatInputChanged(ref float pref, string value)
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
        Saving.SavePreferences();
    }


    /// <summary>
    /// OnFloatInputChanged, but for an integer preference and input field value.
    /// </summary>
    protected static void OnIntInputChanged(ref int pref, string value)
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
        Saving.SavePreferences();
    }


    private static void PreferenceErrorPopup() => Logger.Warn($"Invalid preference value (must be >= 0).");
}