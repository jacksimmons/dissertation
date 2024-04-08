using System;
using System.Collections.ObjectModel;


public interface IFood
{
    public string CompositeKey { get; }
}


/// <summary>
/// All of the data associated with a food in the dataset.
/// All nutrients are measured for a 100g portion.
/// </summary>
public class FoodData : IFood
{
    public string Name;
    public string Code;
    public string FoodGroup;
    public string Description;
    public string Reference;
    public float[] Nutrients;

    // A unique key for each Food type, which can be used to translate between Food and FoodData.
    public string CompositeKey
    {
        get { return Name + Code + FoodGroup; }
    }


    public FoodData()
    {
        Nutrients = new float[Constraint.Count];
    }


    public FoodData MergeWith(FoodData other)
    {
        if (EquivalentType(other))
        {
            for (int i = 0; i < Constraint.Count; i++)
            {
                // If not initialised in this nutrients, take it from the other nutrients.
                if (MathTools.Approx(Nutrients[i], 0)) Nutrients[i] = other.Nutrients[i];
            }
            return this;
        }

        throw new InvalidOperationException($"Could not merge {CompositeKey} and {other.CompositeKey} - they don't represent the same food type.");
    }


    public bool EquivalentType(FoodData other)
    {
        return CompositeKey == other.CompositeKey;
    }
}


/// <summary>
/// A data structure which represents the properties of a 100g portion of
/// a specific food from the dataset.
/// </summary>
public class Food : IFood, IVerbose
{
    public const int MASS = 100;

    public string Name { get; }
    public string Code { get; }
    public string FoodGroup { get; }

    // A unique key for each Food type, which can be used to translate between Food and FoodData.
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