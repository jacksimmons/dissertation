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

        // Defaults for Men

        // Note: Converge constraints have a default minimum value of -1, for good reason.
        // The graph they represent is essentially two different exponential curves, one with limit at x = min and one at x = max.
        // Setting min = -1 means a value of 0 gives a non-infinite fitness, and this is common in the dataset.

        // Default weight: 3 (Detrimental)
        SetConstraint(ENutrient.Sugar, typeof(MinimiseConstraint), max: 30f, weight: 3);
        SetConstraint(ENutrient.SatFat, typeof(MinimiseConstraint), max: 30f, weight: 3);
        SetConstraint(ENutrient.TransFat, typeof(MinimiseConstraint), max: 5f, weight: 3);

        // Default weight: 2 (Essential to survival)
        SetConstraint(ENutrient.Kcal, typeof(ConvergeConstraint), max: 3500f, weight: 2, goal: 3000f);

        // Auto-generate default p/f/c properties based on the above constraint
        ConstraintData proteinData  = new();
        ConstraintData fatData      = new();
        ConstraintData carbsData    = new();

        CalorieMassConverter.CaloriesToMacros(constraints[(int)ENutrient.Kcal].Min,  ref proteinData.Min, ref fatData.Min, ref carbsData.Min);
        CalorieMassConverter.CaloriesToMacros(constraints[(int)ENutrient.Kcal].Max,  ref proteinData.Max, ref fatData.Max, ref carbsData.Max);
        CalorieMassConverter.CaloriesToMacros(constraints[(int)ENutrient.Kcal].Goal, ref proteinData.Goal, ref fatData.Goal, ref carbsData.Goal);

        SetConstraint(ENutrient.Protein,  typeof(ConvergeConstraint), max: proteinData.Max, weight: 2, goal: proteinData.Goal);
        SetConstraint(ENutrient.Fat,      typeof(ConvergeConstraint), max: fatData.Max,     weight: 2, goal: fatData.Goal);
        SetConstraint(ENutrient.Carbs,    typeof(ConvergeConstraint), max: carbsData.Max,   weight: 2, goal: carbsData.Goal);

        // Default weight: 1 (Beneficial)
        // For these, set the goal to be the recommended amount from the NHS.
        // The maximum is the highest "definitely safe" amount recommended by the NHS.
        SetConstraint(ENutrient.Calcium, typeof(ConvergeConstraint), max: 1500f, weight: 1, min: 0, goal: 700f);
        SetConstraint(ENutrient.Iron,    typeof(ConvergeConstraint), max: 17f,   weight: 1, min: 0, goal: 8.7f);
        SetConstraint(ENutrient.Iodine,  typeof(ConvergeConstraint), max: 500f,  weight: 1, min: 0, goal: 140f);

        SetConstraint(ENutrient.VitA,  typeof(ConvergeConstraint), max:1500f, weight: 1, min: 0, goal:700f);
        SetConstraint(ENutrient.VitB1, typeof(ConvergeConstraint), max: 100f, weight: 1, min: 0, goal: 1f);
        SetConstraint(ENutrient.VitB2, typeof(ConvergeConstraint), max: 40f,  weight: 1, min: 0, goal: 1.3f);
        SetConstraint(ENutrient.VitB3, typeof(ConvergeConstraint), max: 17f,  weight: 1, min: 0, goal: 16.5f);
        SetConstraint(ENutrient.VitB6, typeof(ConvergeConstraint), max: 10f,  weight: 1, min: 0, goal: 1.4f);
        SetConstraint(ENutrient.VitB9, typeof(ConvergeConstraint), max:1000f, weight: 1, min: 0, goal: 200f);
        SetConstraint(ENutrient.VitB12,typeof(ConvergeConstraint), max:2000f, weight: 1, min: 0, goal: 1.5f);
        SetConstraint(ENutrient.VitC,  typeof(ConvergeConstraint), max:1000f, weight: 1, min: 0, goal: 40f);
        SetConstraint(ENutrient.VitD,  typeof(ConvergeConstraint), max: 100f, weight: 1, min: 0, goal: 10f); // Not needed if sunny.
        SetConstraint(ENutrient.VitE,  typeof(ConvergeConstraint), max: 540f, weight: 1, min: 0, goal: 4f);
        SetConstraint(ENutrient.VitK1, typeof(ConvergeConstraint), max:1000f, weight: 1, min: 0, goal: 70f); // Goal: 1 microgram per kg bodyweight.

        pheroImportance = 1f;

        // Consistent results with this value
        pheroEvapRate = 0.1f;

        acoAlpha = 1f;
        acoBeta = 1f;
    }


    private void SetConstraint(ENutrient nut, Type constraintType, float max, float weight, float min = 0, float goal = 0)
    {
        constraints[(int)nut] = new() { Min = min, Max = max, Goal = goal, Type = constraintType.FullName!, Weight = weight };
        Constraint.Build(constraints[(int)nut]); // May throw an error, useful to check if all params are valid
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
        for (int i = 0; i < constraints.Length; i++)
        {
            ConstraintData constraint = constraints[i];
            str += $"Nutrient: {Nutrient.Values[i], 10} Min: {constraint.Min, 8:F3} Max: {constraint.Max, 8:F3} Goal: {constraint.Goal, 8:F3} Type: {constraint.Type, 20}\n";
        }

        str += "\n";
        return str;
    }
}