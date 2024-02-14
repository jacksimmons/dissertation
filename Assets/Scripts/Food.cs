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

    public readonly float[] Nutrients;


    public Food(string name, string desc, string group, string reference, float[] nutrients)
    {
        Name = name;
        Description = desc;
        FoodGroup = group;
        Reference = reference;
        Nutrients = nutrients;
    }


    public Food(FoodData data)
    {
        Name = data.Name;
        Description = data.Description;
        FoodGroup = data.Group;
        Reference = data.Reference;
        Nutrients = data.Nutrients;
    }


    public bool IsEqualTo(Food other)
    {
        // Check all simple attribs are the same
        bool attribs = Name == other.Name && Description == other.Description && FoodGroup == other.FoodGroup && Reference == other.Reference;

        if (!attribs) return false;

        // Check all nutrients are the same
        for (int i = 0; i < Nutrients.Length; i++)
        {
            if (Nutrients[i] != other.Nutrients[i])
                return false;
        }
        return true;
    }


    //public override string ToString()
    //{
    //    return $"Name: {Name}\nDescription: {Description}\nFood Group: {FoodGroup}"
    //         + $"\nReference: {Reference}\n{NutrientsToString()}";
    //}


    //public string NutrientsToString()
    //{
    //    string nutrientsString = "";
    //    for (int i = 0; i < Nutrients.Count; i++)
    //    {
    //        // Add newline before second and further lines
    //        if (i > 0)
    //            nutrientsString += "\n";
    //        nutrientsString += $"{(Nutrient)i}: {Nutrients[(Nutrient)i]}{GetNutrientUnit((Nutrient)i)}";
    //    }
    //    return nutrientsString;
    //}
}


