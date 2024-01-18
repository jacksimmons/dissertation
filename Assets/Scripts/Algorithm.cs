using System;
using System.Linq;
using System.Collections.Generic;
using Random = System.Random;
using UnityEngine;


public struct Food
{
    public string Name;
    public string Description;
    public string FoodGroup;
    public string Reference;

    public float Protein;
    public float Fat;
    public float Carbohydrates;
    public float Kcal;
}


public abstract class Algorithm : MonoBehaviour
{
    // A CSV file containing all proximate data. In the context of the McCance Widdowson dataset,
    // this is most major nutrients (Protein, Fat, Carbs, Sugar, Sat Fats, Energy, Water, etc...)
    [SerializeField]
    private TextAsset m_proximatesFile;

    protected static Random rng = new();
    protected List<Food> m_foods;


    private void Start()
    {
        m_foods = ReadFoods(m_proximatesFile.text);
    }


    /// <summary>
    /// This method converts a dataset file into a list of Food structs.
    /// </summary>
    /// <param name="csvText">The CSV data as a unformatted string.
    /// Each line must be separated by `\n` and values by `,`.</param>
    private List<Food> ReadFoods(string csvText)
    {
        string currentWord = "";
        void AddCharToWord(char c) { currentWord += c; }

        List<Food> foods = new List<Food>();
        int currentWordIndex = 0;
        int firstDataRowIndex = 3; // The first 3 rows are just titles, so skip them
        int currentRowIndex = 0;

        // A struct type so this can be edited and not affect foods in the list.
        Food currentFood = new();

        bool speechMarkOpened = false;

        // Each line is a food. Each field is a food property.
        // Iterate over every character in the CSV file.
        foreach (char ch in csvText)
        {
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
                        AddCharToWord(ch); break;
                    }

                    // If the word is just a title, ignore it
                    if (currentRowIndex < firstDataRowIndex)
                        goto reset_word;

                    // Missing values are given a value of -1
                    if (currentWord == "" || currentWord == "N")
                        currentWord = "-1";

                    // Trace values are given a value of 0
                    if (currentWord == "Tr")
                        currentWord = "0";

                    // For string fields, just assign the value.
                    // For float or int fields, we can safely parse the string, as
                    // the only string values these fields can have are checked
                    // in the containing if-clause.
                    switch (currentWordIndex)
                    {
                        case 1:
                            currentFood.Name = currentWord; break;
                        case 2:
                            currentFood.Description = currentWord; break;
                        case 3:
                            currentFood.FoodGroup = currentWord; break;
                        case 5:
                            currentFood.Reference = currentWord; break;
                        case 9:
                            currentFood.Protein = float.Parse(currentWord); break;
                        case 10:
                            currentFood.Fat = float.Parse(currentWord); break;
                        case 11:
                            currentFood.Carbohydrates = float.Parse(currentWord); break;
                        case 12:
                            currentFood.Kcal = float.Parse(currentWord); break;
                        case 16:
                            string sugars = currentWord; break;
                        case 27:
                            string saturates = currentWord; break;
                        case 45:
                            string trans = currentWord; break;
                    }

                    reset_word:
                    // Reset for the next word
                    currentWord = "";
                    currentWordIndex++;
                    break;

                case '\n': // New line
                    // If in speech marks, ignore new row
                    if (speechMarkOpened)
                    {
                        AddCharToWord(ch); break;
                    }

                    // If the finished row is just titles, ignore it
                    if (currentRowIndex < firstDataRowIndex)
                        goto next_food;

                    // Check the current food isn't missing essential data (due to N, Tr or "")
                    // This missing data is given the value -1, as seen in the delimiter ',' case.
                    if (currentFood.Protein < 0 || currentFood.Carbohydrates < 0 || currentFood.Fat < 0
                        || currentFood.Kcal < 0)
                    {
                        goto next_food;
                    }

                    // Check the current food doesn't conflict with the user's dietary needs.
                    if (!IsFoodGroupAllowed(currentFood.FoodGroup))
                    {
                        goto next_food;
                    }

                    // Food only gets added if it fails all the invalidity checks.
                    foods.Add(currentFood);

                    next_food:
                    currentWordIndex = 0;
                    currentRowIndex++;
                    currentFood = new();
                    break;

                default: // Regular character, i.e. part of the next value
                    AddCharToWord(ch); break;
            }
        }

        return foods;
    }


    /// <summary>
    /// A function to eliminate the vast majority of unacceptable foods by food group.
    /// May still leave some in, for example chicken soup may be under the soup group - WA[A,C,E]
    /// Will exclude all alcohol, as it is not nutritious.
    /// </summary>
    /// <param name="foodGroup">The food group code to check if allowed.</param>
    /// <returns>A boolean of whether the provided food is allowed by the user's diet.</returns>
    private bool IsFoodGroupAllowed(string foodGroup)
    {
        // Categories
        switch (foodGroup[0])
        {
            case 'M': // Meat
                if (!Preferences.Saved.eatsLandMeat)
                    return false;
                break;
            case 'J': // Fish
                if (!Preferences.Saved.eatsSeafood)
                    return false;
                break;
            case 'C': // Eggs
                if (!Preferences.Saved.eatsAnimalProduce)
                    return false;
                break;
            case 'B': // Milk
                if (!Preferences.Saved.eatsAnimalProduce || !Preferences.Saved.eatsLactose)
                    return false;
                break;
            case 'Q': // Alcohol - Excluded
                return false;
        }

        // Unique cases
        switch (foodGroup)
        {
            case "OB": // Animal fats
                if (!Preferences.Saved.eatsLandMeat)
                    return false;
                break;
        }

        return true;
    }


    public abstract void Run();
}