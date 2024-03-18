using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


/// <summary>
/// These enums are based on the naming convention of the dataset I am using.
/// Nutrient = Macronutrients; Protein Fat Carbohydrates Energy Sugar SatFat TransFat etc.
/// Inorganics = Elements; Sodium Potassium Calcium Magnesium etc.
/// Vitamins = Vitamin A, B, etc.
/// </summary>
public enum ENutrient
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


public static class Nutrient
{
    // Property-like static variables for ENutrient enum length, and ENutrient enum values.
    public static int Count = Enum.GetValues(typeof(ENutrient)).Length;
    public static ENutrient[] Values = (ENutrient[])Enum.GetValues(typeof(ENutrient));


    public static string GetUnit(ENutrient nutrient)
    {
        return nutrient switch
        {
            ENutrient.Protein or ENutrient.Fat or ENutrient.Carbs or ENutrient.Sugar or ENutrient.SatFat or ENutrient.TransFat => "g",
            ENutrient.Calcium or ENutrient.Iron => "mg",
            ENutrient.Iodine => "µg",
            ENutrient.Kcal => "kcal",
            _ => throw new ArgumentOutOfRangeException(nameof(nutrient)),
        };
    }
}