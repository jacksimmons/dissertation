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


public enum ConstraintType
{
    Null,
    Minimise,
    Converge
}


public enum AlgorithmType
{
    GA,
    ACO
}


public enum GAType
{
    SummedFitness,
    ParetoDominance,
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


    //
    // Food Groups
    //
    public bool eatsLandMeat; // Carnivore, Lactose-Intolerant
    public bool eatsSeafood; // Carnivore, Pescatarian, LI
    public bool eatsAnimalProduce; // Vegetarian, LI
    public bool eatsLactose; // Vegetarian, i.e. no Milk

    //
    // Body
    //
    public EWeightGoal weightGoal;
    public float weightInKG;
    public float heightInCM;
    public EAssignedSex assignedSex;

    //
    // Params for each constraint (indices matching enum values)
    //
    public float[] goals;
    public float[] tolerances;
    public float[] steepnesses;
    public ConstraintType[] constraintTypes;


    // ALG SETUP MENU--------------


    //
    // Algorithm settings
    //
    public int populationSize;
    public int numStartingPortionsPerDay;
    public int portionMinStartMass;
    public int portionMaxStartMass;
    public bool addFitnessForMass;
    public AlgorithmType algType;

    //
    // GA-specific settings
    //
    public GAType gaType;

    //
    // ACO-specific settings
    //
    public float pheroImportance;
    public float pheroEvapRate;
    // Probability calculation variables
    public float acoAlpha;      // Pheromone exponent
    public float acoBeta;       // Desirability exponent


    // By default, the user's settings should permit every food type - this
    // best fits the average person.
    public Preferences()
    {
        eatsLandMeat = true;
        eatsSeafood = true;
        eatsAnimalProduce = true;
        eatsLactose = true;
        weightGoal = EWeightGoal.MaintainWeight;
        weightInKG = 70;
        heightInCM = 170;
        assignedSex = EAssignedSex.Male;

        goals = new float[Nutrients.Count];
        tolerances = new float[Nutrients.Count];
        steepnesses = new float[Nutrients.Count];
        constraintTypes = new ConstraintType[Nutrients.Count];

        for (int i = 0; i < Nutrients.Count; i++)
        {
            tolerances[i] = 1;
            steepnesses[i] = 0.001f;
            constraintTypes[i] = ConstraintType.Minimise;
        }

        // Default values (provides a good basis without customisation)
        goals[(int)Nutrient.Protein] = 200;
        goals[(int)Nutrient.Fat] = 100;
        goals[(int)Nutrient.Carbs] = 375;
        goals[(int)Nutrient.Kcal] = 3200;
        goals[(int)Nutrient.Sugar] = 30;
        goals[(int)Nutrient.SatFat] = 30;
        goals[(int)Nutrient.TransFat] = 5;
        goals[(int)Nutrient.Calcium] = 100;
        goals[(int)Nutrient.Iron] = 100;
        goals[(int)Nutrient.Iodine] = 100;

        tolerances[(int)Nutrient.Protein] = 200;
        tolerances[(int)Nutrient.Kcal] = 3000;

        constraintTypes[(int)Nutrient.Kcal] = ConstraintType.Converge;
        constraintTypes[(int)Nutrient.Protein] = ConstraintType.Converge;

        populationSize = 10;
        numStartingPortionsPerDay = 1;
        portionMinStartMass = 50;
        portionMaxStartMass = 150;
        addFitnessForMass = true;
        algType = AlgorithmType.GA;

        gaType = GAType.SummedFitness;

        pheroImportance = 0.5f;
        pheroEvapRate = 0.1f;
        acoAlpha = 1;
        acoBeta = 1;
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
        string foodGroup = food.foodGroup;

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
        string name = food.name.ToLower();

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
        foreach (Nutrient nutrient in Nutrients.Values)
        {
            str += $"{nutrient} - Goal[{goals[(int)nutrient]}] Tolerance[{tolerances[(int)nutrient]}] Steepness[{steepnesses[(int)nutrient]}]\n";
        }

        str += "\n";
        return str;
    }
}