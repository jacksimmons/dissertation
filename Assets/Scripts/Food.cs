using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;


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

    public readonly Dictionary<Proximate, float> Proximates;


    public Food(string name, string desc, string group, string reference, Dictionary<Proximate, float> nutrients)
    {
        Name = name;
        Description = desc;
        FoodGroup = group;
        Reference = reference;
        Proximates = nutrients;
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


    //public override string ToString()
    //{
    //    return $"Name: {Name}\nDescription: {Description}\nFood Group: {FoodGroup}"
    //         + $"\nReference: {Reference}\n{NutrientsToString()}";
    //}


    //public string NutrientsToString()
    //{
    //    string nutrientsString = "";
    //    for (int i = 0; i < Nutrients.EnumLengths[typeof(Proximate)]; i++)
    //    {
    //        // Add newline before second and further lines
    //        if (i > 0)
    //            nutrientsString += "\n";
    //        nutrientsString += $"{(Proximate)i}: {Proximates[(Proximate)i]}{GetProximateUnit((Proximate)i)}";
    //    }
    //    return nutrientsString;
    //}
}


/// <summary>
/// A class with a type of food (100g), multiplied by a quantity.
/// </summary>
public class Portion
{
    public readonly Food Food;

    public int Mass
    {
        get { return Mathf.RoundToInt(100 * Multiplier); }
        set
        {
            if (value <= 0)
                Debug.LogError($"Mass was not positive: {value}");
            Multiplier = (float)value / 100;
        }
    }
    public float Multiplier { get; private set; }


    public Portion(Food food, int mass)
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
        for (int i = 0; i < Nutrients.EnumLengths[typeof(Proximate)]; i++)
        {
            Proximate proximate = (Proximate)i;
            float proximateAmount = Food.Proximates[proximate];
            asString += $"{proximate}: {proximateAmount * Multiplier}{Food.GetProximateUnit(proximate)}\n";
        }
        return asString + $"Mass: {Mass}g";
    }
}


public enum ParetoComparison
{
    StrictlyDominates,
    Dominates,
    MutuallyNonDominating,
    Dominated,
    StrictlyDominated
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


    public Day(Day day)
    {
        _portions = new(day.Portions);
        Portions = new(_portions);
    }


    public void AddPortion(Portion portion)
    {
        _portions.Add(portion);
    }


    /// <summary>
    /// Tries to remove the portion from the list. If it is the last remaining portion,
    /// it will not be removed.
    /// </summary>
    /// <param name="portion">The portion to remove if it is not the only one left.</param>
    public void RemovePortion(Portion portion)
    {
        if (_portions.Count <= 1)
        {
            Debug.LogWarning("No portion was removed. A Day must have at least one portion.");
            return;
        }
        _portions.Remove(portion);
    }


    /// <summary>
    /// Compares two days, and returns a detailed enum value on the dominance hierarchy between
    /// the two.
    /// </summary>
    public static ParetoComparison Compare(Day a, Day b)
    {
        // Store how many constraints this is better/worse than `day` on.
        int betterCount = 0;
        int worseCount = 0;

        // This loop exits if this has better or equal fitness on every constraint.
        foreach (Proximate proximate in Algorithm.Instance.Constraints.Keys)
        {
            float amountA = a.GetProximateAmount(proximate);
            float fitnessA = Algorithm.Instance.Constraints[proximate]._GetFitness(amountA);

            float amountB = b.GetProximateAmount(proximate);
            float fitnessB = Algorithm.Instance.Constraints[proximate]._GetFitness(amountB);

            if (fitnessA < fitnessB)
                betterCount++;
            else if (fitnessA > fitnessB)
                worseCount++;
        }


        // If on any constraint, !(ourFitness <= fitness) then this does not dominate.
        if (worseCount > 0)
        {
            // Worse on 1+, and better on 1+ => MND
            if (betterCount > 0)
                return ParetoComparison.MutuallyNonDominating;

            // Worse on all => Strictly Dominated
            if (worseCount == Algorithm.Instance.GetNumConstraints())
                return ParetoComparison.StrictlyDominated;

            // Worse on 1+, and not better on any => Dominated
            return ParetoComparison.Dominated;
        }
        else
        {
            // Better on all => Strictly Dominates
            if (betterCount == Algorithm.Instance.GetNumConstraints())
                return ParetoComparison.StrictlyDominates;

            // Not worse on any, and better on 1+ => Dominates
            if (betterCount > 0)
                return ParetoComparison.Dominates;

            // Not worse on any, and not better on any => MND (They are equal)
            return ParetoComparison.MutuallyNonDominating;
        }
    }


    /// <summary>
    /// A simpler form of Compare, which merges strict domination and regular domination, to simplify
    /// usage of the ParetoComparison enum.
    /// Allows conversion of:
    /// 
    /// switch (Compare(a, b)) {
    ///     case ParetoComparison.StrictlyDominates:
    ///     case ParetoComparison.Dominates:
    ///         ...
    /// }
    /// 
    /// Into:
    /// switch (SimpleCompare(a,b)) {
    ///     case ParetoComparison.Dominates:
    ///         ...
    /// }
    /// </summary>
    public static ParetoComparison SimpleCompare(Day a, Day b)
    {
        return Compare(a, b) switch
        {
            ParetoComparison.StrictlyDominates or ParetoComparison.Dominates => ParetoComparison.Dominates,
            ParetoComparison.StrictlyDominated or ParetoComparison.Dominated => ParetoComparison.Dominated,
            _ => ParetoComparison.MutuallyNonDominating,
        };
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

        for (int i = 0; i < Nutrients.EnumLengths[typeof(Proximate)]; i++)
        {
            Proximate proximate = (Proximate)i;
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
            quantity += portion.Food.Proximates[proximate] * portion.Multiplier;
        }
        return quantity;
    }


    public float GetDistance(Day day)
    {
        // Square root of all differences squared = Pythagorean Distance
        Proximate[] vals = (Proximate[])Enum.GetValues(typeof(Proximate));
        return Mathf.Sqrt(vals.Sum(o => Mathf.Pow(GetProximateAmount(o) - day.GetProximateAmount(o), 2)));
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


//    public static int CompareTo(Meal a, Meal b)
//    {
//        if (a.LastFitness < b.LastFitness)
//            return -1;
//        else if (a.LastFitness == b.LastFitness)
//            return 0;
//        else
//            return 1;
//    }
//}
