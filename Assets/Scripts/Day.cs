using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// A data structure representing a meal plan for one day.
/// </summary>
public partial class Day : IVerbose, IComparable<Day>
{
    private readonly List<Portion> _portions;
    public readonly ReadOnlyCollection<Portion> portions;
    private readonly Algorithm m_algorithm;

    // Only change this array when one of the portions changes, eliminating the
    // need to perform expensive Sum operations every iteration.
    private readonly float[] m_constraintAmounts;

    public int Mass { get; private set; } = 0;
    public Fitness TotalFitness { get; }


    /// <summary>
    /// Internal constructor for code reuse.
    /// </summary>
    private Day()
    {
        // Make a ParetoFitness if PD is selected AND AlgGA is selected, otherwise make a SummedFitness
        if (Preferences.Instance.fitnessApproach == EFitnessApproach.ParetoDominance && Preferences.Instance.algorithmType == typeof(AlgGA).FullName)
            TotalFitness = new ParetoFitness(this);
        else
            TotalFitness = new SummedFitness(this);
    }


    /// <summary>
    /// Default constructor.
    /// </summary>
    public Day(Algorithm algorithm) : this()
    {
        _portions = new();
        portions = new(_portions);
        m_algorithm = algorithm;
        m_constraintAmounts = new float[Constraint.Count];
    }


    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Day(Day day) : this()
    {
        _portions = new(day.portions);
        portions = new(_portions);
        m_algorithm = day.m_algorithm;
        Mass = day.Mass;
        m_constraintAmounts = day.m_constraintAmounts;
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
    }


    public void SetPortionMass(int index, int mass)
    {
        int dmass = mass - _portions[index].Mass;

        Portion p = _portions[index];
        p.Mass = mass;
        _portions[index] = p;

        Portion diff = new(p.FoodType, dmass);
        AddPortionProperties(diff);
    }


    // --- Methods which handle changing the total nutrients data structure for the Day
    // --- based on a portion change.


    private void AddPortionProperties(Portion portion)
    {
        for (int i = 0; i < Constraint.Count; i++)
        {
            float diff = portion.GetConstraintAmount((EConstraintType)i);
            m_constraintAmounts[i] += diff;

            if (diff > 0)
                TotalFitness.SetNutrientOutdated((EConstraintType)i);
        }
        Mass += portion.Mass;
    }


    private void SubtractPortionProperties(Portion portion)
    {
        for (int i = 0; i < Constraint.Count; i++)
        {
            float diff = portion.GetConstraintAmount((EConstraintType)i);
            m_constraintAmounts[i] -= diff;

            if (diff > 0)
                TotalFitness.SetNutrientOutdated((EConstraintType)i);
        }
        Mass -= portion.Mass;
    }


    /// <summary>
    /// Returns a sum over the quantity of the provided nutrient each portion contains.
    /// </summary>
    /// <param name="nutrient">The nutrient to get the amount for.</param>
    /// <returns>The quantity of the nutrient in the whole day.</returns>
    public float GetConstraintAmount(EConstraintType nutrient)
    {
        return m_constraintAmounts[(int)nutrient];
    }


    public float GetManhattanDistanceToPerfect()
    {
        float manhattan = 0;
        for (int i = 0; i < Constraint.Count; i++)
        {
            float amount = m_constraintAmounts[i];
            manhattan += MathF.Abs(amount - m_algorithm.Constraints[i].BestValue);
        }
        return manhattan;
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


    public string FitnessVerbose() => TotalFitness.Verbose();


    // --- Other methods


    /// <summary>
    /// Gets a verbose description of this day.
    /// </summary>
    /// <returns>The string data.</returns>
    public string Verbose()
    {
        string asString = "";
        for (int i = 0; i < Constraint.Count; i++)
        {
            EConstraintType nutrient = (EConstraintType)i;
            float constraintAmount = GetConstraintAmount(nutrient);
            asString += $"{nutrient}: {constraintAmount}{Constraint.GetUnit(nutrient)}\n";
        }
        return asString + $"Mass: {Mass}g";
    }
}
