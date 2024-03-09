using System;
using System.Collections.ObjectModel;

#if UNITY_64
using Random = System.Random;
#endif


public abstract class Algorithm
{
    // Singleton pattern
    private static Algorithm m_instance;
    public static Algorithm Instance
    {
        get
        {
            if (m_instance != null) return m_instance;


            // Assign the Instance variable ASAP so this doesn't occur.
            throw new InvalidOperationException("No Algorithm instance exists.");
        }


        // AlgorithmRunner(Core) is the first / only to set this.
        set
        {
            if (m_instance == null) m_instance = value;
            else throw new InvalidOperationException("Algorithm instance was already set.");
        }
    }


    // Random number generator
    public static Random Rand { get; } = new();


    // All of the foods that were extracted from the dataset.
    public ReadOnlyCollection<Food> Foods;


    // User-defined Constraints; one for each Nutrient.
    public ReadOnlyCollection<Constraint> Constraints;


    // The current iteration of the algorithm.
    public int IterNum { get; private set; } = 0;


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


    // The fitness of the best day.
    public float BestFitness { get; private set; } = float.PositiveInfinity;
    public abstract float AverageFitness { get; }

    
    // The iteration number of the best day.
    public int BestIteration { get; private set; } = 0;


    // Stores any errors which occur during the dataset stage, to display to the user instead of running.
    public readonly string DatasetError = "";


    // https://stackoverflow.com/questions/12306/can-i-serialize-a-c-sharp-type-object
    public static Algorithm Build(Type algType)
    {
        return (Algorithm)Activator.CreateInstance(algType)!;
    }


    /// <summary>
    /// Resets the static instance.
    /// </summary>
    public static void EndAlgorithm()
    {
        m_instance = null;
    }


    protected Algorithm()
    {
        Preferences prefs = Preferences.Instance;


        // Load foods from the dataset, and store any errors that occurred.
        DatasetReader dr = new(prefs);
        DatasetError = dr.SetupError;
        if (DatasetError != "") return;
        Foods = new(dr.ProcessFoods());


        // Load constraints from Preferences.
        Constraints = new(GetConstraintsFromPrefs());
    }


    private Constraint[] GetConstraintsFromPrefs()
    {
        Constraint[] constraints = new Constraint[Nutrients.Count];
        for (int i = 0; i < Nutrients.Count; i++)
        {
            // If the constraintTypes arraylength is insufficient, the nutrient has no constraintType setting.
            if (Preferences.Instance.constraintTypes.Length <= i)
            {
                constraints[i] = new NullConstraint();
                continue;
            }

            // Otherwise create the required constraint
            Constraint constraint;
            float goal = Preferences.Instance.goals[i];
            switch (Preferences.Instance.constraintTypes[i])
            {
                case ConstraintType.Minimise:
                    constraint = new MinimiseConstraint(goal);
                    break;
                case ConstraintType.Converge:
                    constraint = new ConvergeConstraint(goal, Preferences.Instance.steepnesses[i], Preferences.Instance.tolerances[i]);
                    break;
                default:
                    constraint = new NullConstraint();
                    break;
            }

            constraints[i] = constraint;
        }

        return constraints;
    }


    /// <summary>
    /// Handles any initialisation that cannot be done in the constructor.
    /// </summary>
    public abstract void Init();


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
    /// Updates the BestDay attrib if a new best day exists in the population.
    /// </summary>
    protected abstract void UpdateBestDay();


    /// <summary>
    /// The only way to "set" the best day, its fitness and iteration.
    /// These parameters are all private so inherited classes MUST set them all
    /// together with this method (reducing bugs).
    /// </summary>
    protected void SetBestDay(Day day, float fitness, int iteration)
    {
        m_bestDay = day;
        BestFitness = fitness;
        BestIteration = iteration;
    }


    /// <summary>
    /// Use this to get the number of non-null constraints defined in the Constraints
    /// dictionary.
    /// </summary>
    public int GetNumConstraints()
    {
        int cnt = 0;
        for (int i = 0; i < Nutrients.Count; i++)
        {
            if (Constraints[i].GetType() != typeof(NullConstraint)) cnt++;
        }
        return cnt;
    }


    /// <summary>
    /// Evaluate the provided solution and return a string corresponding to its fitness, or
    /// another way of describing as a string how good a solution is.
    /// </summary>
    public abstract string EvaluateDay(Day day);


    /// <summary>
    /// Generates and returns a string with the average stats of the program (e.g. average population stats
    /// or average ant stats, etc.)
    /// </summary>
    public abstract string GetAverageStatsLabel();


    /// <summary>
    /// Abstract methods which are called during specific Day events. These vary with the choice of algorithm.
    /// Used to update fitness when it changes, reducing the amount of total GetFitness calls.
    /// </summary>
    public abstract void Day_OnPortionAdded(Day day, Portion portion);
    public abstract void Day_OnPortionRemoved(Day day, Portion portion);
    public abstract void Day_OnPortionMassModified(Day day, Portion portion, int dmass);
}