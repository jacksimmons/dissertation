// Commented 20/4
/// <summary>
/// A data structure with a type of food (100g), multiplied by a quantity.
/// </summary>
public struct Portion : IVerbose
{
    /// <summary>
    /// The type of food this portion represents.
    /// </summary>
    public Food FoodType { get; }

    /// <summary>
    /// The custom mass this portion has.
    /// </summary>
    public int Mass { get; set; }

    /// <summary>
    /// Shorthand for finding the multiplier from default Food mass to custom Portion mass.
    /// </summary>
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


    /// <summary>
    /// Get the amount of the provided constraint type that this portion contains, with its
    /// native units. (see Constraint for units of each constraint type).
    /// </summary>
    /// <param name="nutrient">The constraint type.</param>
    /// <returns>The value (amount) of this constraint type that this portion has.</returns>
    public readonly float GetConstraintAmount(EConstraintType nutrient)
    {
        return FoodType.Nutrients[(int)nutrient] * Multiplier;
    }


    /// <summary>
    /// Checks if two portions are equal; i.e. do they represent the same food type and have
    /// identical mass?
    /// </summary>
    /// <param name="other">The Portion to compare to.</param>
    /// <returns>`true` if equal, `false` otherwise.</returns>
    public readonly bool IsEqualTo(Portion other)
    {
        return FoodType.IsEqualTo(other.FoodType) && Multiplier == other.Multiplier;
    }


    public readonly string Verbose()
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
