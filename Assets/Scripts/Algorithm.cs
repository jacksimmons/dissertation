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

    public readonly ReadOnlyDictionary<Proximate, Constraint> Constraints;
    public readonly Dictionary<Day, float> Population;

    public int NumIterations { get; protected set; } = 1;

    public readonly int NumStartingDaysInPop = 10;
    public readonly int NumStartingPortionsInDay = 1;
    public readonly int IdealNumberOfPortionsInDay = 20; // 2kg of food as a baseline
    public readonly int StartingPortionMassMin = 50;
    public readonly int StartingPortionMassMax = 150;


    public Algorithm()
    {
        // A CSV file containing all proximate data. In the context of the McCance Widdowson dataset,
        // this is most major nutrients (Protein, Fat, Carbs, Sugar, Sat Fats, Energy, Water, etc...)
        string file = File.ReadAllText(Application.dataPath + "/Proximates.csv");

        Preferences prefs = Preferences.Instance;

        m_foods = new DatasetReader().ReadFoods(file, prefs);

        Constraints = new(new Dictionary<Proximate, Constraint>
        {
            { Proximate.Protein, new NullConstraint() },
            { Proximate.Fat, new NullConstraint() },
            { Proximate.Carbs, new NullConstraint() },
            { Proximate.Kcal, new ConvergeConstraint(prefs.goals[Proximate.Kcal], 2, 0.00025f, prefs.goals[Proximate.Kcal]) },
            { Proximate.Sugar, new NullConstraint() },
            { Proximate.SatFat, new NullConstraint() },
            { Proximate.TransFat, new NullConstraint() }
        });
        Population = GetStartingPopulation();
    }


    /// <summary>
    /// Calculates the fitness for every element in the population, and caches it in a dictionary.
    /// NOTE: This is all done in the algorithm running method, but needs to be called once to calculate
    /// the initial fitnesses.
    /// </summary>
    public void CalculateInitialFitnesses()
    {
        Dictionary<Day, float> newFitnesses = new();

        // Two loops: Avoidance of iteration errors and allowing Population to remain readonly.
        foreach (Day day in Population.Keys)
        {
            newFitnesses[day] = day.GetFitness();
        }

        // Iterate rather than overwrite the object itself, to satisfy readonly pattern.
        foreach (Day day in newFitnesses.Keys)
        {
            Population[day] = newFitnesses[day];
        }
    }


    /// <summary>
    /// Populates the Population data structure with randomly generated Days.
    /// </summary>
    /// <returns>The created population data structure, WITHOUT evaluated fitnesses.</returns>
    protected Dictionary<Day, float> GetStartingPopulation()
    {
        // Generate random starting population of Days
        Dictionary<Day, float> days = new();
        for (int i = 0; i < NumStartingDaysInPop; i++)
        {
            // Add a number of days to the population (each has random foods)
            Day day = new();
            for (int j = 0; j < NumStartingPortionsInDay; j++)
            {
                // Add random foods to the day
                day.AddPortion(GenerateRandomPortion());
            }

            // Cannot add fitness yet, as this function is called during the constructor.
            // Fitness calculations require access to this object, which doesn't exist until
            // the constructor ends!
            days.Add(day, -1);
        }

        return days;
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
    }


    protected abstract void RunIteration();


    /// <summary>
    /// Use this to get the number of non-null constraints defined in the Constraints
    /// dictionary.
    /// </summary>
    public int GetNumConstraints()
    {
        int cnt = 0;
        foreach (Constraint c in Constraints.Values)
        {
            if (c.GetType() != typeof(NullConstraint))
            {
                cnt++;
            }
        }
        return cnt;
    }
}