/// <summary>
/// A struct with a type of food (100g), multiplied by a quantity.
/// </summary>
public struct Portion
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
        Multiplier = (float)mass/100;
    }


    public float GetNutrientAmount(Nutrient nutrient)
    {
        return Food.Nutrients[(int)nutrient] * Multiplier;
    }


    public bool IsEqualTo(Portion other)
    {
        return Food.IsEqualTo(other.Food) && Multiplier == other.Multiplier;
    }


    /// <summary>
    /// Gets a verbose description of this portion.
    /// </summary>
    /// <returns>The string data.</returns>
    public string Verbose()
    {
        string asString = $"Name: {Food.Name}\n";
        for (int i = 0; i < Nutrients.Count; i++)
        {
            Nutrient nutrient = (Nutrient)i;
            float nutrientAmount = Food.Nutrients[i];
            asString += $"{nutrient}: {nutrientAmount * Multiplier}{Nutrients.GetUnit(nutrient)}\n";
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
    public readonly ReadOnlyCollection<Portion> Portions;
    private readonly List<Portion> _portions;

    private bool _isFitnessUpdated;
    private float _fitness;

    // Attempts to return cached fitness for performance, but if the Day is not up to date
    // it will calculate the fitness manually.
    public float Fitness
    {
        get
        {
            if (_isFitnessUpdated) return _fitness;

            _fitness = GetFitness();
            _isFitnessUpdated = true;
            return _fitness;
        }
    }


    private readonly float[] _nutrientAmounts = new float[Nutrients.Count];


    public Day()
    {
        _portions = new();
        Portions = new(_portions);
    }


    public Day(Day day)
    {
        _portions = new();
        Portions = new(_portions);

        foreach (Portion portion in day.Portions)
        {
            AddPortion(portion);
        }

        _fitness = day.Fitness;
    }


    public void AddPortion(Portion portion)
    {
        _isFitnessUpdated = false;

        AddPortionAmounts(portion);

        bool merged = false;

        // Merge new portion with an existing one if it has the same name.
        for (int i = 0; i < Portions.Count; i++)
        {
            Portion existing = Portions[i];
            if (existing.Food.Name == portion.Food.Name)
            {
                existing.Mass += portion.Mass;
                merged = true;
                _portions[i] = existing;
            }
        }

        // Otherwise, add the portion (it has a unique food)
        if (!merged)
            _portions.Add(portion);
    }


    /// <summary>
    /// Tries to remove the portion from the list. If it is the last remaining portion,
    /// it will not be removed.
    /// </summary>
    /// <param name="portion">The portion to remove if it is not the only one left.</param>
    public void RemovePortion(int index)
    {
        if (_portions.Count <= 1)
        {
            Debug.LogWarning("No portion was removed. A Day must have at least one portion.");
            return;
        }

        _isFitnessUpdated = false;

        SubtractPortionAmounts(Portions[index]);
        _portions.RemoveAt(index);
    }


    public void AddToPortionMass(int index, int dm)
    {
        _isFitnessUpdated = false;

        // Add mass to the portion
        Portion p = _portions[index];
        p.Mass += dm;
        _portions[index] = p;

        // Create dummy portion with difference in mass
        Portion dummy = new(p.Food, dm);
        AddPortionAmounts(dummy);
    }


    public void SetPortionMass(int index, int mass)
    {
        _isFitnessUpdated = false;

        int dm = mass - _portions[index].Mass;
        AddToPortionMass(index, dm);
    }


    private void AddPortionAmounts(Portion portion)
    {
        for (int i = 0; i < Nutrients.Count; i++)
        {
            _nutrientAmounts[i] += portion.GetNutrientAmount((Nutrient)i);
        }
    }


    private void SubtractPortionAmounts(Portion portion)
    {
        for (int i = 0; i < Nutrients.Count; i++)
        {
            _nutrientAmounts[i] -= portion.GetNutrientAmount((Nutrient)i);
        }
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
        int numConstraints = Algorithm.Instance.GetNumConstraints();

        // This loop exits if this has better or equal fitness on every constraint.
        for (int i = 0; i < Nutrients.Count; i++)
        {
            // Quick exit for null constraints
            if (Algorithm.Instance.Constraints[i].GetType() == typeof(NullConstraint))
                continue;

            float amountA = a.GetNutrientAmount((Nutrient)i);
            float fitnessA = Algorithm.Instance.Constraints[i]._GetFitness(amountA);

            float amountB = b.GetNutrientAmount((Nutrient)i);
            float fitnessB = Algorithm.Instance.Constraints[i]._GetFitness(amountB);

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
            if (worseCount == numConstraints)
                return ParetoComparison.StrictlyDominated;

            // Worse on 1+, and not better on any => Dominated
            return ParetoComparison.Dominated;
        }
        else
        {
            // Better on all => Strictly Dominates
            if (betterCount == numConstraints)
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
        return Pareto.SimplifyComparison(Compare(a, b));
    }


    /// <summary>
    /// Checks if two foods are equal (they have exactly the same portions, in any order).
    /// Need to check if foods have identical portions but with different Portions list order too.
    /// </summary>
    /// <param name="other">The day to compare this against.</param>
    public bool IsEqualTo(Day other)
    {
        // For all portions, check there is an identical portion in the other day.
        foreach (Portion portion in Portions)
        {
            bool equivalentPortionFound = false;
            foreach (Portion otherPortion in other.Portions)
            {
                if (portion.IsEqualTo(otherPortion))
                {
                    equivalentPortionFound = true;
                    break;
                }
            }

            if (!equivalentPortionFound) return false;
        }

        return true;
    }


    /// <summary>
    /// Evaluates the fitness of this day.
    /// </summary>
    /// <returns>The fitness as a float.</returns>
    public float GetFitness()
    {
        // Calculate the overall fitness value based on the sum of the fitness of the individual
        // nutrient amounts. (E.g. protein leads to a fitness value, which is multiplied to the fat fitness,
        // etc... over all nutrients).

        float fitness = 0;

        for (int i = 0; i < Nutrients.Count; i++)
        {
            Nutrient nutrient = (Nutrient)i;
            float amount = GetNutrientAmount(nutrient);
            fitness += Algorithm.Instance.Constraints[i]._GetFitness(amount);

            // Quick exit for infinity fitness
            if (fitness == float.PositiveInfinity) return fitness;
        }

        return fitness;
    }


    /// <summary>
    /// Returns a sum over the quantity of the provided nutrient each portion contains.
    /// </summary>
    /// <param name="nutrient">The nutrient to get the amount for.</param>
    /// <returns>The quantity of the nutrient in the whole day.</returns>
    public float GetNutrientAmount(Nutrient nutrient)
    {
        return _nutrientAmounts[(int)nutrient];
    }


    public int GetMass()
    {
        return Portions.Sum(portion => portion.Mass);
    }


    public float GetDistance(Day day)
    {
        // Square root of all differences squared = Pythagorean Distance
        Nutrient[] vals = (Nutrient[])Enum.GetValues(typeof(Nutrient));
        return Mathf.Sqrt(vals.Sum(o => Mathf.Pow(GetNutrientAmount(o) - day.GetNutrientAmount(o), 2)));
    }


    /// <summary>
    /// Gets a verbose description of this day.
    /// </summary>
    /// <returns>The string data.</returns>
    public string Verbose()
    {
        string asString = "";
        for (int i = 0; i < Nutrients.Count; i++)
        {
            Nutrient nutrient = (Nutrient)i;
            float nutrientAmount = GetNutrientAmount(nutrient);
            asString += $"{nutrient}: {nutrientAmount}{Nutrients.GetUnit(nutrient)}\n";
        }
        return asString + $"Mass: {GetMass()}g";
    }
}

//    //public float Fitness(List<Constraint> constraints, int numMeals, int numPortions)
//    //{
//    //    float fitness = 0f;
//    //    foreach (Constraint constraint in constraints)
//    //    {
//    //        // Multiply the parameter by the number of portions & number of meals to calculate average fitness
//    //        fitness += constraint.GetFitness(GetNutrient(constraint.Parameter) * numMeals * numPortions);
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
