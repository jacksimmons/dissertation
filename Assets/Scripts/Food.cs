using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Progress;


/// <summary>
/// A class for the properties of a food type. E.g. Banana.
/// </summary>
public class Food
{
    public readonly string Name;
    public readonly string Description;
    public readonly string FoodGroup;
    public readonly string Reference;

    public readonly Dictionary<Proximate, float> Nutrients;


    public Food(string name, string desc, string group, string reference, Dictionary<Proximate, float> nutrients)
    {
        Name = name;
        Description = desc;
        FoodGroup = group;
        Reference = reference;
        Nutrients = nutrients;
    }


    public static string GetProximateUnit(Proximate proximate)
    {
        switch (proximate)
        {
            case Proximate.Protein:
            case Proximate.Fat:
            case Proximate.Carbs:
            case Proximate.Sugar:
            case Proximate.SatFat:
            case Proximate.TransFat:
                return "g";
            case Proximate.Kcal:
                return "kcal";
            default:
                throw new ArgumentOutOfRangeException(nameof(proximate));
        }
    }


    public override string ToString()
    {
        return $"Name: {Name}\nDescription: {Description}\nFood Group: {FoodGroup}"
             + $"\nReference: {Reference}\n{NutrientsToString()}";
    }


    public string NutrientsToString()
    {
        string nutrientsString = "";
        Proximate[] proximates = (Proximate[])Enum.GetValues(typeof(Proximate));
        for (int i = 0; i < proximates.Length; i++)
        {
            // Add newline before second and further lines
            if (i > 0)
                nutrientsString += "\n";
            nutrientsString += $"{proximates[i]}: {Nutrients[proximates[i]]}{GetProximateUnit(proximates[i])}";
        }
        return nutrientsString;
    }
}


/// <summary>
/// A class with a type of food (100g), multiplied by a quantity.
/// </summary>
public class Portion
{
    public readonly Food Food;

    public float Mass
    {
        get { return 100 * Multiplier; }
        set { Multiplier = value / 100; }
    }
    public float Multiplier { get; private set; }


    public Portion(Food food, float mass)
    {
        Food = food;
        Mass = mass;
    }


    public float _ApproximateFitness(Dictionary<Proximate, Constraint> constraints, int idealNumPortionsPerDay)
    {
        float rawFitness = 0;
        foreach (Proximate proximate in constraints.Keys)
        {
            // An approximation for the amount of nutrient that would be consumed in an entire day of eating said portion
            float approxDayQuantity = Food.Nutrients[proximate] * Multiplier * idealNumPortionsPerDay;
            rawFitness += constraints[proximate]._GetFitness(approxDayQuantity);
        }
        return rawFitness;
    }
}


/// <summary>
/// A class for all the food eaten in a day.
/// </summary>
public class Day
{
    public ReadOnlyCollection<Portion> Portions;
    private readonly List<Portion> _portions;


    public Day()
    {
        _portions = new();
        Portions = new(_portions);
    }


    public void RemovePortion(Portion portion)
    {
        _portions.Remove(portion);
    }


    public void AddPortion(Portion portion)
    {
        _portions.Add(portion);
    }


    /// <summary>
    /// Evaluates the fitness of a Day through summing fitness of each nutrient amount (ideally), then approximate
    /// evaluation if that yields infinity.
    /// </summary>
    /// <returns>The fitness as a <class>Fitness</class> object.</returns>
    public Fitness _GetFitness(Dictionary<Proximate, Constraint> constraints, int idealNumPortionsPerDay)
    {
        // Calculate the overall fitness value based on the sum of the fitness of the individual
        // nutrient amounts. (E.g. protein leads to a fitness value, which is added to the fat fitness,
        // etc... over all nutrients).
        // This fitness evaluation is more accurate, hence it is weighted more favourably
        float rawFitness = 0;
        FitnessLevel fitnessLevel = FitnessLevel.Day;
        foreach (Proximate proximate in constraints.Keys)
        {
            float amount = _GetProximateAmount(proximate);
            rawFitness += constraints[proximate]._GetFitness(amount);
        }

        // If the evaluation was infinity, it was not useful.
        // Therefore we increase (worsen) the fitness level and approximate at the Portion level.
        if (rawFitness == float.PositiveInfinity)
        {
            fitnessLevel = FitnessLevel.Portion;

            if (Portions.Count == 0)
            {
                // An empty day should have infinitely bad fitness - starvation is not a good diet.
                return new(fitnessLevel, float.PositiveInfinity);
            }

            rawFitness = 0;
            foreach (Portion portion in Portions)
            {
                rawFitness += portion._ApproximateFitness(constraints, idealNumPortionsPerDay);
            }
        }

        return new(fitnessLevel, rawFitness);
    }


    public float _GetProximateAmount(Proximate proximate)
    {
        float quantity = 0;
        foreach (Portion portion in Portions)
        {
            quantity += portion.Food.Nutrients[proximate] * portion.Multiplier;
        }
        return quantity;
    }
}


//    //public float Fitness(List<Constraint> constraints, int numMeals, int numPortions)
//    //{
//    //    float fitness = 0f;
//    //    foreach (Constraint constraint in constraints)
//    //    {
//    //        // Multiply the parameter by the number of portions & number of meals to calculate average fitness
//    //        fitness += constraint.GetFitness(GetProximate(constraint.Parameter) * numMeals * numPortions);
//    //    }
//    //    return fitness;
//    //}


//    public float TotalFitness(List<Constraint> constraints, int numMeals)
//    {
//        float mealFitness = 0f;
//        foreach (Portion p in Portions)
//        {
//            mealFitness += p.Fitness(constraints, numMeals, Portions.Count);
//        }

//        // Inconvenience penalty based on number of portions
//        mealFitness -= MathF.Pow(Portions.Count, 2);

//        LastFitness = mealFitness;
//        return mealFitness;
//    }


//    public void Print()
//    {
//        foreach (Portion portion in Portions)
//        {
//            portion.Print();
//        }

//        if (Mutations > 0)
//            Console.WriteLine($"[Mutations: {Mutations}]");
//    }


//    public static int Compare(Meal a, Meal b)
//    {
//        if (a.LastFitness < b.LastFitness)
//            return -1;
//        else if (a.LastFitness == b.LastFitness)
//            return 0;
//        else
//            return 1;
//    }
//}
