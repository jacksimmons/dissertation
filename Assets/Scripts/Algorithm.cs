// Commented 8/4
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static UnityEditor.Progress;


public abstract class Algorithm
{
    // Shorthand for Preferences.Instance
    protected static Preferences Prefs => Preferences.Instance;

    // Instance-variable Random Instance, to ensure thread-safety.
    protected Random Rand = new();

    // All of the foods that were extracted from the dataset (aligning with user diet preferences), minus the foods
    // the user specifically banned.
    public readonly ReadOnlyCollection<Food> Foods;

    public readonly ReadOnlyCollection<Constraint> Constraints; // User-defined constraints, one for each constraint type.

    public int IterNum { get; private set; } = 0;        // The current iteration of the algorithm.

    private readonly List<Day> m_population;
    public readonly ReadOnlyCollection<Day> Population;

    protected Portion RandomPortion
    {
        get
        {
            Food food = Foods[Rand.Next(0, Foods.Count)];
            int mass = Rand.Next(Prefs.minPortionMass, Prefs.maxPortionMass);
            return new(food, mass);
        }
    }
    private readonly Dictionary<EConstraintType, float> m_prevAvgPopStats = new(); // Stores the average constraint amount for the whole population from last iteration.


    // --- Best day properties, can only be set together ---

    // The current best day. Cloned as a new Day to prevent side effects.
    // (OR returned as null if no best day exists)
    private Day m_bestDay;                      // Underlying
    public Day BestDay                          // Cloned
    {
        get => BestDayExists ? new(m_bestDay) : null;
    }
    public bool BestDayExists
    {
        get => m_bestDay != null;
    }

    /// <summary>
    /// A representation of the best day's fitness.
    /// </summary>
    public float BestFitness { get; private set; } = float.PositiveInfinity;

    /// <summary>
    /// The iteration number of the best day.
    /// </summary>
    public int BestIteration { get; private set; } = 0;


    // https://stackoverflow.com/questions/12306/can-i-serialize-a-c-sharp-type-object
    /// <summary>
    /// Instantiate an Algorithm subclass instance.
    /// </summary>
    /// <param name="algType">A type which is a subclass of Algorithm.</param>
    /// <returns></returns>
    public static Algorithm Build(Type algType)
    {
        if (!algType.IsSubclassOf(typeof(Algorithm)))
            Logger.Warn($"Invalid Algorithm type: {algType.FullName!}.");

        return (Algorithm)Activator.CreateInstance(algType)!;
    }


    protected Algorithm()
    {
        // Load foods from the dataset
        Foods = new(DatasetReader.Instance.Output);


        // Load constraints from Preferences.
        Constraints = new(Prefs.constraints.Select(Constraint.Build).ToList());


        // Create the population data structure
        m_population = new();
        Population = new(m_population);
    }


    /// <summary>
    /// Handles any initialisation that cannot be done in the constructor.
    /// Returns a string - this is empty if no errors, but is an error message otherwise.
    /// </summary>
    public virtual string Init()
    {
        // Handle potential errors.
        string errorText = "";

        if (Preferences.Instance.minPortionMass <= 0)
            errorText = $"Invalid parameter: minPortionMass ({Preferences.Instance.minPortionMass}) must be greater than 0.";

        if (Preferences.Instance.maxPortionMass < Preferences.Instance.minPortionMass)
            errorText = $"Invalid parameters: maxPortionMass ({Preferences.Instance.maxPortionMass}) cannot be less than minPortionMass ({Preferences.Instance.minPortionMass})";

        if (Preferences.Instance.numStartingPortionsPerDay <= 0)
            errorText = $"Invalid parameter: numStartingPortionsPerDay ({Preferences.Instance.numStartingPortionsPerDay}) must be greater than 0.";

        if (Preferences.Instance.populationSize <= 0)
            errorText = $"Invalid parameter: populationSize ({Preferences.Instance.populationSize}) must be greater than 0.";

        return errorText;
    }


    /// <summary>
    /// The exposed entry point for the next iteration in the algorithm.
    /// </summary>
    public void RunIteration()
    {
        IterNum++;
        NextIteration();
        UpdateBestDay();
    }


    /// <summary>
    /// The internal iteration handling method, implemented in derived classes.
    /// </summary>
    protected abstract void NextIteration();


    /// <summary>
    /// Overridable method for subclasses to add a day to the population.
    /// </summary>
    protected virtual void AddToPopulation(Day day)
    {
        m_population.Add(day);
    }


    /// <summary>
    /// Overridable method for subclasses to remove a day to the population.
    /// </summary>
    protected virtual void RemoveFromPopulation(Day day)
    {
        m_population.Remove(day);
    }


    /// <summary>
    /// Accessible method for subclasses to clear the entire population.
    /// </summary>
    protected void ClearPopulation()
    {
        m_population.Clear();
    }


    /// <summary>
    /// Updates the BestDay attrib if a new best day exists in the population.
    /// </summary>
    private void UpdateBestDay()
    {
        foreach (Day day in m_population)
        {
            // Replace the best day if A) It is null B) This day has a lower fitness
            if (!BestDayExists || day < BestDay)
            {
                SetBestDay(day, IterNum);
            }
        }

        // Prevent other classes modifying the just-added best day by cloning it
        SetBestDay(new(BestDay), BestIteration);
    }


    /// <summary>
    /// The only way to "set" the best day.
    /// These parameters are all private so inherited classes MUST set them
    /// together with this method (reducing bugs).
    /// </summary>
    protected void SetBestDay(Day day, int iteration)
    {
        m_bestDay = day;
        BestFitness = day.TotalFitness.Value;
        BestIteration = iteration;
    }


    /// <summary>
    /// Returns a label string describing the properties of the given day.
    /// </summary>
    public string GetDayLabel(Day day)
    {
        string label = $"Portions: {day.portions.Count} ";
        label += EvaluateDay(day);
        return label;
    }


    /// <summary>
    /// Evaluate the provided solution and return a string corresponding to its fitness, or
    /// another way of describing as a string how good a solution is.
    /// </summary>
    public virtual string EvaluateDay(Day day)
    {
        return $"Fitness: {day.TotalFitness.Verbose()}";
    }


    /// <summary>
    /// Generates and returns a string with the average stats of the program (e.g. average population stats
    /// or average ant stats, etc.)
    /// </summary>
    public virtual string GetAverageStatsLabel()
    {
        string avgStr = "";
        foreach (EConstraintType nutrient in Constraint.Values)
        {
            // Calculate population's total amount of this nutrient
            float sum = 0;
            foreach (Day day in m_population)
            {
                sum += day.GetConstraintAmount(nutrient);
            }

            // Calculate average from sum
            float avg = sum / Constraint.Count;

            // Add prevAvgPopStats key if necessary
            if (!m_prevAvgPopStats.ContainsKey(nutrient)) m_prevAvgPopStats.Add(nutrient, avg);

            // Append to the average stats string
            avgStr += $"{nutrient}: {avg:F2}(+{avg - m_prevAvgPopStats[nutrient]:F2}){Constraint.GetUnit(nutrient)}\n";

            // Update the prevAvgPopStats value
            m_prevAvgPopStats[nutrient] = avg;
        }
        return avgStr;
    }
}