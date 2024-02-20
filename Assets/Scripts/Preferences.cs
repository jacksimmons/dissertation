using System;
using System.Collections.Generic;


public enum WeightGoal
{
    MaintainWeight,
    LoseWeight,
    GainWeight
}


public enum AssignedSex
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

            // This will automatically Cache() the preferences, so no need to update m_instance.
            return Saving.LoadFromFile<Preferences>("Preferences.json");
        }
    }
    public void Cache() { m_instance = this; }


    // Food Groups
    public bool eatsLandMeat; // Carnivore, Lactose-Intolerant
    public bool eatsSeafood; // Carnivore, Pescatarian, LI
    public bool eatsAnimalProduce; // Vegetarian, LI
    public bool eatsLactose; // Vegetarian, i.e. no Milk


    // Body
    public WeightGoal weightGoal;
    public float weightInKG;
    public float heightInCM;
    public AssignedSex assignedSex;

    //
    // Parameters for algorithm
    //

    // Params for each constraint (indices matching enum values)
    public float[] goals;
    public float[] tolerances;
    public float[] steepnesses;
    public ConstraintType[] constraintTypes;
    public int populationSize;
    public int numStartingPortionsPerDay;
    public int portionMinStartMass;
    public int portionMaxStartMass;
    public bool addFitnessForMass;

    // The type of algorithm to use (and subtypes)
    public AlgorithmType algType;
    public GAType gaType;


    // By default, the user's settings should permit every food type - this
    // best fits the average person.
    public Preferences()
    {
        eatsLandMeat = true;
        eatsSeafood = true;
        eatsAnimalProduce = true;
        eatsLactose = true;
        weightGoal = WeightGoal.MaintainWeight;
        weightInKG = 70;
        heightInCM = 170;
        assignedSex = AssignedSex.Male;

        goals = new float[Nutrients.Count];
        tolerances = new float[Nutrients.Count];
        steepnesses = new float[Nutrients.Count];

        for (int i = 0; i < Nutrients.Count; i++)
        {
            tolerances[i] = 1;
            steepnesses[i] = 0.001f;
        }
        
        constraintTypes = new ConstraintType[Nutrients.Count];
        populationSize = 10;
        numStartingPortionsPerDay = 1;
        portionMinStartMass = 50;
        portionMaxStartMass = 150;
        addFitnessForMass = true;
        algType = AlgorithmType.GA;

        gaType = GAType.SummedFitness;

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
}