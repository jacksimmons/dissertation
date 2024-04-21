// Commented 17/4
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;


/// <summary>
/// Enum representing a file that can be parsed by the reader.
/// </summary>
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
/// It reads the files "in parallel", one line at a time. This means that each of the file
/// streams have the next line (up to a '\n') read and the read pointer incremented, before
/// moving onto the next line.
/// </summary>
public sealed class DatasetReader
{
    private const int FIRST_ROW = 3; // The first 3 rows are just titles, so skip them
    public const int FOOD_ROWS = 2_887; // Dataset is 2890 rows, 3 of which are titles; 2890-3=2887
    private const int TOTAL_ROWS = FIRST_ROW + FOOD_ROWS;

    // --- Input Data ---
    private ReadOnlyCollection<string> m_files;
    private readonly Preferences m_prefs;

	// --- Parsed Data ---
    private List<FoodData> m_dataset;
    public ReadOnlyCollection<FoodData> Dataset { get; }

	// These arrays store an element for each column in their corresponding dataset file.
	// Each element is a named EConstraintType value, if the column corresponds to desired data.
	// Otherwise (e.g. it is not useful data), it is set to the expression: (EConstraintType)-1.
    private readonly EConstraintType[] m_proximateColumns = new EConstraintType[47]; // The number of columns with useful data in starting from col 0.
    private readonly EConstraintType[] m_inorganicsColumns = new EConstraintType[19];
    private readonly EConstraintType[] m_vitaminsColumns = new EConstraintType[24];


    public DatasetReader(Preferences prefs, string proximatesFile = "Proximates", string inorganicsFile = "Inorganics", string vitaminsFile
        = "Vitamins")
    {
        string proximatesData;
        string inorganicsData;
        string vitaminsData;

        try
        {
            // Load data files through Unity (this function only works with no file extension)
            proximatesData = Resources.Load<TextAsset>(proximatesFile).text;
            inorganicsData = Resources.Load<TextAsset>(inorganicsFile).text;
            vitaminsData = Resources.Load<TextAsset>(vitaminsFile).text;
        }
        catch
        {
			// Catch any sharing violation errors, permission errors, etc.
            Logger.Warn($"Unable to read one of the dataset files. Please ensure the file isn't open, and that"
			+ " this program has read access at {proximatesFile}");
            return;
        }

		// Initialise data structures
        string[] files = new string[] { proximatesData, inorganicsData, vitaminsData };
        m_files = new(files);
        m_prefs = prefs;
        m_dataset = new();
        Dataset = new(m_dataset);

        static void FillNutrientArray(EConstraintType[] array, EConstraintType value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

		// ----- Fill out the arrays which identify useful columns -----
        FillNutrientArray(m_proximateColumns, (EConstraintType)(-1));
        FillNutrientArray(m_inorganicsColumns, (EConstraintType)(-1));
        FillNutrientArray(m_vitaminsColumns, (EConstraintType)(-1));

        m_proximateColumns[9] = EConstraintType.Protein;
        m_proximateColumns[10] = EConstraintType.Fat;
        m_proximateColumns[11] = EConstraintType.Carbs;
        m_proximateColumns[12] = EConstraintType.Kcal;
        m_proximateColumns[16] = EConstraintType.Sugar;
        m_proximateColumns[27] = EConstraintType.SatFat;
        m_proximateColumns[45] = EConstraintType.TransFat;

        m_inorganicsColumns[9] = EConstraintType.Calcium;
        m_inorganicsColumns[12] = EConstraintType.Iron;
        m_inorganicsColumns[18] = EConstraintType.Iodine;

        m_vitaminsColumns[9] = EConstraintType.VitA; // "Retinol equivalent"
        m_vitaminsColumns[13] = EConstraintType.VitB1;
        m_vitaminsColumns[14] = EConstraintType.VitB2;
        m_vitaminsColumns[17] = EConstraintType.VitB3; // "Niacin equivalent"
        m_vitaminsColumns[18] = EConstraintType.VitB6;
        m_vitaminsColumns[20] = EConstraintType.VitB9;
        m_vitaminsColumns[19] = EConstraintType.VitB12;
        m_vitaminsColumns[23] = EConstraintType.VitC;
        m_vitaminsColumns[10] = EConstraintType.VitD;
        m_vitaminsColumns[11] = EConstraintType.VitE;
        m_vitaminsColumns[12] = EConstraintType.VitK1;
		// ----- END -----
    }


    /// <summary>
    /// Processes the dataset into a list of Food objects, by checking which foods in the dataset
    /// are allowed by the user's preferences.
    /// <param name="fileLength">The length (in rows) of the files (their lengths must be equal).</param>
    /// </summary>
    public List<Food> ProcessFoods(int fileLength = TOTAL_ROWS)
    {
        ProcessFiles(fileLength);
        List<Food> foods = new();

        foreach (FoodData data in m_dataset)
        {
            // Check the food is allowed.
            if (!m_prefs.IsFoodAllowed(data.FoodGroup, data.Name, data.CompositeKey))
            {
                continue;
            }

            // Check the current food isn't missing essential data (due to N, Tr or "")
            // This missing data is given the value -1, as seen in the delimiter ',' case.

            // Missing data is only permitted if the user has set the preference "accept missing nutrient value"
            // to true for this specific nutrient.
            for (int i = 0; i < Constraint.Count; i++)
            {
                if (MathTools.ApproxLessThan(data.Nutrients[i], 0))
                {
                    if (m_prefs.acceptMissingNutrientValue[i])
                    {
                        // User has opted to ignore missing values for this nutrient.
                        data.Nutrients[i] = -1;
                        continue;
                    }
                    goto NextFood;
                }
            }

            Food food = new(data);
            foods.Add(food);

        NextFood: continue;
        }

        return foods;
    }
	
	
	/// <summary>
	/// A simple struct representing the position of the file pointer in csv "coordinates", the file open, and the
	/// progress of the parse (currentFood).
	/// </summary>
	private struct DatasetFileStatus
    {
        public DatasetFile file;
        public int currentChar;
        public int currentRow;
        public FoodData currentFood;
    }


    /// <summary>
    /// Processes the dataset into a list of FoodData objects.
    /// </summary>
    /// <param name="fileLength">Specify the length (in rows) of the 3 files (their length must be equal).</param>
    private void ProcessFiles(int fileLength = TOTAL_ROWS)
    {
        // Reset the dataset for subsequent processes
        m_dataset.Clear();

        DatasetFileStatus[] statuses = new DatasetFileStatus[3];

        // Initialise statuses
        for (int i = 0; i < 3; i++)
        {
            statuses[i] = new()
            {
                file = (DatasetFile)i,
                currentChar = 0,
                currentRow = 0
            };
        }

        // Process each row in the dataset in parallel across 3 dataset files.
        // The dataset files will share the same processed row index at all times.
        for (int i = 0; i < fileLength; i++)
        {
            // Process file row
            FoodData currentFood = new()
            {
                Nutrients = new float[Constraint.Count]
            };

            // Process the next row in the 3 dataset files, put them into one FoodData object.
            for (int j = 0; j < 3; j++)
            {
                statuses[j].currentFood = currentFood;
                ProcessFileRow(ref statuses[j]);
            }

            // Add processed row to the list (if it is a valid food)
            if (i >= FIRST_ROW)
            {
                // See if it has a custom setting, if so add its cost value
                foreach (var cfs in m_prefs.customFoodSettings)
                {
                    if (cfs.Key != currentFood.CompositeKey) continue;

                    // Add the custom settings cost value, and break
                    currentFood.Nutrients[(int)EConstraintType.Cost] = cfs.Cost;
                    break;
                }

                m_dataset.Add(currentFood);
            }
        }
    }


    /// <summary>
    /// Processes a dataset file until it reaches the end of the row.
    /// Allows all dataset files to be concurrently processed.
    /// </summary>
    /// <param name="status">Input info, which gets updated in the method.</param>
    private void ProcessFileRow(ref DatasetFileStatus status)
    {
        bool speechMarkOpened = false;
        int currentCol = 0;
        string currentColVal = "";
        int start = status.currentChar;
		
        for (int i = start; i < m_files[(int)status.file].Length; i++) // All files have equal length
        {
            char ch = m_files[(int)status.file][i];
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
                        currentColVal += ch;
                        break;
                    }

                    // Parses the word into a float, if possible.
                    // Default value - "" or "N" get a value of -1.
                    if (!float.TryParse(currentColVal, out float currentColValParsed))
                        currentColValParsed = -1;

                    // If the word is just a title, ignore it
                    if (status.currentRow >= FIRST_ROW)
                    {
                        // Trace values are given a value of 0
                        if (currentColVal == "Tr")
                            currentColValParsed = 0;

                        // Add the column value to the FoodData object (if currentCol corresponds to desired data)
                        HandleColumnLookup(status, currentCol, currentColVal, currentColValParsed);
                    }

                    // If the character was a newline, update status and return
                    if (ch == '\n')
                    {
                        status.currentChar = i + 1;
                        status.currentRow++;
                        return;
                    }
                    // Otherwise, just go to the next column
                    else if (ch == ',')
                    {
                        currentCol++;
                        currentColVal = "";
                    }
                    break;

                default: // Regular character, i.e. part of the next value
                    currentColVal += ch;
                    break;
            }
        }

