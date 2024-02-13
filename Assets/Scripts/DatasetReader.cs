using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;


public enum DatasetFile
{
    Proximates,
    Inorganics,
    Vitamins
}


public struct FoodData
{
    public string Code;
    public string Name;

    public string CompositeKey
    {
        get { return Code + Group; }
    }

    public string Description;
    public string Group;
    public string Reference;
    public float[] Nutrients;


    public FoodData MergeWith(FoodData other)
    {
        if (other.CompositeKey == CompositeKey)
        {
            for (int i = 0; i < Nutrients.Length; i++)
            {
                // If not initialised in this Nutrients, take it from the other Nutrients.
                if (Mathf.Approximately(Nutrients[i], 0)) Nutrients[i] = other.Nutrients[i];
            }
            return this;
        }

        Debug.Log(other);
        throw new InvalidOperationException($"Could not merge {this.Code} and {other.Code} - they are not compatible!");
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

    private readonly Nutrient[] m_proximateColumns = new Nutrient[47]; // The number of columns with useful data in starting from col 0.
    private readonly Nutrient[] m_inorganicsColumns = new Nutrient[19];
    private readonly Nutrient[] m_vitaminsColumns = new Nutrient[24];


    public DatasetReader(Preferences prefs, string proximatesFile = "Proximates.csv", string inorganicsFile = "Inorganics.csv", string vitaminsFile
        = "Vitamins.csv")
    {
        string proximatesData = File.ReadAllText(Application.dataPath + "/" + proximatesFile);
        string inorganicsData = File.ReadAllText(Application.dataPath + "/" + inorganicsFile);
        string vitaminsData = File.ReadAllText(Application.dataPath + "/" + vitaminsFile);

        m_files = new(new Dictionary<DatasetFile, string>
        {
            { DatasetFile.Proximates, proximatesData },
            { DatasetFile.Inorganics, inorganicsData },
        });
        m_prefs = prefs;
        m_dataset = new();

        FillNutrientArray(m_proximateColumns, (Nutrient)(-1));
        FillNutrientArray(m_inorganicsColumns, (Nutrient)(-1));
        FillNutrientArray(m_vitaminsColumns, (Nutrient)(-1));

        m_proximateColumns[9] = Nutrient.Protein;
        m_proximateColumns[10] = Nutrient.Fat;
        m_proximateColumns[11] = Nutrient.Carbs;
        m_proximateColumns[12] = Nutrient.Kcal;
        m_proximateColumns[16] = Nutrient.Sugar;
        m_proximateColumns[27] = Nutrient.SatFat;
        m_proximateColumns[45] = Nutrient.TransFat;

        m_inorganicsColumns[9] = Nutrient.Calcium;
        m_inorganicsColumns[12] = Nutrient.Iron;
        m_inorganicsColumns[18] = Nutrient.Iodine;
    }


    private void FillNutrientArray(Nutrient[] array, Nutrient value)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = value;
        }
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
            for (int i = 0; i < data.Nutrients.Length; i++)
            {
                if (data.Nutrients[i] < 0)
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

        FoodData currentFood = new();
        currentFood.Nutrients = new float[Nutrients.Count];

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
                            currentFood.Nutrients = new float[Nutrients.Count];
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
    /// <returns>The updated FoodData struct.</returns>
    private FoodData HandleColumnLookup(DatasetFile file, FoodData currentFood, string currentWord, int wordIndex, float floatVal)
    {
        switch (wordIndex)
        {
            case 0:
                currentFood.Code = currentWord;
                return currentFood;
            case 1:
                currentFood.Name = currentWord;
                return currentFood;
            case 2:
                currentFood.Description = currentWord;
                return currentFood;
            case 3:
                currentFood.Group = currentWord;
                return currentFood;
            case 5:
                currentFood.Reference = currentWord;
                return currentFood;
        }

        HandleNutrientLookup(file, wordIndex, currentFood, floatVal);
        return currentFood;
    }


    private FoodData HandleNutrientLookup(DatasetFile file, int wordIndex, FoodData currentFood, float floatVal)
    {
        Nutrient[] columns;

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
            Debug.LogWarning("Word index >= number of columns.");
            return currentFood;
        }

        if (Nutrients.Values.Contains(columns[wordIndex]))
            currentFood.Nutrients[(int)columns[wordIndex]] = floatVal;

        return currentFood;
    }
}