using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


public abstract class Algorithm
{
    protected static Random Rand { get; } = new();

    // Shorthand for Preferences.Instance
    protected static Preferences Prefs => Preferences.Instance;

    // All of the foods that were extracted from the dataset (aligning with user diet preferences), minus the foods
    // the user specifically banned.
    private readonly List<Food> m_foods;
    public readonly ReadOnlyCollection<Food> Foods;

    public readonly ReadOnlyCollection<Constraint> Constraints; // User-defined constraints, one for each nutrient.

    public int IterNum { get; private set; } = 0;        // The current iteration of the algorithm.

    private readonly List<Day> m_population;
    public readonly ReadOnlyCollection<Day> Population;

    // A huge data structure storing all possible portions from the dataset in non-random order.
    // Preferences.Instance.minPortionMass and Preferences.Instance.maxPortionMass can drastically change the
    // size of this in memory. (By default, it is about 3M elements, or at least 3MB)
    protected Portion[] m_portions = Array.Empty<Portion>();
    protected Portion RandomPortion => m_portions[Rand.Next(m_portions.Length)];

    private readonly Dictionary<ENutrient, float> m_prevAvgPopStats = new(); // Stores the average nutrient amount for the whole population from last iteration.


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

    public readonly string DatasetError = ""; // Stores any errors which occur during the dataset stage, to display to the user instead of running.


    // https://stackoverflow.com/questions/12306/can-i-serialize-a-c-sharp-type-object
    /// <summary>
    /// Instantiate an Algorithm subclass instance.
    /// </summary>
    /// <param name="algType">A type which is a subclass of Algorithm.</param>
    /// <returns></returns>
    public static Algorithm Build(Type algType)
    {
        if (!algType.IsSubclassOf(typeof(Algorithm)))
            Logger.Error($"Invalid Algorithm type: {algType.FullName!}.");

        return (Algorithm)Activator.CreateInstance(algType)!;
    }


    protected Algorithm()
    {
        // Load foods from the dataset, and store any errors that occurred.
        DatasetReader dr = new(Prefs);
        DatasetError = dr.SetupError;

        if (DatasetError != "") // Thrown in Init
            return;

        m_foods = new(dr.ProcessFoods());
        Foods = new(m_foods);

        // Remove any banned foods from the extracted list
        List<Food> allFoods = new(m_foods);
        foreach (Food food in allFoods)
        {
            // If the FoodData of `food` from the Dataset is in the banned list...
            if (Prefs.IsFoodBanned(food.CompositeKey))
            {
                m_foods.Remove(food);
            }
        }
        // Update the foods list with the unbanned list.
        m_foods = allFoods;


        // Generate the search space from these foods
        // MAX - MIN + 1 is the number of elements MIN <= x <= MAX. (E.g. 2 <= x <= 3 => x in {2, 3} (count 2) and 3 - 2 + 1 = 2)
        int portionsPerFood = Prefs.maxPortionMass - Prefs.minPortionMass + 1;
        m_portions = new Portion[Foods.Count * portionsPerFood];
        for (int i = 0; i < Foods.Count; i++)
        {
            for (int j = 0; j < portionsPerFood; j++)
            {
                // The mass of the portion can be calculated by just adding the minimum mass to j.
                m_portions[i * portionsPerFood + j] = new(Foods[i], j + Prefs.minPortionMass);
            }
        }


        // Load constraints from Preferences.
        Constraints = new(Prefs.constraints.Select(Constraint.Build).ToList());


        // Create the population data structure
        m_population = new();
        Population = new(m_population);
    }


    /// <summary>
    /// Handles any initialisation that cannot be done in the constructor.
    /// Returns true if successful, false if there was an error.
    /// </summary>
    public virtual bool Init()
    {
        // Handle potential errors.
        string errorText = "";

        if (DatasetError != "")
            errorText = DatasetError;

        if (Preferences.Instance.acoAlpha <= 0)
            errorText = "Invalid parameter: acoAlpha was <= 0.";

        if (Preferences.Instance.acoBeta <= 0)
            errorText = "Invalid parameter: acoBeta was <= 0.";

        if (Preferences.Instance.minPortionMass <= 0)
            errorText = "Invalid parameter: minPortionMass was <= 0.";

        if (Preferences.Instance.maxPortionMass < Preferences.Instance.minPortionMass)
            errorText = "Invalid parameter: maxPortionMass was < minPortionMass.";

        if (Preferences.Instance.numStartingPortionsPerDay <= 0)
            errorText = "Invalid parameter: numStartingPortionsPerDay was <= 0.";

        if (Preferences.Instance.pheroEvapRate < 0)
            errorText = "Invalid parameter: pheroEvapRate was < 0.";

        if (Preferences.Instance.pheroImportance < 0)
            errorText = "Invalid parameter: pheroImportance was < 0.";

        if (Preferences.Instance.populationSize <= 0)
            errorText = "Invalid parameter: populationSize was <= 0.";

        // By default, don't show error
        if (errorText != "")
        {
            Logger.Warn(errorText);
            return false;
        }

        return true;
    }


    /// <summary>
    /// The exposed entry point for the next iteration in the algorithm.
    /// </summary>
    public void RunIteration()
    {
        IterNum++;
        NextIteration();
        UpdateBestDay(); // Ensures best day is updated for all iterations, even 0
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
        return $"Fitness: {day.FitnessVerbose()}";
    }


    /// <summary>
    /// Generates and returns a string with the average stats of the program (e.g. average population stats
    /// or average ant stats, etc.)
    /// </summary>
    public virtual string GetAverageStatsLabel()
    {
        string avgStr = "";
        foreach (ENutrient nutrient in Nutrient.Values)
        {
            float sum = 0;
            foreach (Day day in m_population)
            {
                sum += day.GetNutrientAmount(nutrient);
            }

            float avg = sum / Nutrient.Count;
            if (!m_prevAvgPopStats.ContainsKey(nutrient)) m_prevAvgPopStats[nutrient] = avg;

            avgStr += $"{nutrient}: {avg:F2}(+{avg - m_prevAvgPopStats[nutrient]:F2}){Nutrient.GetUnit(nutrient)}\n";

            m_prevAvgPopStats[nutrient] = avg;
        }
        return avgStr;
    }
}