        // End of file reached
        return;
    }


    /// <summary>
    /// Looks up which property the column `wordIndex` corresponds to, and if this is a string-based property,
	/// sets the corresponding property in the current food to the parsed string value. Otherwise, delegates to
	/// HandleNutrientLookup with the parsed float value.
    /// </summary>
    private void HandleColumnLookup(DatasetFileStatus status, int col, string colVal, float colValParsed)
    {
        switch (col)
        {
            case 0:
                status.currentFood.Code = colVal;
                return;
            case 1:
                status.currentFood.Name = colVal;
                return;
            case 2:
                status.currentFood.Description = colVal;
                return;
            case 3:
                status.currentFood.FoodGroup = colVal;
                return;
            case 5:
                status.currentFood.Reference = colVal;
                return;
        }

        HandleNutrientLookup(status, col, colValParsed);
    }


    /// <summary>
    /// Looks up which nutrient the column `wordIndex` corresponds to. The columns depend on the dataset file
    /// being explored currently. Sets the value of the nutrient property in the current food to the provided
	/// parsed float value.
    /// </summary>
    private void HandleNutrientLookup(DatasetFileStatus status, int col, float colValParsed)
    {
        EConstraintType[] columns;

        switch (status.file)
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

        if (col >= columns.Length)
        {
			// This column is empty, this can happen due to trailing commas ",,,"
			// in a csv file.
            return;
        }

		// Provided the column is a considered nutrient, update the corresponding
		// currentFood value to the parsed float value.
        if (columns[col] != (EConstraintType)(-1))
            status.currentFood.Nutrients[(int)columns[col]] = colValParsed;
    }
}