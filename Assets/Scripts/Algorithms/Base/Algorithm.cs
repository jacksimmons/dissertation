using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;
using System.Collections.ObjectModel;


public abstract class Algorithm
{
    // Singleton pattern
    private static Algorithm m_instance;
    public static Algorithm Instance
    {
        get
        {
            if (m_instance != null) return m_instance;

            // Assign to the private set variable, and return it.
            // Gets the (one) AlgorithmGlobal script through Unity.

            // NOTE: In editor tests, this will throw an error as the GameObject tree is inactive.
            // Assign the Instance variable ASAP in test classes.
            return m_instance = GameObject.FindWithTag("AlgorithmRunner").GetComponent<AlgorithmRunner>().Algorithm;
        }

        protected set { m_instance = value; }
    }


    // The constraints for each nutrient in a day.
    protected List<Food> m_foods;

    public readonly ReadOnlyCollection<Constraint> Constraints;

    private List<Day> m_population;
    public ReadOnlyCollection<Day> Population;

    private Day m_bestDay;

    public int NumIterations { get; protected set; } = 1;

    public const int NumStartingDaysInPop = 10;
    public const int NumStartingPortionsInDay = 1;
    public const int StartingPortionMassMin = 50;
    public const int StartingPortionMassMax = 150;

    public readonly string DatasetError = "";


    public Algorithm()
    {
        Preferences prefs = Preferences.Instance;

        DatasetReader dr = new(prefs);
        DatasetError = dr.SetupError;

        if (DatasetError != "") return;
        m_foods = dr.ProcessFoods();

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
            float tolerance = Preferences.Instance.tolerances[i];
            float steepness = Preferences.Instance.steepnesses[i];
            switch (Preferences.Instance.constraintTypes[i])
            {
                case ConstraintType.Minimise:
                    constraint = new MinimiseConstraint(goal);
                    break;
                case ConstraintType.Converge:
                    constraint = new ConvergeConstraint(goal, steepness, tolerance);
                    break;
                default:
                    constraint = new NullConstraint();
                    break;
            }

            constraints[i] = constraint;
        }

        Constraints = new(constraints);

        m_population = new();
        GetStartingPopulation();
        Population = new(m_population);
    }


    /// <summary>
    /// Resets the static instance.
    /// </summary>
    public static void EndAlgorithm()
    {
        Instance = null;
    }


    /// <summary>
    /// Populates the Population data structure with randomly generated Days.
    /// </summary>
    /// <returns>The created population data structure, WITHOUT evaluated fitnesses.</returns>
    protected void GetStartingPopulation()
    {
        for (int i = 0; i < NumStartingDaysInPop; i++)
        {
            // Add a number of days to the population (each has random foods)
            Day day = new();
            for (int j = 0; j < NumStartingPortionsInDay; j++)
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
        Food food = m_foods[Random.Range(0, m_foods.Count)];
        return new(food, Random.Range(StartingPortionMassMin, StartingPortionMassMax));
    }


    public abstract void PostConstructorSetup();


    /// <summary>
    /// Must evaluate the fitness first, as to begin with the Population data structure hasn't
    /// got evaluated fitnesses.
    /// 
    /// Every RunIteration method call will begin with fitnesses calculated from the previous
    /// iteration, even if there was no previous iteration (e.g. starting iteration 1)!
    /// </summary>
    public void NextIteration()
    {
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
    /// Copies the best day into a new object, and returns it, if the best day is non-null.
    /// Otherwise, returns null.
    /// </summary>
    public Day GetBestDay()
    {
        return m_bestDay == null ? null : new(m_bestDay);
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
}