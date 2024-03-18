using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

#if UNITY_64
using UnityEngine;
#endif


public enum DatasetFile
{
    Proximates,
    Inorganics,
    Vitamins
}


/// <summary>
/// All of the data associated with a food in the dataset.
/// All nutrients are measured for a 100g portion.
/// </summary>
public class FoodData
{
    public string code;
    public string name;

    public string CompositeKey
    {
        get { return code + foodGroup; }
    }

    public string description;
    public string foodGroup;
    public float[] nutrients;


    public FoodData()
    {
        nutrients = new float[Nutrient.Count];
    }


    public FoodData MergeWith(FoodData other)
    {
        if (other.CompositeKey == CompositeKey)
        {
            for (int i = 0; i < Nutrient.Count; i++)
            {
                // If not initialised in this nutrients, take it from the other nutrients.
                if (MathfTools.Approx(nutrients[i], 0)) nutrients[i] = other.nutrients[i];
            }
            return this;
        }

        Logger.Log(other);
        throw new InvalidOperationException($"Could not merge {code} and {other.code} - they are not compatible!");
    }
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

    private Dictionary<string, FoodData> m_dataset; // Keys are composite keys representing their corresponding food value uniquely.

    private readonly ENutrient[] m_proximateColumns = new ENutrient[47]; // The number of columns with useful data in starting from col 0.
    private readonly ENutrient[] m_inorganicsColumns = new ENutrient[19];
    private readonly ENutrient[] m_vitaminsColumns = new ENutrient[24];

    public readonly string SetupError = "";


    public DatasetReader(Preferences prefs, string proximatesFile = "Proximates", string inorganicsFile = "Inorganics", string vitaminsFile
        = "Vitamins")
    {
        string proximatesData;
        string inorganicsData;
        string vitaminsData;

        try
        {
            // Load data files through Unity (this function only works with no file extension)
#if UNITY_64
            proximatesData = Resources.Load<TextAsset>(proximatesFile).text;
            inorganicsData = Resources.Load<TextAsset>(inorganicsFile).text;
            vitaminsData = Resources.Load<TextAsset>(vitaminsFile).text;
#else
            proximatesData = File.ReadAllText(FileTools.GetProjectDirectory() + "Proximates.csv");
            inorganicsData = File.ReadAllText(FileTools.GetProjectDirectory() + "Inorganics.csv");
            vitaminsData = File.ReadAllText(FileTools.GetProjectDirectory() + "Vitamins.csv");
#endif
        }
        // Catch any sharing violation errors, permission errors, etc.
        catch (Exception e)
        {
            SetupError = $"Message:\n{e.Message}\nStack Trace:\n{e.StackTrace}";
            return;
        }

        m_files = new(new Dictionary<DatasetFile, string>
        {
            { DatasetFile.Proximates, proximatesData },
            { DatasetFile.Inorganics, inorganicsData },
            { DatasetFile.Vitamins,   vitaminsData },
        });
        m_prefs = prefs;
        m_dataset = new();

        FillNutrientArray(m_proximateColumns, (ENutrient)(-1));
        FillNutrientArray(m_inorganicsColumns, (ENutrient)(-1));
        FillNutrientArray(m_vitaminsColumns, (ENutrient)(-1));

        m_proximateColumns[9] = ENutrient.Protein;
        m_proximateColumns[10] = ENutrient.Fat;
        m_proximateColumns[11] = ENutrient.Carbs;
        m_proximateColumns[12] = ENutrient.Kcal;
        m_proximateColumns[16] = ENutrient.Sugar;
        m_proximateColumns[27] = ENutrient.SatFat;
        m_proximateColumns[45] = ENutrient.TransFat;

        m_inorganicsColumns[9] = ENutrient.Calcium;
        m_inorganicsColumns[12] = ENutrient.Iron;
        m_inorganicsColumns[18] = ENutrient.Iodine;

        m_vitaminsColumns[9] = ENutrient.VitA; // "Retinol equivalent"
        m_vitaminsColumns[13] = ENutrient.VitB1;
        m_vitaminsColumns[14] = ENutrient.VitB2;
        m_vitaminsColumns[17] = ENutrient.VitB3; // "Niacin equivalent"
        m_vitaminsColumns[18] = ENutrient.VitB6;
        m_vitaminsColumns[20] = ENutrient.VitB9;
        m_vitaminsColumns[19] = ENutrient.VitB12;
        m_vitaminsColumns[23] = ENutrient.VitC;
        m_vitaminsColumns[10] = ENutrient.VitD;
        m_vitaminsColumns[11] = ENutrient.VitE;
        m_vitaminsColumns[12] = ENutrient.VitK1;
    }


