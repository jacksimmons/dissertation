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
/// A class which represents the properties of a 100g portion of
/// a specific food from the dataset.
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


    /// <summary>
    /// Gets a verbose description of this portion.
    /// </summary>
    /// <returns>The string data.</returns>
    public string Verbose()
    {
        string asString = $"Name: {Food.Name}\n";
        foreach (Proximate proximate in Enum.GetValues(typeof(Proximate)))
        {
            float proximateAmount = Food.Nutrients[proximate];
            asString += 
                proximate.ToString() + $": {proximateAmount * Multiplier}{Food.GetProximateUnit(proximate)}"
                + $" (Fitness = {Algorithm.Instance.Constraints[proximate]._GetFitness(proximateAmount * Multiplier)})\n";
        }
        return asString + $"Mass: {Mass}g";
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


    public void AddPortion(Portion portion)
    {
        _portions.Add(portion);
    }


    public void RemovePortion(Portion portion)
    {
        _portions.Remove(portion);
    }


    public bool Dominates(Day day)
    {
        bool betterOnOne = false;

        // This loop exits if this has better or equal fitness on every constraint.
        foreach (Proximate proximate in Algorithm.Instance.Constraints.Keys)
        {
            float amount = day.GetProximateAmount(proximate);
            float fitness = Algorithm.Instance.Constraints[proximate]._GetFitness(amount);

            float ourAmount = GetProximateAmount(proximate);
            float ourFitness = Algorithm.Instance.Constraints[proximate]._GetFitness(ourAmount);

            // To dominate, our fitness must be strictly lower on at least one constraint.
            if (ourFitness < fitness)
                betterOnOne = true;

            // If on any constraint, !(ourFitness <= fitness) then this does not dominate.
            if (ourFitness > fitness)
                return false;
        }

        // Only return true if our fitness is strictly better on one constraint, and equal or better
        // on every other constraint (which is true if the loop exits).
        return betterOnOne;
    }


    /// <summary>
    /// Evaluates the fitness of this day.
    /// </summary>
    /// <returns>The fitness as a float.</returns>
    public float GetFitness()
    {
        // Calculate the overall fitness value based on the sum of the fitness of the individual
        // nutrient amounts. (E.g. protein leads to a fitness value, which is added to the fat fitness,
        // etc... over all nutrients).
        // This fitness evaluation is more accurate, hence it is weighted more favourably
        float fitness = 0;
        foreach (Proximate proximate in Algorithm.Instance.Constraints.Keys)
        {
            float amount = GetProximateAmount(proximate);
            fitness += Algorithm.Instance.Constraints[proximate]._GetFitness(amount);
        }

        return fitness;
    }


    public float GetProximateAmount(Proximate proximate)
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
