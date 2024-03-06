using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;

#if UNITY_64
using UnityEngine;
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


    private readonly Random m_rand = new();
    public Random Rand
    {
        get { return m_rand; }
    }


    // All of the foods that were extracted from the dataset.
    protected ReadOnlyCollection<Food> m_foods;

    // User-defined Constraints; one for each Nutrient.
    public ReadOnlyCollection<Constraint> Constraints;

    /// <summary>
    /// Visible on the GUI.
    /// </summary>

    // The population of Days currently being processed (there may be more hidden, such as with ACO, but these are the
    // ones displayed to the user in the GUI.)
    private List<Day> m_population;             // Underlying
    public ReadOnlyCollection<Day> Population;  // Visible

    public int IterNum { get; private set; } = 0;

    // The current best day. Encapsulated as a new Day to prevent frontend side effects.
    private Day m_bestDay;                      // Underlying
    public Day BestDay
    {
        get
        {
            return m_bestDay == null ? null : new(m_bestDay);
        }
    }

    public int BestIteration { get; private set; } = 0;

    // Stores any errors which occur during the dataset stage, to display to the user instead of running.
    public readonly string DatasetError = "";


    public Algorithm(bool fillPopulation = true)
    {
        Preferences prefs = Preferences.Instance;

        DatasetReader dr = new(prefs);
        DatasetError = dr.SetupError;

        if (DatasetError != "") return;
        m_foods = new(dr.ProcessFoods());

        // Load constraints from disk.
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

        Constraints = new(constraints);

        m_population = new();
        if (fillPopulation)
            GetStartingPopulation();
        Population = new(m_population);
    }


    /// <summary>
    /// Populates the Population data structure with randomly generated Days.
    /// </summary>
    /// <returns>The created population data structure, WITHOUT evaluated fitnesses.</returns>
    protected void GetStartingPopulation()
    {
        for (int i = 0; i < Preferences.Instance.populationSize; i++)
        {
            // Add a number of days to the population (each has random foods)
            Day day = new();
            for (int j = 0; j < Preferences.Instance.numStartingPortionsPerDay; j++)
            {
                // Add random foods to the day
                day.AddPortion(GenerateRandomPortion());
            }

            m_population.Add(day);
        }
    }


    /// <summary>
    /// Generates a random portion (a random food selected from the dataset, with a random
    /// quantity multiplier).
    /// </summary>
    /// <returns></returns>
    protected Portion GenerateRandomPortion()
    {
        Food food = m_foods[m_rand.Next(m_foods.Count)];
        return new(food, m_rand.Next(Preferences.Instance.portionMinStartMass, Preferences.Instance.portionMaxStartMass));
    }


    public virtual void PostConstructorSetup() { }


    /// <summary>
    /// Must evaluate the fitness first, as to begin with the Population data structure hasn't
    /// got evaluated fitnesses.
    /// 
    /// Every RunIteration method call will begin with fitnesses calculated from the previous
    /// iteration, even if there was no previous iteration (e.g. starting iteration 1)!
    /// </summary>
    public void NextIteration()
    {
        IterNum++;
        RunIteration();

        for (int i = 0; i < Population.Count; i++)
        {
            if (m_bestDay == null)
            {
                m_bestDay = Population[i];
                continue;
            }

            if (Population[i].Fitness < m_bestDay.Fitness)
            {
                m_bestDay = Population[i];
                BestIteration = IterNum;
            }
        }
    }


    protected abstract void RunIteration();


    protected virtual void AddToPopulation(Day day)
    {
        m_population.Add(day);
    }


    protected virtual void RemoveFromPopulation(Day day)
    {
        m_population.Remove(day);
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
    /// Resets the static instance.
    /// </summary>
    public static void EndAlgorithm()
    {
        m_instance = null;
    }
}