    private void FillNutrientArray(ENutrient[] array, ENutrient value)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = value;
        }
    }


    /// <summary>
    /// This method converts the dataset into a list of Food objects.
    /// </summary>
    public List<Food> ProcessFoods()
    {
        foreach (DatasetFile file in m_files.Keys)
        {
            ProcessFile(file);
        }

        List<Food> foods = new();
        foreach (FoodData data in m_dataset.Values)
        {
            // Check the food is allowed.
            if (!m_prefs.IsFoodGroupAllowed(new(data)))
                continue;

            // Check the current food isn't missing essential data (due to N, Tr or "")
            // This missing data is given the value -1, as seen in the delimiter ',' case.
            for (int i = 0; i < Nutrient.Count; i++)
            {
                if (data.nutrients[i] < 0)
                    goto NextFood;
            }

            Food food = new(data);
            foods.Add(food);

            NextFood: continue;
        }

        return foods;
    }


    private void ProcessFile(DatasetFile file)
    {
        const int FIRST_ROW = 3; // The first 3 rows are just titles, so skip them

        FoodData currentFood = new();
        currentFood.nutrients = new float[Nutrient.Count];

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
                case '\r':
                    break;
                case '\n':
                case ',': // We have finished the current word.
                          // If in speech marks, we have not yet finished the current word.
                          // Comma and Newline both mark the end of a word; they differ in what happens next.
                          // - Comma just starts a new word (new food property)
                          // - Newline starts a new word AND new row (new food entirely)
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

                        currentFood = HandleColumnLookup(file, currentFood, currentWord, currentWordIndex, floatVal);

                        // If the character was a newline, also save and reset the word
                        if (ch == '\n')
                        {
                            // Save
                            if (m_dataset.ContainsKey(currentFood.CompositeKey))
                                m_dataset[currentFood.CompositeKey] = currentFood.MergeWith(m_dataset[currentFood.CompositeKey]);
                            else
                                m_dataset.Add(currentFood.CompositeKey, currentFood);

                            // Reset
                            currentFood = new();
                            currentFood.nutrients = new float[Nutrient.Count];
                        }
                    }

                    if (ch == '\n')
                    {
                        // Reset word, word counter and increment row counter if moving to a new row
                        currentWord = "";
                        currentWordIndex = 0;
                        currentRowIndex++;
                    }
                    else
                    {
                        // Reset for the next word
                        currentWord = "";
                        currentWordIndex++;
                    }
                    break;

                default: // Regular character, i.e. part of the next value
                    currentWord += ch;
                    break;
            }
        }
    }


    /// <summary>
    /// </summary>
    /// <returns>The updated FoodData.</returns>
    private FoodData HandleColumnLookup(DatasetFile file, FoodData currentFood, string currentWord, int wordIndex, float floatVal)
    {
        switch (wordIndex)
        {
            case 0:
                currentFood.code = currentWord;
                return currentFood;
            case 1:
                currentFood.name = currentWord;
                return currentFood;
            case 2:
                currentFood.description = currentWord;
                return currentFood;
            case 3:
                currentFood.foodGroup = currentWord;
                return currentFood;
            //case 5:
            //    currentFood.reference = currentWord;
            //    return currentFood;
        }

        HandleNutrientLookup(file, wordIndex, currentFood, floatVal);
        return currentFood;
    }


    private FoodData HandleNutrientLookup(DatasetFile file, int wordIndex, FoodData currentFood, float floatVal)
    {
        ENutrient[] columns;

        switch (file)
        {
            case DatasetFile.Proximates:
                columns = m_proximateColumns;
                break;
            case DatasetFile.Inorganics:
                columns = m_inorganicsColumns;
                break;
            default:
                columns = m_vitaminsColumns;
                break;
        }

        if (wordIndex >= columns.Length)
        {
            // The file, for some reason, has trailing empty data commas. So quick exit.
            return currentFood;
        }

        if (columns[wordIndex] != (ENutrient)(-1))
            currentFood.nutrients[(int)columns[wordIndex]] = floatVal;

        return currentFood;
    }
}