using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


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
    // Property-like static variables for Nutrient enum length, and Nutrient enum values.
    public static int Count = Enum.GetValues(typeof(Nutrient)).Length;
    public static Nutrient[] Values = (Nutrient[])Enum.GetValues(typeof(Nutrient));


    public static string GetUnit(Nutrient nutrient)
    {
        return nutrient switch
        {
            Nutrient.Protein or Nutrient.Fat or Nutrient.Carbs or Nutrient.Sugar or Nutrient.SatFat or Nutrient.TransFat => "g",
            Nutrient.Calcium or Nutrient.Iron => "mg",
            Nutrient.Iodine => "µg",
            Nutrient.Kcal => "kcal",
            _ => throw new ArgumentOutOfRangeException(nameof(nutrient)),
        };
    }
}