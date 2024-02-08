using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;


public enum DatasetFile
{
    Proximates,
    Inorganics,
    Vitamins
}


public struct FoodData
{
    public string Name;
    public string Description;
    public string Group;
    public string Reference;
    public Dictionary<Nutrient, float> Nutrients;
}


/// <summary>
/// This is a pessimistic class which assumes the different files from the dataset are NOT
/// lined up perfectly (even though at first glance it appears they are).
/// 
/// It reads each file one-by-one, putting data into a dictionary by its food code.
/// It then constructs all of the food objects that are valid (no missing fields) and returns
/// them.
/// </summary>
public class DatasetReader
{
    // --- Input Data ---
    private ReadOnlyDictionary<DatasetFile, string> m_files;
    private readonly Preferences m_prefs;


    private Dictionary<string, FoodData> m_dataset;


    public DatasetReader(Dictionary<DatasetFile, string> csvFiles, Preferences prefs)
    {
        m_files = new(csvFiles);
        m_prefs = prefs;
    }


    /// <summary>
    /// This method converts the dataset into a list of Food objects.
    /// </summary>
    public List<Food> ReadFoods()
    {
        foreach (DatasetFile file in m_files.Keys)
        {
            ReadFile(file);
        }


        List<Food> foods = new();
        foreach (FoodData data in m_dataset.Values)
        {
            // Check the food is allowed.
            if (!m_prefs.IsFoodGroupAllowed(data.Group))
                continue;

            // Check the current food isn't missing essential data (due to N, Tr or "")
            // This missing data is given the value -1, as seen in the delimiter ',' case.
            for (int i = 0; i < Nutrients.Count; i++)
            {
                if (data.Nutrients[(Nutrient)i] < 0)
                    goto NextFood;
            }

            Food food = new(data);
            foods.Add(food);

            NextFood: continue;
        }


        return foods;
    }


    private void ReadFile(DatasetFile file)
    {
        const int FIRST_ROW = 3; // The first 3 rows are just titles, so skip them

        string currentFoodCode = "";
        string currentWord = "";

        int currentWordIndex = 0;
        int currentRowIndex = 0;

        bool speechMarkOpened = false;


        for (int i = 0; i < m_files[file].Length; i++)
        {
            char ch = m_files[file][i];
            switch (ch)
            {
                case '\"':
                    // Permits any character to be added to the word, even ',' or '\n'.
                    // The speech marks themselves will be ignored.
                    speechMarkOpened = !speechMarkOpened;
                    break;

                case ',': // Delimiter, move onto the next word
                          // If in speech marks, cancel the delimiting
                    if (speechMarkOpened)
                    {
                        currentWord += ch;
                        break;
                    }

                    // Parses the word into a float, if possible.
                    // Default value - "" or "N" get a value of -1.
                    if (!float.TryParse(currentWord, out float floatVal))
                        floatVal = -1;

                    // If the word is just a title, ignore it
                    if (currentRowIndex >= FIRST_ROW)
                    {
                        // Trace values are given a value of 0
                        if (currentWord == "Tr")
                            floatVal = 0;

                        currentFoodCode = HandleLookupTable(file, currentFoodCode, currentWord, currentWordIndex, floatVal);
                    }

                    // Reset for the next word
                    currentWord = "";
                    currentWordIndex++;
                    break;

                case '\n': // New line
                           // If in speech marks, ignore new row
                    if (speechMarkOpened)
                    {
                        currentWord += ch;
                        break;
                    }

                    // Otherwise, reset the word data and increment the row.
                    currentFoodCode = "";
                    currentWord = "";
                    currentWordIndex = 0;
                    currentRowIndex++;
                    break;

                default: // Regular character, i.e. part of the next value
                    currentWord += ch;
                    break;
            }
        }
    }


    /// <summary>
    /// </summary>
    /// <param name="file"></param>
    /// <param name="code"></param>
    /// <param name="currentWord"></param>
    /// <param name="wordIndex"></param>
    /// <param name="floatVal"></param>
    /// <returns>The food code provided to it, or the correct food code (if the current field is food code).</returns>
    private string HandleLookupTable(DatasetFile file, string code, string currentWord, int wordIndex, float floatVal)
    {
        // For string fields, just assign the value.
        // For float or int fields, we can safely parse the string, as
        // the only string values these fields can have are checked
        // in the containing if-clause.

        FoodData data = new();
        if (code != "")
            data = m_dataset[code];

        switch (file)
        {
            case DatasetFile.Proximates:
                switch (wordIndex)
                {
                    case 0:
                        m_dataset[currentWord] = data;
                        data.Nutrients = new();
                        code = currentWord;
                        break;
                    case 1:
                        data.Name = currentWord;
                        break;
                    case 2:
                        data.Description = currentWord;
                        break;
                    case 3:
                        data.Group = currentWord;
                        break;
                    case 5:
                        data.Reference = currentWord;
                        break;
                    case 9:
                        data.Nutrients[Nutrient.Protein] = floatVal; break;
                    case 10:
                        data.Nutrients[Nutrient.Fat] = floatVal; break;
                    case 11:
                        data.Nutrients[Nutrient.Carbs] = floatVal; break;
                    case 12:
                        data.Nutrients[Nutrient.Kcal] = floatVal; break;
                    case 16:
                        data.Nutrients[Nutrient.Sugar] = floatVal; break;
                    case 27:
                        data.Nutrients[Nutrient.SatFat] = floatVal; break;
                    case 45:
                        data.Nutrients[Nutrient.TransFat] = floatVal; break;
                }
                break;
        }
        return code;
    }
}