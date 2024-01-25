using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;


public abstract class Algorithm
{
    // The constraints for each nutrient in a day.
    protected Dictionary<Proximate, Constraint> m_constraints;
    protected List<Food> m_foods;
    protected List<Day> m_population;

    public int NumStartingDaysInPop = 10;
    public int NumStartingPortionsInDay = 1;
    public int IdealNumberOfPortionsInDay = 9; // 3 portions of food per meal, 3 meals
    public float StartingFoodQuantityMin = 0.5f;
    public float StartingFoodQuantityMax = 1.5f;


    public Algorithm()
    {
        // A CSV file containing all proximate data. In the context of the McCance Widdowson dataset,
        // this is most major nutrients (Protein, Fat, Carbs, Sugar, Sat Fats, Energy, Water, etc...)
        string file = File.ReadAllText(Application.dataPath + "/Proximates.csv");
        m_foods = new DatasetReader().ReadFoods(file, Preferences.Saved);
        m_constraints = new Dictionary<Proximate, Constraint>
        {
            { Proximate.Protein, new ConvergeConstraint(180, 1, 0.001f, 180) },
            { Proximate.Fat, new ConvergeConstraint(100, 1, 0.001f, 100) },
            { Proximate.Carbs, new ConvergeConstraint(400, 1, 0.001f, 400) },
            { Proximate.Kcal, new ConvergeConstraint(3200, 1, 0.001f, 3200) },
            { Proximate.Sugar, new MinimiseConstraint(40, 2) },
            { Proximate.SatFat, new MinimiseConstraint(40, 2) },
            { Proximate.TransFat, new MinimiseConstraint(5, 4) }
        };
    }


    protected static string DayListToString(List<Day> days)
    {
        string str = "";
        for (int i = 0; i < days.Count; i++)
        {
            if (i > 0)
                str += "\n\n";
            str += $"Day {i}:\n{days[i]}";
        }

        return str;
    }


    public abstract void Run();


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


    protected Portion GenerateRandomPortion()
    {
        Food food = m_foods[Random.Range(0, m_foods.Count)];
        return new(food, Random.Range(StartingFoodQuantityMin, StartingFoodQuantityMax));
    }


    protected Fitness GetFitness(Day day)
    {
        return day._GetFitness(m_constraints, IdealNumberOfPortionsInDay);
    }
}