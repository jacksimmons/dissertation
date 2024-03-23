using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using System;

/// <summary>
/// A data structure representing a meal plan for one day.
/// </summary>
public class Day : IVerbose
{
    private readonly List<Portion> _portions;
    public readonly ReadOnlyCollection<Portion> portions;
    private readonly Algorithm m_algorithm;

    // Only change this array when one of the portions changes, eliminating the
    // need to perform expensive Linq Sum operations every iteration.
    private readonly float[] m_nutrientAmounts = new float[Nutrient.Count];

    public int Mass { get; private set; } = 0;


    public Day(Algorithm algorithm)
    {
        _portions = new();
        portions = new(_portions);
        m_algorithm = algorithm;
    }


    public Day(Day day)
    {
        _portions = new(day.portions);
        portions = new(_portions);
        m_algorithm = day.m_algorithm;
        Mass = day.Mass;
        m_nutrientAmounts = day.m_nutrientAmounts;
    }


    // --- Permitted portion list operations


    public void AddPortion(Portion portion)
    {
        bool merged = false;

        // Merge new portion with an existing one if it has the same name.
        for (int i = 0; i < portions.Count; i++)
        {
            Portion existing = portions[i];
            if (existing.FoodType == portion.FoodType)
            {
                existing.Mass += portion.Mass;
                merged = true;
                _portions[i] = existing;
                break;
            }
        }

        // Otherwise, add the portion (it has a unique food)
        if (!merged)
            _portions.Add(portion);
        AddPortionProperties(portion);
        m_algorithm.OnDayUpdated(this);
    }


    /// <summary>
    /// Tries to remove the portion from the list. If it is the last remaining portion,
    /// it will not be removed.
    /// </summary>
    /// <param name="index">The portion index to remove if it is not the only one left.</param>
    public void RemovePortion(int index)
    {
        if (_portions.Count <= 1)
            return;

        SubtractPortionProperties(_portions[index]);
        _portions.RemoveAt(index);
        m_algorithm.OnDayUpdated(this);
    }


    public void SetPortionMass(int index, int mass)
    {
        int dmass = mass - _portions[index].Mass;

        Portion p = _portions[index];
        p.Mass = mass;
        _portions[index] = p;

        Portion diff = new(p.FoodType, dmass);
        AddPortionProperties(diff);
        m_algorithm.OnDayUpdated(this);
    }


    // --- Methods which handle changing the total nutrients data structure for the Day
    // --- based on a portion change.


    private void AddPortionProperties(Portion portion)
    {
        for (int i = 0; i < Nutrient.Count; i++)
        {
            m_nutrientAmounts[i] += portion.GetNutrientAmount((ENutrient)i);
        }
        Mass += portion.Mass;
    }


    private void SubtractPortionProperties(Portion portion)
    {
        for (int i = 0; i < Nutrient.Count; i++)
        {
            m_nutrientAmounts[i] -= portion.GetNutrientAmount((ENutrient)i);
        }
        Mass -= portion.Mass;
    }


    // --- Calculation methods


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

        for (int i = 0; i < Nutrient.Count; i++)
        {
            float amount = m_nutrientAmounts[i];
            fitness += m_algorithm.Constraints[i].GetFitness(amount);

            // Quick exit for infinity fitness
            if (fitness == float.PositiveInfinity)
            {
                //Logger.Log($"{Nutrient.Values[i]} : {Preferences.Instance.constraints[i].Type} {((HardConstraint)m_algorithm.Constraints[i]).min} {amount} {((HardConstraint)m_algorithm.Constraints[i]).max} gave finf");
                return fitness;
            }
        }

        if (Preferences.Instance.addFitnessForMass)
        {
            // Penalise portions with mass over the maximum (Food mass constraint)
            foreach (Portion p in portions)
            {
                fitness += MathF.Max(p.Mass - Preferences.Instance.maxPortionMass, 0);
            }
        }

        return fitness;
    }


    /// <summary>
    /// Returns a sum over the quantity of the provided nutrient each portion contains.
    /// </summary>
    /// <param name="nutrient">The nutrient to get the amount for.</param>
    /// <returns>The quantity of the nutrient in the whole day.</returns>
    public float GetNutrientAmount(ENutrient nutrient)
    {
        return m_nutrientAmounts[(int)nutrient];
    }


    public float GetManhattanDistanceToPerfect()
    {
        float manhattan = 0;
        for (int i = 0; i < Nutrient.Count; i++)
        {
            float amount = m_nutrientAmounts[i];
            manhattan += MathF.Abs(amount - m_algorithm.Constraints[i].BestValue);
        }
        return manhattan;
    }


    public float GetDistance(Day day)
    {
        // Square root of all differences squared = Pythagorean Distance
        ENutrient[] vals = (ENutrient[])Enum.GetValues(typeof(ENutrient));
        return MathF.Sqrt(vals.Sum(o => MathF.Pow(GetNutrientAmount(o) - day.GetNutrientAmount(o), 2)));
    }


    /// <summary>
    /// Checks if two foods are equal (they have exactly the same portions, in any order).
    /// Need to check if foods have identical portions but with different portions list order too.
    /// </summary>
    /// <param name="other">The object to compare this against.</param>
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
    /// Compares two days, and returns a detailed enum value on the dominance hierarchy between
    /// the two.
    /// </summary>
    public ParetoComparison CompareTo(Day other)
    {
        return CompareTo(other, m_algorithm.Constraints);
    }


    public ParetoComparison CompareTo(Day other, ReadOnlyCollection<Constraint> constraints)
    {
        // Store how many constraints this is better/worse than `day` on.
        int betterCount = 0;
        int worseCount = 0;

        for (int i = 0; i < Nutrient.Count; i++)
        {
            float fitnessA = constraints[i].GetFitness(GetNutrientAmount((ENutrient)i));
            float fitnessB = constraints[i].GetFitness(other.GetNutrientAmount((ENutrient)i));

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


    // --- Other methods


    /// <summary>
    /// Gets a verbose description of this day.
    /// </summary>
    /// <returns>The string data.</returns>
    public string Verbose()
    {
        string asString = "";
        for (int i = 0; i < Nutrient.Count; i++)
        {
            ENutrient nutrient = (ENutrient)i;
            float nutrientAmount = GetNutrientAmount(nutrient);
            asString += $"{nutrient}: {nutrientAmount}{Nutrient.GetUnit(nutrient)}\n";
        }
        return asString + $"Mass: {Mass}g";
    }
}
