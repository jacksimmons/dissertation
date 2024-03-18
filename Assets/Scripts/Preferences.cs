using System;
using System.Collections.Generic;
using System.Reflection;


public enum EWeightGoal
{
    MaintainWeight,
    LoseWeight,
    GainWeight
}


public enum EAssignedSex
{
    Male,
    Female
}


public interface ICached
{
    /// <summary>
    /// Saves the instance as the .Saved static member variable for its class.
    /// This occurs during saving and loading to files.
    /// </summary>
    public void Cache();
}


/// <summary>
/// A class which saves the user's preferences when serialized into a file.
/// </summary>
[Serializable]
public class Preferences : ICached
{
    private static Preferences m_instance;
    public static Preferences Instance
    {
        get
        {
            if (m_instance != null) return m_instance;

#if UNITY_64
            // This will automatically Cache() the preferences, so no need to update m_instance.
            return Saving.LoadFromFile<Preferences>("Preferences.json");
#else
            throw new NullReferenceException("Preferences was not defined.");
#endif
        }

#if !UNITY_64
        set
        {
            m_instance = value;
        }
#endif
    }
    public void Cache() { m_instance = this; }


    // PREFS MENU------------------


    public static readonly string[] ALG_TYPES =
    {
        typeof(AlgSFGA).FullName!,
        typeof(AlgACO).FullName!,
    };


    public static readonly string[] CONSTRAINT_TYPES =
    {
        typeof(HardConstraint).FullName!,
        typeof(MinimiseConstraint).FullName!,
        typeof(ConvergeConstraint).FullName!,
    };


    //
    // Food Groups
    //
    public bool eatsLandMeat = true; // Carnivore, Lactose-Intolerant
    public bool eatsSeafood = true; // Carnivore, Pescatarian, LI
    public bool eatsAnimalProduce = true; // Vegetarian, LI
    public bool eatsLactose = true; // Vegetarian, i.e. no Milk


    //
    // Constraints
    //
    public ConstraintData[] constraints;


    // ALG SETUP MENU--------------


    //
    // Algorithm settings
    //
    public int populationSize = 10;
    public int numStartingPortionsPerDay = 1;
    public int minPortionMass = 1;
    public int maxPortionMass = 500;
    public bool addFitnessForMass = true;
    public string algorithmType = typeof(AlgSFGA).FullName!;
    public bool elitist = true;

    //
    // ACO-specific settings
    //
    public float pheroImportance;
    public float pheroEvapRate;
    // Probability calculation variables
    public float acoAlpha;      // Pheromone exponent
    public float acoBeta;       // Desirability exponent

    //
    // Experiment settings
    //
    // Max fitness value plotted on the output graph.
    public const int MAX_FITNESS = 50_000;
    // Any fitness below this will be assigned maximum selection pressure.
    public const int CRITICAL_FITNESS = 1_000;


    // By default, the user's settings should permit every food type - this
    // best fits the average person.
    public Preferences()
    {
        constraints = new ConstraintData[Nutrient.Count];

        constraints[(int)ENutrient.Kcal] = new() { Min = 0f, Max = 3500f, Goal = 2700f, Type = "ConvergeConstraint" };
        constraints[(int)ENutrient.Protein] = new() { Min = 0f, Max = 200f, Goal = 150f, Type = "ConvergeConstraint" };
        constraints[(int)ENutrient.Fat] = new() { Max = 150f, Goal = 100f, Type = "MinimiseConstraint" };
        constraints[(int)ENutrient.Carbs] = new() { Max = 375f, Goal = 300f, Type = "MinimiseConstraint" };

        constraints[(int)ENutrient.Sugar] = new() { Max = 30f, Type = "MinimiseConstraint" };
        constraints[(int)ENutrient.SatFat] = new() { Max = 30f, Type = "MinimiseConstraint" };
        constraints[(int)ENutrient.TransFat] = new() { Max = 5f, Type = "MinimiseConstraint" };

        constraints[(int)ENutrient.Calcium] = new() { Max = 700f, Type = "HardConstraint" };
        constraints[(int)ENutrient.Iodine] = new() { Max = 140f, Type = "HardConstraint" };
        constraints[(int)ENutrient.Iron] = new() { Max = 8.7f, Type = "HardConstraint" };

        pheroImportance = 1f;

        // Consistent results with this value
        pheroEvapRate = 0.1f;

        acoAlpha = 1f;
        acoBeta = 1f;
    }


    public void MakeVegan()
    {
        eatsLandMeat = false;
        eatsSeafood = false;
        eatsAnimalProduce = false;
    }


    public void MakeVegetarian()
    {
        eatsLandMeat = false;
        eatsSeafood = false;
    }


    public void MakePescatarian()
    {
        eatsLandMeat = false;
    }


    /// <summary>
    /// A function to eliminate the vast majority of unacceptable foods by food group.
    /// May still leave some in, for example chicken soup may be under the soup group - WA[A,C,E]
    /// Will exclude all alcohol, as it is not nutritious.
    /// </summary>
    /// <param name="foodGroup">The food group code to check if allowed.</param>
    /// <returns>A boolean of whether the provided food is allowed by the user's diet.</returns>
    public bool IsFoodGroupAllowed(Food food)
    {
        string foodGroup = food.FoodGroup;

        // In case of no food group, say it is not allowed.
        // Safest approach - removes all foods without a proper food group label.
        if (foodGroup.Length == 0) return false;

        // Categories
        switch (foodGroup[0])
        {
            case 'M': // Meat
                if (!eatsLandMeat)
                    return false;
                break;
            case 'J': // Fish
                if (!eatsSeafood)
                    return false;
                break;
            case 'C': // Eggs
                if (!eatsAnimalProduce)
                    return false;
                break;
            case 'B': // Milk
                if (!eatsAnimalProduce || !eatsLactose)
                    return false;
                break;
            case 'Q': // Alcohol - Excluded
                return false;
        }


        // Unique groups
        switch (foodGroup)
        {
            case "OB": // Animal fats
                if (!eatsLandMeat)
                    return false;
                break;
        }


        // Unique keywords to catch hybrid items (e.g. Tuna sandwich)
        string name = food.Name.ToLower();

        if (name.Contains("salmon") && !eatsSeafood) return false;
        if (name.Contains("cod") && !eatsSeafood) return false;
        if (name.Contains("tuna") && !eatsSeafood) return false;

        if (name.Contains("gelatine") && !eatsLandMeat) return false;
        if (name.Contains("beef") && !eatsLandMeat) return false;
        if (name.Contains("pork") && !eatsLandMeat) return false;

        return true;
    }


    public string Verbose()
    {
        string str = "";

        FieldInfo[] props = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
        str += "Algorithm Fields:\n";
        foreach (var prop in props)
        {
            str += $"{prop.Name}: {prop.GetValue(this)}\n";
        }

        str += "\nAlgorithm Constraints:\n";
        foreach (ConstraintData constraint in constraints)
        {
            str += $"Min:{constraint.Min} Max:{constraint.Max} Goal:{constraint.Goal} Type:{constraint.Type}\n";
        }

        str += "\n";
        return str;
    }
}