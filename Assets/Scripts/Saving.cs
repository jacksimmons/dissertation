using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.VisualScripting;
using UnityEngine;


public interface ICached
{
    /// <summary>
    /// Saves the instance as the .Saved static member variable for its class.
    /// This occurs during saving and loading to files.
    /// </summary>
    public void Cache();
}


/// <summary>
/// A static class for saving generic [Serializable] types.
/// 
/// References:
/// This is a modified version of a Saving class I used in an unpublished project.
/// </summary>
public static class Saving
{
    /// <summary>
    /// Serialises objects and saves them to a given file location.
    /// </summary>
    public static void SaveToFile<T>(T serializable, string filename)
    {
        Type type = serializable.GetType();
        if (!type.IsSerializable)
        {
            Debug.LogError("Provided object is not serializable, so cannot be saved.");
            return;
        }

        string dest = Application.dataPath + "/" + filename;
        FileStream fs;

        if (File.Exists(dest)) fs = File.OpenWrite(dest);
        else fs = File.Create(dest);

        BinaryFormatter bf = new();
        bf.Serialize(fs, serializable);
        fs.Close();

        // If the type is cache-able, cache it. (Full explanation in ICached).
        if (serializable is ICached cached)
        {
            cached.Cache();
        }

        Debug.Log(filename);
    }

    /// <summary>
    /// Deserialises a serialised object stored in a file.
    /// </summary>
    public static T LoadFromFile<T>(string filename) where T : new()
    {
        string dest = Application.dataPath + "/" + filename;
        FileStream fs;

        if (File.Exists(dest))
        {
            Debug.Log(dest);
            fs = File.OpenRead(dest);
            BinaryFormatter bf = new();

            T val = (T)bf.Deserialize(fs);
            fs.Close();

            // If the type is cache-able, cache it. (Full explanation in ICached).
            if (val is ICached cached)
            {
                cached.Cache();
            }

            return val;
        }
        else
        {
            // Restart the function after creating a new T save
            SaveToFile(new T(), filename);
            Debug.LogError("File does NOT exist! Returning empty object");
            return LoadFromFile<T>(filename);
        }
    }
}