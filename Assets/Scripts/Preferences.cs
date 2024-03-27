using System;
using System.Collections.Generic;
using System.Reflection;


public enum EFitnessFunc
{
    Exponential,
    ManhattanDist,
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
public class Preferences : ICached, IVerbose
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
        typeof(AlgACODynamic).FullName!,
    };


    public static readonly string[] CONSTRAINT_TYPES =
    {
        typeof(HardConstraint).FullName!,
        typeof(MinimiseConstraint).FullName!,
        typeof(ConvergeConstraint).FullName!,
        typeof(NullConstraint).FullName!,
    };


    //
    // Food Groups
    //
    public bool eatsLandMeat = true; // Carnivore, Lactose-Intolerant
    public bool eatsSeafood = true; // Carnivore, Pescatarian, LI
    public bool eatsAnimalProduce = true; // Vegetarian, LI
    public bool eatsLactose = true; // Vegetarian, i.e. no Milk
    public List<string> bannedFoodKeys = new(); // The composite keys of banned foods.


    //
    // User Info
    //
    public bool isMale = true;
    public bool isPregnant = false;
    public bool needsVitD = true;
    public int ageYears = 20;
    public float weightKg = 70;
    public float heightCm = 160;


    //
    // Constraints
    //
    public ConstraintData[] constraints = new ConstraintData[Nutrient.Count];
    public bool[] acceptMissingNutrientValue = new bool[Nutrient.Count];


    // ALG SETUP MENU--------------


    //
    // Algorithm settings
    //
    public int populationSize = 10;
    public int numStartingPortionsPerDay = 1;
    public int minPortionMass = 1;
    public int maxPortionMass = 500;
    public bool addFitnessForMass = true;
    public string algorithmType = ALG_TYPES[0];
    public bool elitist = false;
    public EFitnessFunc fitnessFunc = EFitnessFunc.ManhattanDist; // Checked


    //
    // GA-specific settings
    //
    public int mutationMassChangeMin = 1;
    public int mutationMassChangeMax = 10;
    // Chance for any portion to be mutated in an algorithm pass.
    // Is divided by the number of portions in the calculation.
    public float chanceToMutatePortion = 1f;
    // Impacts determinism. Low value => High determinism, High value => Random chaos
    // 0 or 1 -> No convergence
    public float chanceToAddOrRemovePortion = 0.1f;
    // Controls proportion of selected parents in each generation (> 0). Drastically slows, and decreases optimality of, the
    // program as this approaches 1.
    public float proportionParents = 0.5f;
    // Must be in the range [1, 2]
    public float selectionPressure = 1.5f;
    // crossoverPoints-point crossover. Set this to 2 for 2-point crossover, etc.
    public int crossoverPoints = 1;


    //
    // ACO-specific settings
    //
    // Number of iterations before replacing the worst food.
    // High value => Deeper search (more iterations for evaluation)
    // Low value => Wider search (more foods)
    public int colonyPortions = 10;
    public int colonyStagnationIters = 1000;

    public float pheroImportance = 1f;
    public float pheroEvapRate = 0.1f;
    // Probability calculation variables
    public float acoAlpha = 1f;      // Pheromone exponent
    public float acoBeta = 1f;       // Desirability exponent
    public float defaultPheromone = 1f;

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
        CalculateDefaultConstraints();
    }


    public void CalculateDefaultConstraints()
    {
        // Default weight: 3
        SetConstraint(ENutrient.Sugar, typeof(MinimiseConstraint), max: 30f, weight: 3);
        SetConstraint(ENutrient.SatFat, typeof(MinimiseConstraint), max: isMale ? 30f : 20f, weight: 3);
        SetConstraint(ENutrient.TransFat, typeof(MinimiseConstraint), max: 5f, weight: 3);

        // Default weight: 2
        SetConstraint(ENutrient.Kcal, typeof(ConvergeConstraint), max: isMale ? 3000 : 2500, weight: 2, goal: isMale ? 2500 : 2000);

        // Auto-generate default p/f/c properties based on the above constraint
        ConstraintData proteinData = new();
        ConstraintData fatData = new();
        ConstraintData carbsData = new();

        MathTools.CaloriesToMacros(constraints[(int)ENutrient.Kcal].Min, ref proteinData.Min, ref fatData.Min, ref carbsData.Min);
        MathTools.CaloriesToMacros(constraints[(int)ENutrient.Kcal].Max, ref proteinData.Max, ref fatData.Max, ref carbsData.Max);
        MathTools.CaloriesToMacros(constraints[(int)ENutrient.Kcal].Goal, ref proteinData.Goal, ref fatData.Goal, ref carbsData.Goal);

        SetConstraint(ENutrient.Protein, typeof(ConvergeConstraint), max: proteinData.Max, weight: 2, goal: proteinData.Goal);
        SetConstraint(ENutrient.Fat, typeof(ConvergeConstraint), max: fatData.Max, weight: 2, goal: fatData.Goal);
        SetConstraint(ENutrient.Carbs, typeof(ConvergeConstraint), max: carbsData.Max, weight: 2, goal: carbsData.Goal);

        // Default weight: 1 (Beneficial)
        // For these, set the goal to be the recommended amount from the NHS.
        // The maximum is the highest "definitely safe" amount recommended by the NHS.
        SetConstraint(ENutrient.Calcium, typeof(ConvergeConstraint), max: 1500f, weight: 1, min: 0, goal: 700f);

        bool moreIron = !isMale && (19 <= ageYears && ageYears <= 49);
        SetConstraint(ENutrient.Iodine, typeof(ConvergeConstraint), max: 500f, weight: 1, min: 0, goal: 140f);
        SetConstraint(ENutrient.Iron, typeof(ConvergeConstraint), max: 17f, weight: 1, min: 0, goal: moreIron ? 14.8f : 8.7f);

        SetConstraint(ENutrient.VitA, typeof(ConvergeConstraint), max: 1500, weight: 1, min: 0, goal: isMale ? 700 : 600);
        SetConstraint(ENutrient.VitB1, typeof(ConvergeConstraint), max: 100, weight: 1, min: 0, goal: isMale ? 1 : 0.8f);
        SetConstraint(ENutrient.VitB2, typeof(ConvergeConstraint), max: 40f, weight: 1, min: 0, goal: isMale ? 1.3f : 1.1f);
        SetConstraint(ENutrient.VitB3, typeof(ConvergeConstraint), max: 17f, weight: 1, min: 0, goal: isMale ? 16.5f: 13.2f);
        SetConstraint(ENutrient.VitB6, typeof(ConvergeConstraint), max: 10f, weight: 1, min: 0, goal: isMale ? 1.4f : 1.2f);
        SetConstraint(ENutrient.VitB9, typeof(ConvergeConstraint), max: 1000f, weight: 1, min: 0, goal: isPregnant ? 400f : 200f);
        SetConstraint(ENutrient.VitB12, typeof(ConvergeConstraint), max: 2000f, weight: 1, min: 0, goal: 1.5f);
        SetConstraint(ENutrient.VitC, typeof(ConvergeConstraint), max: 1000f, weight: 1, min: 0, goal: 40f);
        SetConstraint(ENutrient.VitD, typeof(ConvergeConstraint), max: 100f, weight: 1, min: 0, goal: needsVitD ? 10 : 0); // Not needed if sunny.
        SetConstraint(ENutrient.VitE, typeof(ConvergeConstraint), max: 540f, weight: 1, min: 0, goal: isMale ? 4 : 3);
        SetConstraint(ENutrient.VitK1, typeof(ConvergeConstraint), max: 1000f, weight: 1, min: 0, goal: weightKg); // Goal: 1 microgram per kg bodyweight.
    }


    private void SetConstraint(ENutrient nut, Type constraintType, float max, float weight, float min = 0, float goal = 0)
    {
        constraints[(int)nut] = new() { Min = min, Max = max, Goal = goal, Type = constraintType.FullName!, Weight = weight };
        Constraint.Build(constraints[(int)nut]); // ! Check if all params are valid. If not, throws an error.
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
    /// A function to eliminate the vast majority of unacceptable foods by food group, and/or name.
    /// May still leave some in, for example chicken soup may be under the soup group - WA[A,C,E]
    /// Will exclude all alcohol, as it is not nutritious.
    /// </summary>
    /// <param name="foodGroup">The food group to check, to see if allowed.</param>
    /// <param name="name">The name to check.</param>
    /// <returns>A boolean of whether the provided food is allowed by the user's diet.</returns>
    public bool IsFoodAllowed(string foodGroup, string name)
    {
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

        string lowerName = name.ToLower();

        // Unique keywords to catch hybrid items (e.g. Tuna sandwich)
        if (lowerName.Contains("salmon") && !eatsSeafood) return false;
        if (lowerName.Contains("cod") && !eatsSeafood) return false;
        if (lowerName.Contains("tuna") && !eatsSeafood) return false;

        if (lowerName.Contains("gelatine") && !eatsLandMeat) return false;
        if (lowerName.Contains("beef") && !eatsLandMeat) return false;
        if (lowerName.Contains("pork") && !eatsLandMeat) return false;

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
            str += $"Nutrient: {Nutrient.Values[i],10}" + constraint.Verbose();
        }

        str += "\n";
        return str;
    }
}