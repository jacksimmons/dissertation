
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.VisualScripting;

/// <summary>
/// These enums are based on the naming convention of the dataset I am using.
/// Nutrients = Macronutrients; Protein Fat Carbohydrates Energy Sugar SatFat TransFat etc.
/// Inorganics = Elements; Sodium Potassium Calcium Magnesium etc.
/// Vitamins = Vitamin A, B, etc.
/// </summary>
public enum Nutrient
{
    // Proximates
    Protein,
    Fat,
    Carbs,
    Kcal,
    Sugar,
    SatFat,
    TransFat,

    // Inorganics
    Calcium,
    Iron,
    Iodine,

    // Vitamins
    //A,
    //B1,
    //B2,
    //B3,
    //B6,
    //B7,
    //B9,
    //B12,
    //C,
    //D,
    //E,
    //K1
}


public static class Nutrients
{
    public static int Count = Enum.GetValues(typeof(Nutrient)).Length - 1;
    public static Nutrient[] Values = (Nutrient[])Enum.GetValues(typeof(Nutrient));


    public static string GetUnit(Nutrient nutrient)
    {
        switch (nutrient)
        {
            case Nutrient.Protein:
            case Nutrient.Fat:
            case Nutrient.Carbs:
            case Nutrient.Sugar:
            case Nutrient.SatFat:
            case Nutrient.TransFat:
                return "g";
            case Nutrient.Calcium:
            case Nutrient.Iron:
                return "mg";
            case Nutrient.Iodine:
                return "µg";
            case Nutrient.Kcal:
                return "kcal";
            default:
                throw new ArgumentOutOfRangeException(nameof(nutrient));
        }
    }
}