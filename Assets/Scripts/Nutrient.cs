using System;


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
    Iodine,
    Iron,

    // Vitamins
    VitA,
    VitB1,
    VitB2,
    VitB3,
    VitB6,
    VitB9,
    VitB12,
    VitC,
    VitD,
    VitE,
    VitK1
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

            ENutrient.Calcium or ENutrient.Iron or
            ENutrient.VitE or ENutrient.VitB1 or ENutrient.VitB2 or ENutrient.VitB3 or ENutrient.VitB6 or ENutrient.VitC => "mg",

            ENutrient.Iodine or
            ENutrient.VitA or ENutrient.VitD or ENutrient.VitK1 or ENutrient.VitB12 or ENutrient.VitB9 => "µg",

            ENutrient.Kcal => "kcal",
            _ => throw new ArgumentOutOfRangeException(nameof(nutrient)),
        };
    }
}


///// <summary>
///// A quantity struct representing a float amount and a unit.
///// </summary>
//public struct Quantity : IComparable<Quantity>
//{
//    public float Amount { get; }
//    public string Unit { get; set; }


//    public Quantity(float amount, string unit)
//    {
//        Amount = amount;
//        Unit = unit;
//    }


//    /// <summary>
//    /// Converts the Mass to a readable, normalised integer.
//    /// Cascades through the units Unit -> Milliunits -> Microunits
//    /// until it finds one which is non-zero, then uses the corresponding
//    /// suffix (Unit, mUnit, µUnit).
//    /// 
//    /// If the whole value is 0, returns 0µUnit.
//    /// </summary>
//    /// <returns></returns>
//    public override string ToString()
//    {
//        return Amount + Unit;
//    }


//    public int CompareTo(Quantity other)
//    {
//        if (Unit != other.Unit)
//        {
//            Logger.Error($"{Unit} and {other.Unit} cannot be compared!");
//        }

//        return Amount.CompareTo(other.Amount);
//    }
//}