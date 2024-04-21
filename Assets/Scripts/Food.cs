// Commented 20/4
using System;
using System.Collections.ObjectModel;


/// <summary>
/// An interface for Food Type related classes.
/// </summary>
public interface IFood
{
    public string CompositeKey { get; }
}


/// <summary>
/// All of the data associated with a food in the dataset.
/// All nutrients are measured for a 100g portion.
/// </summary>
public sealed class FoodData : IFood
{
    public string Name;
    public string Code;
    public string FoodGroup;
    public string Description;
    public string Reference;
    public float[] Nutrients; // Amount of each nutrient, in its native unit. See Constraint for units.

    // A unique key for each Food type, which can be used to translate between Food and FoodData.
    public string CompositeKey
    {
        get { return Name + Code + FoodGroup; }
    }


    public FoodData()
    {
        Nutrients = new float[Constraint.Count];
    }


    /// <summary>
    /// Combines this FoodData with another FoodData. If this FoodData has an empty value, and the other doesn't,
    /// the output FoodData will inherit the value from the other.
    /// </summary>
    /// <param name="other">The FoodData to merge with.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public FoodData MergeWith(FoodData other)
    {
        if (EquivalentType(other))
        {
            for (int i = 0; i < Constraint.Count; i++)
            {
                // If not initialised in this array, take it from the other array.
                if (MathTools.Approx(Nutrients[i], 0)) Nutrients[i] = other.Nutrients[i];
            }
            return this;
        }

        throw new InvalidOperationException($"Could not merge {CompositeKey} and {other.CompositeKey} - they don't represent the same food type.");
    }


    /// <summary>
    /// Returns whether or not the provided FoodData object represents the same food type as this.
    /// </summary>
    /// <param name="other">The FoodData object to check.</param>
    /// <returns>`true` if they are the same type, `false` otherwise.</returns>
    public bool EquivalentType(FoodData other)
    {
        return CompositeKey == other.CompositeKey;
    }
}


/// <summary>
/// A data structure which represents the properties of a 100g portion of
/// a specific food from the dataset.
/// </summary>
public sealed class Food : IFood, IVerbose
{
    /// <summary>
    /// The mass of food that gave the corresponding nutrient amounts (const because all foods
    /// have the same value).
    /// </summary>
    public const int MASS = 100;

    public string Name { get; }
    public string Code { get; }
    public string FoodGroup { get; }

    /// <summary>
    /// A unique key for each Food type, which can be used to translate between Food and FoodData.
    /// </summary>
    public string CompositeKey
    {
        get { return Name + Code + FoodGroup; }
    }

    public ReadOnlyCollection<float> Nutrients { get; }


    public Food(FoodData data)
    {
        Name = data.Name;
        Code = data.Code;
        FoodGroup = data.FoodGroup;
        Nutrients = new(data.Nutrients);
    }


    /// <summary>
    /// Checks whether this food represents the same food type as the other.
    /// Also checks that all the nutrients match, as a precaution.
    /// </summary>
    /// <param name="other">The other food to check.</param>
    /// <returns>`true` if so, `false` otherwise.</returns>
    public bool IsEqualTo(Food other)
    {
        // Check all simple attribs are the same
        bool attribs = CompositeKey == other.CompositeKey;

        if (!attribs) return false;

        // Check all nutrients are the same
        for (int i = 0; i < Constraint.Count; i++)
        {
            if (Nutrients[i] != other.Nutrients[i])
                return false;
        }
        return true;
    }


    public string Verbose()
    {
        // Use the Portion's verbose code with the default mass.
        return new Portion(this, MASS).Verbose();
    }
}