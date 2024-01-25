using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public abstract class Algorithm : MonoBehaviour
{
    // A CSV file containing all proximate data. In the context of the McCance Widdowson dataset,
    // this is most major nutrients (Protein, Fat, Carbs, Sugar, Sat Fats, Energy, Water, etc...)
    [SerializeField]
    private TextAsset m_proximatesFile;

    // The constraints for each nutrient in a day.
    protected Dictionary<ProximateType, Constraint> m_constraints;
    protected List<Food> m_foods;
    protected List<Day> m_population;

    public int NumStartingDaysInPop = 10;
    public int NumStartingPortionsInDay = 1;
    public float StartingFoodQuantityMin = 0.5f;
    public float StartingFoodQuantityMax = 1.5f;


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


    private void Start()
    {
        m_foods = new DatasetReader().ReadFoods(m_proximatesFile.text, Preferences.Saved);
        m_constraints = new Dictionary<ProximateType, Constraint>
        {
            { ProximateType.Protein, new(ConstraintType.Converge, 1f, 180) },
            { ProximateType.Fat, new(ConstraintType.Converge, 1f, 100) },
            { ProximateType.Carbs, new(ConstraintType.Converge, 1f, 400) },
            { ProximateType.Kcal, new(ConstraintType.Converge, 1f, 3200) }
        };
    }


    public abstract void Run();


    protected List<Day> GetStartingPopulation()
    {
        // Generate random starting population of Days
        List<Day> days = new();
        for (int i = 0; i < NumStartingDaysInPop; i++)
        {
            // Add a number of days to the population (each has random foods)
            List<Portion> dayPortions = new();
            for (int j = 0; j < NumStartingPortionsInDay; j++)
            {
                // Add random foods to the day
                int randFoodIndex = Random.Range(0, m_foods.Count);
                float randFoodQuantity = Random.Range(StartingFoodQuantityMin, StartingFoodQuantityMax);

                Food food = m_foods[randFoodIndex];
                Portion portion = new(food, randFoodQuantity);

                dayPortions.Add(portion);
            }

            days.Add(new(dayPortions));
        }

        return days;
    }
}