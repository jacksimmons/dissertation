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
            return m_instance = GameObject.FindWithTag("AlgorithmRunner").GetComponent<AlgorithmBehaviour>().Algorithm;
        }

        protected set { m_instance = value; }
    }


    // The constraints for each nutrient in a day.
    protected List<Food> m_foods;

    public readonly ReadOnlyDictionary<Proximate, Constraint> Constraints;
    public readonly List<Day> Population;

    public ReadOnlyDictionary<Day, uint> PopHierarchy;

    public int NumIterations { get; protected set; } = 1;

    public const int NumStartingDaysInPop = 10;
    public const int NumStartingPortionsInDay = 1;
    public const int StartingPortionMassMin = 50;
    public const int StartingPortionMassMax = 150;


    public Algorithm()
    {
        // A CSV file containing all proximate data. In the context of the McCance Widdowson dataset,
        // this is most major nutrients (Protein, Fat, Carbs, Sugar, Sat Fats, Energy, Water, etc...)
        string file = File.ReadAllText(Application.dataPath + "/Proximates.csv");

        Preferences prefs = Preferences.Instance;

        m_foods = new DatasetReader().ReadFoods(file, prefs);

        Constraints = new(new Dictionary<Proximate, Constraint>
        {
            { Proximate.Protein, new ConvergeConstraint(prefs.goals[(int)Proximate.Protein], 0.00025f, prefs.goals[(int)Proximate.Protein]) },
            { Proximate.Fat, new ConvergeConstraint(prefs.goals[(int)Proximate.Fat], 0.00025f, prefs.goals[(int)Proximate.Fat]) },
            { Proximate.Carbs, new ConvergeConstraint(prefs.goals[(int)Proximate.Carbs], 0.00025f, prefs.goals[(int)Proximate.Carbs]) },
            { Proximate.Kcal, new ConvergeConstraint(prefs.goals[(int)Proximate.Kcal], 0.00025f, prefs.goals[(int)Proximate.Kcal]) },
            { Proximate.Sugar, new NullConstraint() },
            { Proximate.SatFat, new NullConstraint() },
            { Proximate.TransFat, new NullConstraint() }
        });
        Population = GetStartingPopulation();
    }


    /// <summary>
    /// Populates the Population data structure with randomly generated Days.
    /// </summary>
    /// <returns>The created population data structure, WITHOUT evaluated fitnesses.</returns>
    protected List<Day> GetStartingPopulation()
    {
        // Generate random starting population of Days
        List<Day> days = new();
        for (int i = 0; i < NumStartingDaysInPop; i++)
        {
            // Add a number of days to the population (each has random foods)
            Day day = new();
            for (int j = 0; j < NumStartingPortionsInDay; j++)
            {
                // Add random foods to the day
                day.AddPortion(GenerateRandomPortion());
            }

            days.Add(day);
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


    public void AssignDominanceHierarchy()
    {
        PopHierarchy = new(Pareto.GetDominanceHierarchy(new(Population)));
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
        AssignDominanceHierarchy();
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