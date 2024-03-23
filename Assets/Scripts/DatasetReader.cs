using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
/// This is a pessimistic class which assumes the different files from the dataset are NOT
/// lined up perfectly (even though at first glance it appears they are).
/// 
/// It reads each file one-by-one, putting data into a dictionary by its food code.
/// It then constructs all of the food objects that are valid (no missing fields) and returns
/// them.
/// </summary>
public class DatasetReader
{
    public const int FOOD_ROWS = 2_887; // Dataset is 2890 rows, 3 of which are titles; 2890-3=2887

    // --- Input Data ---
    private ReadOnlyDictionary<DatasetFile, string> m_files;
    private readonly Preferences m_prefs;

    private Dictionary<string, FoodData> m_dataset; // Keys are composite keys representing their corresponding food value uniquely.
    public ReadOnlyDictionary<string, FoodData> Dataset { get; }

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
        Dataset = new(m_dataset);

        static void FillNutrientArray(ENutrient[] array, ENutrient value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

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


    /// <summary>
    /// Reads the dataset into C# objects.
    /// </summary>
    private List<FoodData> ReadFoods()
    {
        foreach (DatasetFile file in m_files.Keys)
        {
            ProcessFile(file);
        }

        return m_dataset.Values.ToList();
    }


    /// <summary>
    /// Converts the dataset into a list of Food objects.
    /// </summary>
    public List<Food> ProcessFoods()
    {
        List<FoodData> foodDatas = ReadFoods();
        List<Food> foods = new();

        foreach (FoodData data in foodDatas)
        {
            // Check the food is allowed.
            if (!m_prefs.IsFoodAllowed(data.FoodGroup, data.Name))
            {
                continue;
            }

            // Check the current food isn't missing essential data (due to N, Tr or "")
            // This missing data is given the value -1, as seen in the delimiter ',' case.

            // Missing data is only permitted if the user has set the preference "accept missing nutrient value"
            // to true for this specific nutrient.
            for (int i = 0; i < Nutrient.Count; i++)
            {
                if (MathTools.Approx(data.Nutrients[i], -1))
                {
                    if (m_prefs.acceptMissingNutrientValue[i])
                    {
                        // User has accepted missing values for this type; set it to NaN to remind the user
                        // that this data point is not to be trusted.
                        data.Nutrients[i] = float.NaN;
                        continue;
                    }
                    goto NextFood;
                }
            }

            Food food = new(data);
            foods.Add(food);

            NextFood: continue;
        }

        Logger.Log(Dataset.Count);

        return foods;
    }


    private void ProcessFile(DatasetFile file)
    {
        const int FIRST_ROW = 3; // The first 3 rows are just titles, so skip them

        FoodData currentFood = new()
        {
            Nutrients = new float[Nutrient.Count]
        };

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

                        HandleColumnLookup(file, ref currentFood, currentWord, currentWordIndex, floatVal);

                        // If the character was a newline, also save and reset the word
                        if (ch == '\n')
                        {
                            // Save
                            if (m_dataset.ContainsKey(currentFood.CompositeKey))
                                m_dataset[currentFood.CompositeKey] = currentFood.MergeWith(m_dataset[currentFood.CompositeKey]);
                            else
                                m_dataset.Add(currentFood.CompositeKey, currentFood);

                            // Reset
                            currentFood = new()
                            {
                                Nutrients = new float[Nutrient.Count]
                            };
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
    /// Looks up which property the column `wordIndex` corresponds to, and updates currentFood accordingly.
    /// </summary>
    /// <param name="file">The file being explored.</param>
    /// <param name="currentFood">Reference to the current food object. Explicitly used 'ref' keyword for clarity.</param>
    /// <param name="currentWord">The unparsed value of the current column.</param>
    /// <param name="wordIndex">Thee column index to lookup.</param>
    /// <param name="floatVal">The parsed value of the current column.</param>
    private void HandleColumnLookup(DatasetFile file, ref FoodData currentFood, string currentWord, int wordIndex, float floatVal)
    {
        switch (wordIndex)
        {
            case 0:
                currentFood.Code = currentWord;
                return;
            case 1:
                currentFood.Name = currentWord;
                return;
            case 2:
                currentFood.Description = currentWord;
                return;
            case 3:
                currentFood.FoodGroup = currentWord;
                return;
            case 5:
                currentFood.Reference = currentWord;
                return;
        }

        HandleNutrientLookup(file, wordIndex, ref currentFood, floatVal);
    }


    /// <summary>
    /// Looks up which nutrient the column `wordIndex` corresponds to. The columns depend on the dataset file
    /// being explored currently.
    /// </summary>
    /// <param name="file">The file being explored.</param>
    /// <param name="wordIndex">The column index to lookup.</param>
    /// <param name="currentFood">The current object to output into (reference type).</param>
    /// <param name="floatVal">The parsed float value of the current column.</param>
    private void HandleNutrientLookup(DatasetFile file, int wordIndex, ref FoodData currentFood, float floatVal)
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
            // The file has trailing empty data commas. So quick exit.
            return;
        }

        if (columns[wordIndex] != (ENutrient)(-1))
            currentFood.Nutrients[(int)columns[wordIndex]] = floatVal;

        return;
    }
}