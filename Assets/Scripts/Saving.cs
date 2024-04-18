// Commented 17/4
using System.IO;
using UnityEngine;


/// <summary>
/// A static class for saving generic serialisable types.
/// It serialises/deserialises using a JSON formatter, and stores them at the
/// persistent data path on your computer (%APPDATA%/../LocalLow/JackSimmons/Dissertation/{FileName})
/// </summary>
public static class Saving
{
    /// <summary>
    /// Saves the current static Preferences instance to disk.
    /// </summary>
    public static void SavePreferences()
    {
        try
        {
            SaveToFile(Preferences.Instance, "Preferences.json");
            Logger.Log("Saved preferences.");
        }
        catch
        {
            Logger.Warn("Unable to save to Preferences.json. Please ensure you don't have it open in a text editor"
			+ $" and that this program has read/write access at {Application.persistentDataPath + "/Preferences.json"}");
        }
    }


    /// <summary>
    /// Loads preferences from disk into the static Preferences instance.
    /// </summary>
    public static Preferences LoadPreferences()
    {
        try
        {
            Preferences p = LoadFromFile<Preferences>("Preferences.json");
            Logger.Log("Loaded preferences.");
            return p;
        }
        catch
        {
            Logger.Warn("Unable to load Preferences.json. Please ensure you don't have it open in a text editor"
			+ $" and that this program has read/write access at {Application.persistentDataPath + "/Preferences.json"}");
            return new();
        }
    }


    /// <summary>
    /// Serialises objects and saves them to a given file location.
    /// Also calls .Cache() on the object beforehand if it : ICached.
    /// </summary>
    private static void SaveToFile<T>(T serializable, string filename)
    {
        if (serializable is ICached cached)
            cached.Cache();

        string dest = Application.persistentDataPath + "/" + filename;
        
		// Ensure first that the file exists.
		if (!File.Exists(dest)) File.Create(dest).Close();

        // If the provided object is null, delete the file.
        if (serializable == null)
        {
            File.Delete(dest);
            return;
        }

		// Save the file using Unity's JSON serialisation.
        string json = JsonUtility.ToJson(serializable, true);
        File.WriteAllText(dest, json);
    }

    /// <summary>
    /// Deserialises a serialised object stored in a file.
    /// Calls .Cache() on the object if it : ICached.
    /// </summary>
    private static T LoadFromFile<T>(string filename) where T : new()
    {
        string dest = Application.persistentDataPath + "/" + filename;

        if (File.Exists(dest))
        {
			// Read the file using Unity's JSON deserialisation.
            string json = File.ReadAllText(dest);
            T val = (T)JsonUtility.FromJson(json, typeof(T));

			// Cache the instance for utility.
            if (val is ICached cached)
                cached.Cache();

            return val;
        }
        else
        {
            // Restart the function after creating a new T save
            SaveToFile(new T(), filename);
            Logger.Log("Couldn't find Preferences file; saved a new one.");
            return LoadFromFile<T>(filename);
        }
    }
}