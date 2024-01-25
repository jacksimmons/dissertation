/// <summary>
/// These enums are based on the naming convention of the dataset I am using.
/// Proximates = Macronutrients; Protein Fat Carbohydrates Energy Sugar SatFat TransFat etc.
/// Inorganics = Elements; Sodium Potassium Calcium Magnesium etc.
/// Vitamins = Vitamin A, B, etc.
/// </summary>

public enum Proximate
{
    Protein,
    Fat,
    Carbs,
    Kcal,
    Sugar,
    SatFat,
    TransFat
}


public enum Inorganic
{
    Calcium,
    Iodine,
    Iron
}


public enum Vitamin
{
    A,
    B1,
    B2,
    B3,
    B6,
    B7,
    B9,
    B12,
    C,
    D,
    E,
    K1
}