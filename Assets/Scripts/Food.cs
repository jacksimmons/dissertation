using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EA;


public enum Proximate
{
    Energy,
    Protein,
    Fat, Saturates, Trans,
    Carbs, Sugars
}


/// <summary>
/// Data for each 100g portion of food.
/// </summary>
public class Portion
{
    // Raw attributes of the food, per 100g.
    private readonly float _energy;
    private readonly float _protein;
    private readonly float _fat, _saturates, _trans;
    private readonly float _carbs, _sugars;

    public string Name;

    /// <summary>
    /// Scales every food property, allowing different portion sizes.
    /// </summary>
    public float Scale;


    /// <summary>
    /// In Kilocalories (Kcal)
    /// </summary>
    public float Energy { get => _energy * Scale; }

    public float Protein { get => _protein * Scale; }
    public float Fat { get => _fat * Scale; }
    public float Carbs { get => _carbs * Scale; }

    public float Saturates { get => _saturates * Scale; }
    public float Trans { get => _trans * Scale; }

    public float Sugars { get => _sugars * Scale; }


    public Portion(Portion old)
    {
        Name = old.Name;
        Scale = old.Scale;
        
        _energy = old._energy;
        _protein = old._protein;
        _fat = old._fat;
        _carbs = old._carbs;
        
        _saturates = old._saturates;
        _trans = old._trans;

        _sugars = old._sugars;
    }


    public Portion(string name, float energy, float protein, float fat, float carbs, 
        float saturates, float trans, float sugars, float scale = 1)
    {
        Name = name;
        _energy = energy;
        _protein = protein;
        _fat = fat;
        _carbs = carbs;
        
        _saturates = saturates;
        _trans = trans;
        
        _sugars = sugars;

        Scale = scale;
    }


    public float GetProximate(Proximate proximate)
    {
        switch (proximate)
        {
            case Proximate.Energy:
                return Energy;
            case Proximate.Fat:
                return Fat;
            case Proximate.Protein:
                return Protein;
            case Proximate.Carbs:
                return Carbs;
            case Proximate.Saturates:
                return Saturates;
            case Proximate.Trans:
                return Trans;
            case Proximate.Sugars:
                return Sugars;
            default:
                return 0f;
        }
    }


    public float Fitness(List<Constraint> constraints, int numMeals, int numPortions)
    {
        float fitness = 0f;
        foreach (Constraint constraint in constraints)
        {
            // Multiply the parameter by the number of portions & number of meals to calculate average fitness
            fitness += constraint.GetFitness(GetProximate(constraint.Parameter) * numMeals * numPortions);
        }
        return fitness;
    }


    public void Print()
    {
        Console.WriteLine($"{Name}\n Energy: {Energy}kcal, Protein: {Protein}g, Fat: {Fat}g (Saturates: {Saturates}g) (Trans: {Trans}g), Carbs: {Carbs}g (Sugars: {Sugars}g)");
    }
}


public class Meal
{
    public List<Portion> Portions { get; private set; } = new();
    public int Mutations { get; set; } = 0;


    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Meal(Meal old)
    {
        foreach (Portion portion in old.Portions)
        {
            Portions.Add(new(portion));
        }

        LastFitness = old.LastFitness;
        Mutations = old.Mutations;
    }


    public Meal() { }


    /// <summary>
    /// Cached fitness value, not indicative of current fitness.
    /// </summary>
    public float LastFitness { get; private set; } = float.MinValue;


    public float TotalFitness(List<Constraint> constraints, int numMeals)
    {
        float mealFitness = 0f;
        foreach (Portion p in Portions)
        {
            mealFitness += p.Fitness(constraints, numMeals, Portions.Count);
        }

        // Inconvenience penalty based on number of portions
        mealFitness -= MathF.Pow(Portions.Count, 2);

        LastFitness = mealFitness;
        return mealFitness;
    }


    public void Print()
    {
        foreach (Portion portion in Portions)
        {
            portion.Print();
        }

        if (Mutations > 0)
            Console.WriteLine($"[Mutations: {Mutations}]");
    }


    public static int Compare(Meal a, Meal b)
    {
        if (a.LastFitness < b.LastFitness)
            return -1;
        else if (a.LastFitness == b.LastFitness)
            return 0;
        else
            return 1;
    }
}
