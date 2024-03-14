using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// A class which represents the properties of a 100g portion of
/// a specific food from the dataset.
/// </summary>
public class Food
{
    public readonly string name;
    public readonly string description;
    public readonly string foodGroup;
    public readonly string reference;

    public readonly float[] nutrients;


    public Food(string name, string desc, string foodGroup, string reference, float[] nutrients)
    {
        this.name = name;
        this.description = desc;
        this.foodGroup = foodGroup;
        this.reference = reference;
        this.nutrients = nutrients;
    }


    public Food(FoodData data)
    {
        name = data.name;
        description = data.description;
        foodGroup = data.foodGroup;
        reference = data.reference;
        nutrients = data.nutrients;
    }


    public bool IsEqualTo(Food other)
    {
        // Check all simple attribs are the same
        bool attribs = name == other.name && description == other.description && foodGroup == other.foodGroup && reference == other.reference;

        if (!attribs) return false;

        // Check all nutrients are the same
        for (int i = 0; i < Nutrients.Count; i++)
        {
            if (nutrients[i] != other.nutrients[i])
                return false;
        }
        return true;
    }
}


/// <summary>
/// A struct with a type of food (100g), multiplied by a quantity.
/// </summary>
public struct Portion
{
    public readonly Food food;

    public int Mass
    {
        readonly get { return (int)MathF.Round(100 * Multiplier); }
        set
        {
            if (value < 0)
                Logger.Log($"Mass was negative: {value}", Severity.Error);
            Multiplier = (float)value / 100;
        }
    }
    public float Multiplier { get; set; }


    public Portion(Food food)
    {
        this.food = food;
        Multiplier = 1;
    }


    public Portion(Food food, int mass)
    {
        this.food = food;
        Multiplier = (float)mass/100;
    }


    public readonly float GetNutrientAmount(Nutrient nutrient)
    {
        return food.nutrients[(int)nutrient] * Multiplier;
    }


    public readonly bool IsEqualTo(Portion other)
    {
        return food.IsEqualTo(other.food) && Multiplier == other.Multiplier;
    }


    /// <summary>
    /// Gets a verbose description of this portion.
    /// </summary>
    /// <returns>The string data.</returns>
    public readonly string Verbose()
    {
        string asString = $"name: {food.name}\n";
        for (int i = 0; i < Nutrients.Count; i++)
        {
            Nutrient nutrient = (Nutrient)i;
            float nutrientAmount = food.nutrients[i];
            asString += $"{nutrient}: {nutrientAmount * Multiplier}{Nutrients.GetUnit(nutrient)}\n";
        }
        return asString + $"Mass: {Mass}g";
    }
}


/// <summary>
/// A class for all the food eaten in a day.
/// </summary>
public class Day
{
    private readonly List<Portion> _portions;
    public readonly ReadOnlyCollection<Portion> portions;

    // Only change this array when one of the portions changes, eliminating the
    // need to perform expensive Linq Sum operations every iteration.
    private readonly float[] m_nutrientAmounts = new float[Nutrients.Count];

    public int Mass { get; private set; } = 0;


    public Day()
    {
        _portions = new();
        portions = new(_portions);
    }


    public Day(Day day)
    {
        _portions = new(day.portions);
        portions = new(_portions);
        Mass = day.Mass;
        m_nutrientAmounts = day.m_nutrientAmounts;
    }


    /// <summary>
    /// Compares two days, and returns a detailed enum value on the dominance hierarchy between
    /// the two.
    /// </summary>
    public static ParetoComparison Compare(Day a, Day b)
    {
        return Compare(a, b, Algorithm.Instance.Constraints);
    }


    public static ParetoComparison Compare(Day a, Day b, ReadOnlyCollection<Constraint> constraints)
    {
        // Store how many constraints this is better/worse than `day` on.
        int betterCount = 0;
        int worseCount = 0;

        for (int i = 0; i < Nutrients.Count; i++)
        {
            float fitnessA = constraints[i].GetFitness(a.GetNutrientAmount((Nutrient)i));
            float fitnessB = constraints[i].GetFitness(b.GetNutrientAmount((Nutrient)i));

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
            if (worseCount == constraints.Count)
                return ParetoComparison.StrictlyDominated;

            // Worse on 1+, and not better on any => Dominated
            return ParetoComparison.Dominated;
        }
        else
        {
            // Better on all => Strictly Dominates
            if (betterCount == constraints.Count)
                return ParetoComparison.StrictlyDominates;

            // Not worse on any, and better on 1+ => Dominates
            if (betterCount > 0)
                return ParetoComparison.Dominates;

            // Not worse on any, and not better on any => MND (They are equal)
            return ParetoComparison.MutuallyNonDominating;
        }
    }


