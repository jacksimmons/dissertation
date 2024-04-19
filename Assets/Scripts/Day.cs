// Commented 18/4
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


/// <summary>
/// A data structure representing a meal plan for one day.
/// </summary>
public partial class Day : IVerbose, IComparable<Day>
{
    /// <summary>
    /// The algorithm whose population this belongs to.
    /// </summary>
    private readonly Algorithm m_algorithm;

    // The portions that compose the day; i.e. the food recommended by the plan.
    private readonly List<Portion> m_portions;
    public readonly ReadOnlyCollection<Portion> portions;

    /// <summary>
    /// For every Food type the Day has, map the one and only Portion of this type.
    /// </summary>
    private Dictionary<Food, Portion> m_foodToPortion;

    /// <summary>
    /// A cache for the total amount of each "nutrient" the Day has. Each value is the sum of all the Portion amounts
    /// for that nutrient. Storing it this way prevents performing many expensive Sum() operations.
    /// </summary>
    private readonly float[] m_nutrientAmounts;

    /// <summary>
    /// The total mass of the Day. Equivalent to Sum() of all the Portions' masses, but caching this value here
    /// means these Sum()s don't need to be performed.
    /// </summary>
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
        m_portions = new();
        portions = new(m_portions);
        m_algorithm = algorithm;
        m_nutrientAmounts = new float[Constraint.Count];
        m_foodToPortion = new();
    }


    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Day(Day day) : this()
    {
        m_portions = new(day.portions);
        portions = new(m_portions);
        m_algorithm = day.m_algorithm;
        Mass = day.Mass;
        m_nutrientAmounts = day.m_nutrientAmounts;
        m_foodToPortion = day.m_foodToPortion;
    }


    // --- Exposed portion list operations


    /// <summary>
    /// Adds a portion to the Day, and updates "total" quantities, so that expensive Sum() operations
    /// don't need to be carried out later.
    /// </summary>
    /// <param name="portion">The portion to add.</param>
    public void AddPortion(Portion portion)
    {
        bool merged = false;

        // Merge new portion with any existing one.
        if (m_foodToPortion.ContainsKey(portion.FoodType))
        {
            Portion existing = m_foodToPortion[portion.FoodType];
            existing.Mass += portion.Mass;
            merged = true;
        }

        // Otherwise, add the portion (it has a unique food)
        if (!merged)
        {
            m_portions.Add(portion);
            m_foodToPortion.Add(portion.FoodType, portion);
        }
        AddPortionProperties(portion);
    }


    /// <summary>
    /// Removes a portion from the list.
    /// </summary>
    /// <param name="index">The portion index to remove.</param>
    public void RemovePortion(int index)
    {
        SubtractPortionProperties(m_portions[index]);
        m_foodToPortion.Remove(m_portions[index].FoodType);
        m_portions.RemoveAt(index);
    }


    /// <summary>
    /// Sets the mass of the portion at index `index` to `mass`.
    /// </summary>
    /// <param name="index">The index of the portion to set the mass of.</param>
    /// <param name="mass">The new mass.</param>
    public void SetPortionMass(int index, int mass)
    {
        // Calculate the change in mass, and add the properties (which can be positive or negative)
        // of this mass change.
        int dmass = mass - m_portions[index].Mass;

        Portion p = m_portions[index];
        p.Mass = mass;
        m_portions[index] = p;

        Portion diff = new(p.FoodType, dmass);
        AddPortionProperties(diff);
        // ----- END -----
    }


    // --- Methods which handle changing the total nutrients data structure for the Day
    // --- based on a portion change.


    /// <summary>
    /// Adds the portion's properties to any corresponding "total" variables. Must be performed when
    /// a new portion is added to ensure validity of the cache.
    /// </summary>
    /// <param name="portion">The portion whose properties must be added.</param>
    private void AddPortionProperties(Portion portion)
    {
        for (int i = 0; i < Constraint.Count; i++)
        {
            float diff = portion.GetConstraintAmount((EConstraintType)i);
            m_nutrientAmounts[i] += diff;

            if (diff > 0)
                TotalFitness.SetNutrientOutdated((EConstraintType)i);
        }
        Mass += portion.Mass;
    }


    /// <summary>
    /// Subtracts the portion's properties to any corresponding "total" variables. Must be performed when
    /// a portion is removed to ensure validity of the cache.
    /// </summary>
    /// <param name="portion">The portion whose properties must be subtracted.</param>
    private void SubtractPortionProperties(Portion portion)
    {
        for (int i = 0; i < Constraint.Count; i++)
        {
            float diff = portion.GetConstraintAmount((EConstraintType)i);
            m_nutrientAmounts[i] -= diff;

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
        return m_nutrientAmounts[(int)nutrient];
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
