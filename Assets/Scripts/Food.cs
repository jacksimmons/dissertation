using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


/// <summary>
/// A class for the properties of a food type. E.g. Banana.
/// </summary>
public class Food
{
    public readonly string Name;
    public readonly string Description;
    public readonly string FoodGroup;
    public readonly string Reference;

    public readonly Dictionary<ProximateType, float> Nutrients;


    public Food(string name, string desc, string group, string reference, Dictionary<ProximateType, float> nutrients)
    {
        Name = name;
        Description = desc;
        FoodGroup = group;
        Reference = reference;
        Nutrients = nutrients;
    }


    public override string ToString()
    {
        return $"Name: {Name}\nDescription: {Description}\nFood Group: {FoodGroup}"
             + $"\nReference: {Reference}\n{NutrientsToString()}";
    }


    public string NutrientsToString()
    {
        string nutrientsString = "";
        ProximateType[] proximates = (ProximateType[])Enum.GetValues(typeof(ProximateType));
        for (int i = 0; i < proximates.Length; i++)
        {
            // Add newline before second and further lines
            if (i > 0)
                nutrientsString += "\n";
            nutrientsString += $"{proximates[i]}: {Nutrients[proximates[i]]}{GetProximateUnit(proximates[i])}";
        }
        return nutrientsString;
    }


    public string GetProximateUnit(ProximateType proximate)
    {
        switch (proximate)
        {
            case ProximateType.Protein:
            case ProximateType.Fat:
            case ProximateType.Carbs:
            case ProximateType.Sugar:
            case ProximateType.SatFat:
            case ProximateType.TransFat:
                return "g";
            case ProximateType.Kcal:
                return "kcal";
            default:
                // T! Tested, but need to add to suite
                throw new ArgumentOutOfRangeException(nameof(proximate));
        }
    }
}


/// <summary>
/// A class with a type of food (100g), multiplied by a quantity.
/// </summary>
public class Portion
{
    public readonly Food Food;
    public readonly float Quantity;


    public Portion(Food food, float quantity)
    {
        Food = food;
        Quantity = quantity;
    }


    public override string ToString()
    {
        string str = Food.ToString();
        str += $"\nMass: {Quantity * 100}g";
        return str;
    }
}


/// <summary>
/// A class for all the food eaten in a day.
/// </summary>
public class Day
{
    public ReadOnlyCollection<Portion> Portions;
    private readonly List<Portion> _portions;


    public Day(List<Portion> portions)
    {
        _portions = portions;
        Portions = new(_portions);
    }


    /// <summary>
    /// Calculates the overall fitness value based on each portion.
    /// Calculated as a nested sum of fitness for each individual nutrient for each portion.
    /// </summary>
    /// <returns>The fitness value, lower is better.</returns>
    public float GetFitness(Dictionary<ProximateType, Constraint> constraints)
    {
        float fitness = 0;
        foreach (Portion portion in Portions)
        {
            foreach (ProximateType proximate in Enum.GetValues(typeof(ProximateType)))
            {
                fitness += constraints[proximate].GetFitness(portion.Food.Nutrients[proximate] * portion.Quantity);
            }
        }
        return fitness;
    }


    public override string ToString()
    {
        string str = "";
        for (int i = 0; i < Portions.Count; i++)
        {
            if (i > 0)
                str += "\n";
            str += $"Portion {i}:\n{Portions[i]}";
        }
        return str;
    }
}


//public class Day
//{
//    public ReadOnlyCollection<Meal> Meals;
//    private readonly List<Meal> _meals;


//    public Day(List<Meal> meals)
//    {
//        _meals = meals;
//        Meals = new(_meals);
//    }
//}



///// <summary>
///// Data for each 100g portion of food.
///// </summary>
//public class Portion
//{
//    // Raw attributes of the food, per 100g.
//    private readonly float _energy;
//    private readonly float _protein;
//    private readonly float _fat, _saturates, _trans;
//    private readonly float _carbs, _sugars;

//    public string Name;

//    /// <summary>
//    /// Scales every food property, allowing different portion sizes.
//    /// </summary>
//    public float Scale;


//    /// <summary>
//    /// In Kilocalories (Kcal)
//    /// </summary>
//    public float Energy { get => _energy * Scale; }

//    public float Protein { get => _protein * Scale; }
//    public float Fat { get => _fat * Scale; }
//    public float Carbs { get => _carbs * Scale; }

//    public float Saturates { get => _saturates * Scale; }
//    public float Trans { get => _trans * Scale; }

//    public float Sugars { get => _sugars * Scale; }


//    public Portion(Portion old)
//    {
//        Name = old.Name;
//        Scale = old.Scale;
        
//        _energy = old._energy;
//        _protein = old._protein;
//        _fat = old._fat;
//        _carbs = old._carbs;
        
//        _saturates = old._saturates;
//        _trans = old._trans;

//        _sugars = old._sugars;
//    }


//    public Portion(string name, float energy, float protein, float fat, float carbs, 
//        float saturates, float trans, float sugars, float scale = 1)
//    {
//        Name = name;
//        _energy = energy;
//        _protein = protein;
//        _fat = fat;
//        _carbs = carbs;
        
//        _saturates = saturates;
//        _trans = trans;
        
//        _sugars = sugars;

//        Scale = scale;
//    }


//    //public float GetProximate(Proximate proximate)
//    //{
//    //    switch (proximate)
//    //    {
//    //        case Proximate.Energy:
//    //            return Energy;
//    //        case Proximate.Fat:
//    //            return Fat;
//    //        case Proximate.Protein:
//    //            return Protein;
//    //        case Proximate.Carbs:
//    //            return Carbs;
//    //        case Proximate.Saturates:
//    //            return Saturates;
//    //        case Proximate.Trans:
//    //            return Trans;
//    //        case Proximate.Sugars:
//    //            return Sugars;
//    //        default:
//    //            return 0f;
//    //    }
//    //}


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


//    public void Print()
//    {
//        Console.WriteLine($"{Name}\n Energy: {Energy}kcal, Protein: {Protein}g, Fat: {Fat}g (Saturates: {Saturates}g) (Trans: {Trans}g), Carbs: {Carbs}g (Sugars: {Sugars}g)");
//    }
//}


//public class Meal
//{
//    public List<Portion> Portions { get; private set; } = new();
//    public int Mutations { get; set; } = 0;


//    /// <summary>
//    /// Copy constructor.
//    /// </summary>
//    public Meal(Meal old)
//    {
//        foreach (Portion portion in old.Portions)
//        {
//            Portions.Add(new(portion));
//        }

//        LastFitness = old.LastFitness;
//        Mutations = old.Mutations;
//    }


//    public Meal() { }


//    /// <summary>
//    /// Cached fitness value, not indicative of current fitness.
//    /// </summary>
//    public float LastFitness { get; private set; } = float.MinValue;


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