    public void AddPortion(Portion portion)
    {
        AddPortionProperties(portion);

        bool merged = false;

        // Merge new portion with an existing one if it has the same name.
        for (int i = 0; i < portions.Count; i++)
        {
            Portion existing = portions[i];
            if (existing.food.name == portion.food.name)
            {
                existing.Mass += portion.Mass;
                merged = true;
                _portions[i] = existing;
            }
        }

        // Otherwise, add the portion (it has a unique food)
        if (!merged)
            _portions.Add(portion);
        Algorithm.Instance.OnDayUpdated(this);
    }


    /// <summary>
    /// Tries to remove the portion from the list. If it is the last remaining portion,
    /// it will not be removed.
    /// </summary>
    /// <param name="portion">The portion to remove if it is not the only one left.</param>
    public void RemovePortion(int index)
    {
        if (_portions.Count <= 1)
            return;

        SubtractPortionProperties(_portions[index]);

        _portions.RemoveAt(index);
        Algorithm.Instance.OnDayUpdated(this);
    }


    /// <summary>
    /// Replaces portion at given index with a new one.
    /// The only way to publicly change the last portion in the day.
    /// </summary>
    /// <param name="portion">The portion to overwrite with.</param>
    /// <param name="index">The index to overwrite at.</param>
    public void ReplacePortion(Portion portion, int index)
    {
        SubtractPortionProperties(_portions[index]);
        AddPortionProperties(portion);

        _portions[index] = portion;
        Algorithm.Instance.OnDayUpdated(this);
    }


    public void AddToPortionMass(int index, int dmass)
    {
        // Add mass to the portion
        Portion p = _portions[index];
        p.Mass += dmass;
        _portions[index] = p;

        // Create dummy portion with mass = dmass
        Portion dummy = new(p.food, dmass);
        AddPortionProperties(dummy);
        Algorithm.Instance.OnDayUpdated(this);
    }


    public void SetPortionMass(int index, int mass)
    {
        int dmass = mass - _portions[index].Mass;
        AddToPortionMass(index, dmass);
    }


    private void AddPortionProperties(Portion portion)
    {
        for (int i = 0; i < Nutrients.Count; i++)
        {
            m_nutrientAmounts[i] += portion.GetNutrientAmount((Nutrient)i);
        }
        Mass += portion.Mass;
    }


    private void SubtractPortionProperties(Portion portion)
    {
        for (int i = 0; i < Nutrients.Count; i++)
        {
            m_nutrientAmounts[i] -= portion.GetNutrientAmount((Nutrient)i);
        }
        Mass -= portion.Mass;
    }


    /// <summary>
    /// Checks if two foods are equal (they have exactly the same portions, in any order).
    /// Need to check if foods have identical portions but with different portions list order too.
    /// </summary>
    /// <param name="other">The day to compare this against.</param>
    public bool IsEqualTo(Day other)
    {
        // For all portions, check there is an identical portion in the other day.
        foreach (Portion portion in portions)
        {
            bool equivalentPortionFound = false;
            foreach (Portion otherPortion in other.portions)
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
    /// <param name="targetPortionNum">
    /// When this is set, this func extrapolates the fitness of the Day, evaluating how fit the Day would be
    /// if the Day's nutrient amounts were multiplied by (targetPortionNum/portions.Count).
    /// </param>
    /// <returns>The fitness as a float.</returns>
    public float GetFitness(int targetPortionNum = -1)
    {
        // Calculate the overall fitness value based on the sum of the fitness of the individual
        // nutrient amounts. (E.g. protein leads to a fitness value, which is multiplied to the fat fitness,
        // etc... over all nutrients).

        float fitness = 0;

        // Extrapolation multiplier, by default 1 for no extrapolation.
        float mult = targetPortionNum > 0 ? ((float)targetPortionNum / portions.Count) : 1;

        for (int i = 0; i < Nutrients.Count; i++)
        {
            float amount = m_nutrientAmounts[i];
            fitness += mult * Algorithm.Instance.Constraints[i].GetFitness(amount);

            // Quick exit for infinity fitness
            if (fitness == float.PositiveInfinity) return fitness;
        }

        if (Preferences.Instance.addFitnessForMass)
        {
            // Penalise Days with lots of mass
            fitness += mult * Mass;
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
        return m_nutrientAmounts[(int)nutrient];
    }


    public float GetDistance(Day day)
    {
        // Square root of all differences squared = Pythagorean Distance
        Nutrient[] vals = (Nutrient[])Enum.GetValues(typeof(Nutrient));
        return MathF.Sqrt(vals.Sum(o => MathF.Pow(GetNutrientAmount(o) - day.GetNutrientAmount(o), 2)));
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
        return asString + $"Mass: {Mass}g";
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
//        foreach (Portion p in portions)
//        {
//            mealFitness += p.Fitness(constraints, numMeals, portions.Count);
//        }

//        // Inconvenience penalty based on number of portions
//        mealFitness -= MathF.Pow(portions.Count, 2);

//        LastFitness = mealFitness;
//        return mealFitness;
//    }


//    public void Print()
//    {
//        foreach (Portion portion in portions)
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
