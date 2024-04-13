/// <summary>
/// A data structure with a type of food (100g), multiplied by a quantity.
/// </summary>
public struct Portion : IVerbose
{
    public readonly Food FoodType { get; }

    public int Mass { get; set; }
    private readonly float Multiplier => (float)Mass / Food.MASS;


    public Portion(Food food)
    {
        FoodType = food;
        Mass = Food.MASS;
    }


    public Portion(Food food, int mass)
    {
        FoodType = food;
        Mass = mass;
    }


    public float GetConstraintAmount(EConstraintType nutrient)
    {
        return FoodType.Nutrients[(int)nutrient] * Multiplier;
    }


    public bool IsEqualTo(Portion other)
    {
        return FoodType.IsEqualTo(other.FoodType) && Multiplier == other.Multiplier;
    }


    public string Verbose()
    {
        string str = $"Name: {FoodType.Name}\nNutrients:";

        for (int i = 0; i < Constraint.Count; i++)
        {
            str += $"\n{Constraint.Values[i]}: {FoodType.Nutrients[i] * Multiplier}{Constraint.GetUnit(Constraint.Values[i])}";
        }

        str += $"\nMass: {Mass}g";

        return str;
    }
}
