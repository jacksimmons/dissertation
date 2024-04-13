using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


public enum EFitnessApproach
{
    SummedFitness,
    ParetoDominance
}


public enum ESelectionMethod
{
    Tournament,
    Rank
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

            // This will automatically Cache() the preferences, so no need to update m_instance.
            return Saving.LoadPreferences();
        }
    }
    public void Cache() { m_instance = this; }


    [Serializable]
    public class CustomFoodSettings
    {
        public string Key;
        public float Cost;
        public bool Banned;
    }

    // PREFS MENU------------------


    public static readonly string[] ALG_TYPES =
    {
        typeof(AlgGA).FullName!,
        typeof(AlgACO).FullName!,
        typeof(AlgPSO).FullName!,
    };


    public static readonly string[] CONSTRAINT_TYPES =
    {
        typeof(HardConstraint).FullName!,
        typeof(MinimiseConstraint).FullName!,
        typeof(ConvergeConstraint).FullName!,
        typeof(NullConstraint).FullName!,
    };


    //
    // Calorie Goals
    //
    public float dailyExerciseKcal;
    public bool gainElseLoseWeight;
    public float dailyLoseOrGainKcal;


    //
    // Food Groups
    //
    
    public bool eatsLandMeat; // Carnivore, Lactose-Intolerant
    public bool eatsSeafood; // Carnivore, Pescatarian, LI
    public bool eatsAnimalProduce; // Vegetarian, LI
    public bool eatsLactose; // Vegetarian, i.e. no Milk


    //
    // User Info
    //

    public bool maleElseFemale;
    public bool isPregnant;
    public bool needsVitD;
    public int ageYears;
    public float weightKg;
    public float heightCm;


    //
    // Constraints
    //
    
    public ConstraintData[] constraints = new ConstraintData[Constraint.Count];
    public bool[] acceptMissingNutrientValue = new bool[Constraint.Count];
    public List<CustomFoodSettings> customFoodSettings = new();                      // List of structs, each of which contains a composite key and cost of the food it represents.


    // ALG SETUP MENU--------------


    //
    // Algorithm settings
    //

    public int populationSize;
    public int numStartingPortionsPerDay;
    public int minPortionMass;
    public int maxPortionMass;
    public bool addFitnessForMass;
    public string algorithmType;
    public EFitnessApproach fitnessApproach;


    //
    // GA-specific settings
    //
    
    public int mutationMassChangeMin;
    public int mutationMassChangeMax;
    
    // Chance for any portion to be mutated in an algorithm pass.
    // Is divided by the number of portions in the calculation.
    public float changePortionMassMutationProb;
    
    // Impacts determinism. Low value => High determinism, High value => Random chaos
    // 0 or 1 -> No convergence
    public float addOrRemovePortionMutationProb;
    
    // Must be in the range [0, 1]
    public float selectionPressure;
    
    // Set this to 1 for 1-pt crossover, 2 for 2-pt crossover, N for N-pt crossover.
    public int numCrossoverPoints;
    
    // Selection method
    public ESelectionMethod selectionMethod;


    //
    // ACO-specific settings
    //

    // Number of iterations before replacing the worst food.
    // High value => Deeper search (more iterations for evaluation)
    // Low value => Wider search (more foods)
    public int colonyStagnationIters;

    public bool elitist;
    public int colonyPortions;

    public float pheroImportance;
    public float pheroEvapRate;

    // Probability calculation variables
    public float acoAlpha;      // Pheromone exponent
    public float acoBeta;       // Desirability exponent
    public float defaultPheromone;


    //
    // PSO-specific settings
    // For both coefficients, as PSO deals with integer masses, selecting non-integer values will lead
    // to masses getting rounded down to the nearest integer during calculations involving these values.
    //

    /// <summary>
    /// The acceleration coefficient relating particle best and current position.
    /// </summary>
    public float pAccCoefficient;

    /// <summary>
    /// The acceleration coefficient relating global best and current position.
    /// </summary>
    public float gAccCoefficient;
    public float inertialWeight;


    public Preferences()
    {
        Reset();
    }


    /// <summary>
    /// Resets all preferences to their default values.
    /// </summary>
    public void Reset()
    {
        eatsLandMeat = true;
        eatsSeafood = true;
        eatsAnimalProduce = true;
        eatsLactose = true;

        maleElseFemale = true;
        ageYears = 18;
        weightKg = 85f;
        heightCm = 177.5f;

        populationSize = 10;
        numStartingPortionsPerDay = 1;
        minPortionMass = 1;
        maxPortionMass = 500;
        addFitnessForMass = true;
        algorithmType = ALG_TYPES[0];
        fitnessApproach = EFitnessApproach.SummedFitness;

        mutationMassChangeMin = 1;
        mutationMassChangeMax = 10;
        changePortionMassMutationProb = 1f;
        addOrRemovePortionMutationProb = 0.1f;
        selectionPressure = 0.5f;
        numCrossoverPoints = 1;
        selectionMethod = ESelectionMethod.Tournament;

        colonyStagnationIters = 50;
        colonyPortions = 10;
        pheroImportance = 1f;
        pheroEvapRate = 0.1f;
        acoAlpha = 1f;      // Pheromone exponent
        acoBeta = 1f;       // Desirability exponent
        defaultPheromone = 1f;

        pAccCoefficient = 1;
        gAccCoefficient = 1;
        inertialWeight = 1;

        CalculateDefaultConstraints();
    }


    /// <summary>
    /// Calculates constraint values based on user preferences.
    /// </summary>
    public void CalculateDefaultConstraints()
    {
        // Calculates daily calories based on the Harris-Benedict equations.
        float GetDailyCalories()
        {
            if (maleElseFemale)
                return 66.4730f + 13.7516f * weightKg + 5.0033f * heightCm - 6.7550f * ageYears;
            return 655.0955f + 9.5634f * weightKg + 1.8496f * heightCm - 4.6756f * ageYears;
        }

        float kcalGoal = GetDailyCalories();
        SetConstraint(EConstraintType.Kcal, typeof(ConvergeConstraint), max: kcalGoal * 1.5f, weight: 2, goal: kcalGoal);

        // Auto-generate default p/f/c properties based on the above constraint
        ConstraintData proteinData = new();
        ConstraintData fatData = new();
        ConstraintData carbsData = new();

        MathTools.CaloriesToMacros(constraints[(int)EConstraintType.Kcal].Min, ref proteinData.Min, ref fatData.Min, ref carbsData.Min);
        MathTools.CaloriesToMacros(constraints[(int)EConstraintType.Kcal].Max, ref proteinData.Max, ref fatData.Max, ref carbsData.Max);
        MathTools.CaloriesToMacros(constraints[(int)EConstraintType.Kcal].Goal, ref proteinData.Goal, ref fatData.Goal, ref carbsData.Goal);

        SetConstraint(EConstraintType.Protein, typeof(ConvergeConstraint), max: proteinData.Max, weight: 2, goal: proteinData.Goal);
        SetConstraint(EConstraintType.Fat, typeof(ConvergeConstraint), max: fatData.Max, weight: 2, goal: fatData.Goal);
        SetConstraint(EConstraintType.Carbs, typeof(ConvergeConstraint), max: carbsData.Max, weight: 2, goal: carbsData.Goal);

        // Default weight: 3
        SetConstraint(EConstraintType.Sugar, typeof(MinimiseConstraint), max: 30f, weight: 3);
        SetConstraint(EConstraintType.SatFat, typeof(MinimiseConstraint), max: maleElseFemale ? 30f : 20f, weight: 3);
        SetConstraint(EConstraintType.TransFat, typeof(MinimiseConstraint), max: 5f, weight: 3);

        // Default weight: 1
        // For these, set the goal to be the recommended amount from the NHS.
        // The maximum is the highest "definitely safe" amount recommended by the NHS.
        SetConstraint(EConstraintType.Calcium, typeof(ConvergeConstraint), max: 1500f, weight: 1, goal: 700f);

        bool moreIron = !maleElseFemale && (19 <= ageYears && ageYears <= 49);
        SetConstraint(EConstraintType.Iodine, typeof(ConvergeConstraint), max: 500f, weight: 1, goal: 140f);
        SetConstraint(EConstraintType.Iron, typeof(ConvergeConstraint), max: 17f, weight: 1, goal: moreIron ? 14.8f : 8.7f);

        SetConstraint(EConstraintType.VitA, typeof(ConvergeConstraint), max: 1500, weight: 1, goal: maleElseFemale ? 700 : 600);
        SetConstraint(EConstraintType.VitB1, typeof(ConvergeConstraint), max: 100, weight: 1, goal: maleElseFemale ? 1 : 0.8f);
        SetConstraint(EConstraintType.VitB2, typeof(ConvergeConstraint), max: 40f, weight: 1, goal: maleElseFemale ? 1.3f : 1.1f);
        SetConstraint(EConstraintType.VitB3, typeof(ConvergeConstraint), max: 17f, weight: 1, goal: maleElseFemale ? 16.5f : 13.2f);
        SetConstraint(EConstraintType.VitB6, typeof(ConvergeConstraint), max: 10f, weight: 1, goal: maleElseFemale ? 1.4f : 1.2f);
        SetConstraint(EConstraintType.VitB9, typeof(ConvergeConstraint), max: 1000f, weight: 1, goal: isPregnant ? 400f : 200f);
        SetConstraint(EConstraintType.VitB12, typeof(ConvergeConstraint), max: 2000f, weight: 1, goal: 1.5f);
        SetConstraint(EConstraintType.VitC, typeof(ConvergeConstraint), max: 1000f, weight: 1, goal: 40f);
        SetConstraint(EConstraintType.VitD, typeof(ConvergeConstraint), max: 100f, weight: 1, goal: needsVitD ? 10 : 0); // Not needed if sunny.
        SetConstraint(EConstraintType.VitE, typeof(ConvergeConstraint), max: 540f, weight: 1, goal: maleElseFemale ? 4 : 3);
        SetConstraint(EConstraintType.VitK1, typeof(ConvergeConstraint), max: 1000f, weight: 1, goal: weightKg); // Goal: 1 microgram per kg bodyweight.

        // Disable cost by default as it requires user input for each food they enable.
        SetConstraint(EConstraintType.Cost, typeof(NullConstraint), max: 0f, weight: 1);
    }


    private void SetConstraint(EConstraintType nutrient, Type constraintType, float max, float weight, float min = 0, float goal = 0)
    {
        constraints[(int)nutrient] = new() { Min = min, Max = max, Goal = goal, Type = constraintType.FullName!, Weight = weight, NutrientName = $"{nutrient}"};
        Constraint.Build(constraints[(int)nutrient]); // Checks if all params are valid. If not, throws an error.
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
    /// Checks if a composite key has a custom food setting. If so, returns it. If not, throws.
    /// </summary>
    /// <param name="key">The composite key.</param>
    /// <returns>The custom settings, if there are any.</returns>
    /// <exception cref="KeyNotFoundException">exception>
    public CustomFoodSettings TryGetSettings(string key)
    {
        foreach (var cfs in customFoodSettings)
        {
            if (cfs.Key == key) return cfs;
        }

        throw new KeyNotFoundException("Composite key provided has no custom settings.");
    }


    /// <summary>
    /// Gets whether the provided key is banned or not.
    /// </summary>
    /// <param name="key">The key to check.</param>
    public bool IsFoodBanned(string key)
    {
        try
        {
            if (TryGetSettings(key).Banned) return true;
            return false;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }


    /// <summary>
    /// Toggles whether the provided key is banned or not.
    /// If the key has no corresponding settings, then it must not yet be banned.
    /// </summary>
    /// <param name="key">The key to toggle.</param>
    public void ToggleFoodBanned(string key)
    {
        try
        {
            // Get struct from list and update
            CustomFoodSettings settings = TryGetSettings(key);
            settings.Banned = !settings.Banned;
        }
        catch (KeyNotFoundException)
        {
            // If a settings for this food doesn't exist, the food cannot be banned, so ban it
            CustomFoodSettings settings = new()
            {
                Key = key,
                Banned = true
            };

            // Add the new setting
            customFoodSettings.Add(settings);
        }
    }


    /// <summary>
    /// A function to eliminate the vast majority of unacceptable foods by food group, and/or name.
    /// Also removes a food if it was banned specifically by the user.
    /// May still leave some in, for example chicken soup may be under the soup group - WA[A,C,E]
    /// Will exclude all alcohol, as it is not nutritious.
    /// </summary>
    /// <param name="foodGroup">The food group to check, to see if allowed.</param>
    /// <param name="name">The name to check.</param>
    /// <returns>A boolean of whether the provided food is allowed by the user's diet.</returns>
    public bool IsFoodAllowed(string foodGroup, string name, string compositeKey)
    {
        // If a matching setting says the food is banned, then the food is not allowed.
        if (IsFoodBanned(compositeKey)) return false;

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
            str += $"Constraint: {Constraint.Values[i],10}" + constraint.Verbose();
        }

        str += "\n";
        return str;
    }
}