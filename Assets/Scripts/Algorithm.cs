using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;
using System.Collections.ObjectModel;


public abstract class Algorithm
{
    // The constraints for each nutrient in a day.
    protected Dictionary<Proximate, Constraint> m_constraints;
    protected List<Food> m_foods;

    protected Dictionary<Day, Fitness> m_population;

    public int NumIterations { get; protected set; } = 1;

    public int NumStartingDaysInPop = 10;
    public int NumStartingPortionsInDay = 1;
    public int IdealNumberOfPortionsInDay = 20; // 2kg of food as a baseline
    public float StartingPortionMassMin = 50;
    public float StartingPortionMassMax = 150;


    public Algorithm()
    {
        // A CSV file containing all proximate data. In the context of the McCance Widdowson dataset,
        // this is most major nutrients (Protein, Fat, Carbs, Sugar, Sat Fats, Energy, Water, etc...)
        string file = File.ReadAllText(Application.dataPath + "/Proximates.csv");

        Preferences prefs = Preferences.Saved;

        m_foods = new DatasetReader().ReadFoods(file, prefs);
        m_constraints = new Dictionary<Proximate, Constraint>
        {
            { Proximate.Protein, new RangeConstraint(0, 200) },
            { Proximate.Fat, new RangeConstraint(0, 200) },
            { Proximate.Carbs, new RangeConstraint(0, 400) },
            { Proximate.Kcal, new ConvergeConstraint(prefs.goals[Proximate.Kcal], 2, 0.0025f, prefs.goals[Proximate.Kcal] / 2) },
            { Proximate.Sugar, new MinimiseConstraint(40, 2) },
            { Proximate.SatFat, new MinimiseConstraint(40, 2) },
            { Proximate.TransFat, new MinimiseConstraint(5, 4) }
        };

        // Generate population
        m_population = GetStartingPopulation();
    }


    public ReadOnlyDictionary<Day, Fitness> GetPopulation()
    {
        return new(m_population);
    }


    protected Dictionary<Day, Fitness> GetStartingPopulation()
    {
        // Generate random starting population of Days
        Dictionary<Day, Fitness> days = new();
        for (int i = 0; i < NumStartingDaysInPop; i++)
        {
            // Add a number of days to the population (each has random foods)
            Day day = new();
            for (int j = 0; j < NumStartingPortionsInDay; j++)
            {
                // Add random foods to the day
                day.AddPortion(GenerateRandomPortion());
            }

            days.Add(day, GetFitness(day));
        }

        return days;
    }


    public void NextIteration()
    {
        RunIteration();
        UpdateFitnesses();
    }


    protected abstract void RunIteration();


    protected Portion GenerateRandomPortion()
    {
        Food food = m_foods[Random.Range(0, m_foods.Count)];
        return new(food, Random.Range(StartingPortionMassMin, StartingPortionMassMax));
    }


    protected Fitness GetFitness(Day day)
    {
        return day._GetFitness(m_constraints, IdealNumberOfPortionsInDay);
    }


    private void UpdateFitnesses()
    {
        Dictionary<Day, Fitness> newFitnesses = new();
        foreach (Day day in m_population.Keys)
        {
            newFitnesses[day] = GetFitness(day);
        }
        m_population = newFitnesses;
    }


    private string FoodAsString(Food food)
    {
        string foodAsString = "";
        foreach (Proximate proximate in Enum.GetValues(typeof(Proximate)))
        {
            float proximateAmount = food.Nutrients[proximate];
            foodAsString += proximate.ToString() + $": {proximateAmount}{Food.GetProximateUnit(proximate)} (Fitness: {m_constraints[proximate]._GetFitness(proximateAmount)})\n";
        }
        return foodAsString;
    }


    public string PortionToString(Portion portion)
    {
        return FoodAsString(portion.Food) + $"Mass: {portion.Mass}";
    }
}