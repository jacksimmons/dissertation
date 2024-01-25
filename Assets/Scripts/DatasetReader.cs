using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class DatasetReader
{
    /// <summary>
    /// This method converts a dataset file into a list of Food structs.
    /// </summary>
    /// <param name="csvText">The CSV data as a unformatted string.
    /// Each line must be separated by `\n` and values by `,`.</param>
    public List<Food> ReadFoods(string csvText, Preferences prefs)
    {
        List<Food> foods = new();

        // Store food's properties before construction - it is a readonly class
        string currentFoodName;
        string currentFoodDesc;
        string currentFoodGroup;
        string currentFoodRef;
        Dictionary<Proximate, float> currentFoodProximates;
        ResetCurrentFood();

        void ResetCurrentFood()
        {
            currentFoodName = "";
            currentFoodDesc = "";
            currentFoodGroup = "";
            currentFoodRef = "";

            // Change the reference, so that existing dictionaries passed
            // into Foods can still exist.
            currentFoodProximates = new();

            foreach (Proximate p in Enum.GetValues(typeof(Proximate)))
            {
                currentFoodProximates[p] = -1;
            }
        }

        string currentWord = "";
        void AddCharToWord(char c) { currentWord += c; }

        int currentWordIndex = 0;
        int firstDataRowIndex = 3; // The first 3 rows are just titles, so skip them
        int currentRowIndex = 0;
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

                    // Parses the word into a float, if possible.
                    // Default value - "" or "N" get a value of -1.
                    if (!float.TryParse(currentWord, out float floatVal))
                        floatVal = -1;

                    // If the word is just a title, ignore it
                    if (currentRowIndex < firstDataRowIndex)
                        goto reset_word;

                    // Trace values are given a value of 0
                    if (currentWord == "Tr")
                        floatVal = 0;

                    // For string fields, just assign the value.
                    // For float or int fields, we can safely parse the string, as
                    // the only string values these fields can have are checked
                    // in the containing if-clause.
                    switch (currentWordIndex)
                    {
                        case 1:
                            currentFoodName = currentWord; break;
                        case 2:
                            currentFoodDesc = currentWord; break;
                        case 3:
                            currentFoodGroup = currentWord; break;
                        case 5:
                            currentFoodRef = currentWord; break;
                        case 9:
                            currentFoodProximates[Proximate.Protein] = floatVal; break;
                        case 10:
                            currentFoodProximates[Proximate.Fat] = floatVal; break;
                        case 11:
                            currentFoodProximates[Proximate.Carbs] = floatVal; break;
                        case 12:
                            currentFoodProximates[Proximate.Kcal] = floatVal; break;
                        case 16:
                            currentFoodProximates[Proximate.Sugar] = floatVal; break;
                        case 27:
                            currentFoodProximates[Proximate.SatFat] = floatVal; break;
                        case 45:
                            currentFoodProximates[Proximate.TransFat] = floatVal; break;
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
                    foreach (Proximate p in Enum.GetValues(typeof(Proximate)))
                    {
                        if (currentFoodProximates[p] < 0)
                            goto next_food;
                    }

                    // Check the current food doesn't conflict with the user's dietary needs.
                    if (!prefs.IsFoodGroupAllowed(currentFoodGroup))
                    {
                        goto next_food;
                    }

                    // Food only gets added if it fails all the invalidity checks.
                    foods.Add(new(currentFoodName, currentFoodDesc, currentFoodGroup, currentFoodRef, currentFoodProximates));

                next_food:
                    currentWordIndex = 0;
                    currentRowIndex++;
                    ResetCurrentFood();
                    break;

                default: // Regular character, i.e. part of the next value
                    AddCharToWord(ch); break;
            }
        }

        return foods;
    }
}