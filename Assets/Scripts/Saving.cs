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
            Logger.Warn("Unable to save to Preferences.json. Please ensure you don't have it open in a text editor.");
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
            Logger.Warn("Unable to load Preferences.json. Please ensure you don't have it open in a text editor.");
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
        if (!File.Exists(dest)) File.Create(dest).Close();

        // If the provided object is null, delete the file.
        if (serializable == null)
        {
            File.Delete(dest);
            return;
        }

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
            string json = File.ReadAllText(dest);
            T val = (T)JsonUtility.FromJson(json, typeof(T));

            if (val is ICached cached)
                cached.Cache();

            return val;
        }
        else
        {
            // Restart the function after creating a new T save
            SaveToFile(new T(), filename);
            Logger.Log("File does not exist! Returning empty object");
            return LoadFromFile<T>(filename);
        }
    }
}