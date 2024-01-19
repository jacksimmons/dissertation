using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


// 32-Bit Bit Field.

// Saves on memory usage for boolean values
// bool = 1 byte, but T/F can be stored in 1 bit.
// + BitField can store data 8x more memory efficiently.
// - Accessing a single value is more computationally expensive.
// + But performing bitwise operations negates this issue.
//public class BitField8
//{
//    // 8 bit integer - 8 fields
//    // Undefined behaviour if !(0 <= index <= 7)
//    public byte Data { get; private set; }

//    // Set every bit to 0 by default
//    public BitField8(byte data = 0) { Data = data; }

//    public void SetBit(int index, bool value)
//    {
//        byte mask = (byte)(1 << index);
//        Data = (byte)(value ? (Data | mask) : (Data & ~mask));
//    }

//    public bool GetBit(int index)
//    {
//        int mask = 1 << index;
//        return (Data & mask) != 0;
//    }
//}


public class GeneticAlgorithm : Algorithm
{
    public int NumStartingDaysInPop = 10;
    public int NumStartingFoodsInDay = 10;
    public float StartingFoodQuantityMin = 0.5f;
    public float StartingFoodQuantityMax = 1.5f;


    public override void Run()
    {
        // Generate population
        List<Day> pop = GetStartingPopulation();

        // Selection

        // Mutation

        // Crossover

        // Integration
    }


    private List<Day> GetStartingPopulation()
    {
        // Generate random starting population of Days
        List<Day> days = new();
        for (int i = 0; i < NumStartingDaysInPop; i++)
        {
            // Add a number of days to the population (each has random foods)
            List<Portion> dayPortions = new();
            for (int j = 0; j < NumStartingFoodsInDay; j++)
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


//public class AlgorithmOld
//{
//    private const float MIN_PORTION_SCALE = 0.5f;
//    private const float MAX_PORTION_SCALE = 2.0f;

//    private const int NUM_MEALS = 4;
//    private static Random rng = new();

//    private static List<Portion> allPortions = new();
//    private static List<Meal> mealPopulation = new();

//    // Soft constraints
//    private static List<Constraint> Constraints = new()
//    {
//    // Hard constraints
//    { new(Proximate.Trans, ConstraintType.HardLessThan, 50, 5) },
//    { new(Proximate.Sugars, ConstraintType.HardLessThan, 30, 36) },
//    { new(Proximate.Saturates, ConstraintType.HardLessThan, 30, 30) },

//    // Soft constraints
//    { new(Proximate.Energy, ConstraintType.Converge, 20, 3000) },

//    { new(Proximate.Fat, ConstraintType.Converge, 10, 250) },
//    { new(Proximate.Protein, ConstraintType.Converge, 10, 113) },
//    { new(Proximate.Carbs, ConstraintType.Converge, 10, 75) },
//    };


//    public static void Main()
//    {
//        Console.WriteLine($"Highest possible fitness: {Constraints.Select(x => x.Weight).Sum()}");

//        // Create portion population

//        // Randomly generate a smaller, local population, with no mutations
//        for (int i = 0; i < 10; i++)
//        {
//            int index = rng.Next(allPortions.Count);
//            Meal meal = new();
//            meal.Portions.Add(new(allPortions[index]));
//            mealPopulation.Add(meal);
//        }

//        int G = -1;

//        // Main loop of the algorithm
//        while (true)
//        {
//            G++;

//            Console.WriteLine($"GENERATION {G}");
//            CalculateFitnesses();

//            if (G > 0)
//            {
//                List<Meal> children = Run();
//                Console.WriteLine();

//                // Add to the population so they can be sorted with the rest
//                int addedWinners = 0;
//                for (int i = 0; i < children.Count; i++)
//                {
//                    if (children[i].Mutations == -1)
//                        continue;

//                    CalculateFitness(children[i]);
//                    mealPopulation.Add(children[i]);
//                    addedWinners++;
//                }

//                mealPopulation.Sort(Meal.Compare);

//                // "Kill off" the weakest entries
//                for (int j = 0; j < addedWinners; j++)
//                {
//                    mealPopulation.RemoveAt(0);
//                }
//            }

//            Display();
//            Console.WriteLine(mealPopulation.Count);

//            Console.WriteLine();
//            Console.WriteLine();
//            Console.WriteLine();
//            Console.WriteLine();

//            if (G % 100 == 0)
//                Console.ReadKey();
//        }
//    }


//    /// <summary>
//    /// Runs the body of the algorithm.
//    /// </summary>
//    /// <returns>The winning meals, to be added to the population.</returns>
//    private static List<Meal> Run()
//    {
//        // Tournament selection
//        // Children are initially copies of their parents before mutation, crossover, etc.
//        Meal child1 = new(Duel());
//        Meal child2 = new(Duel());

//        // Crossover (if applicable)
//        int portions1 = child1.Portions.Count;
//        int portions2 = child2.Portions.Count;
//        if (portions1 > 1 && portions2 > 1)
//        {
//            int index1 = rng.Next(portions1);
//            int index2 = rng.Next(portions2);

//            Portion portion1 = new(child1.Portions[index1]);
//            Portion portion2 = new(child2.Portions[index2]);

//            child1.Portions[index1] = portion2;
//            child2.Portions[index2] = portion1;
//        }

//        // Mutation
//        Mutate(child1); Mutate(child2);

//        return new List<Meal>() { child1, child2 };
//    }


//    private static void Mutate(Meal meal)
//    {
//        bool removePortion = CoinFlip();
//        bool addPortion = !removePortion;

//        // Remove random portion
//        if (removePortion)
//        {
//            if (meal.Portions.Count < 2)
//            {
//                meal.Mutations = -1; // Indicate the mutation failed and this meal is to be discarded.
//                return;
//            }
//            int randIndex = rng.Next(meal.Portions.Count);
//            meal.Portions.RemoveAt(randIndex);
//        }

//        // Add random portion
//        if (addPortion)
//        {
//            Portion newPortion = new(allPortions[rng.Next(allPortions.Count)]);
//            meal.Portions.Add(newPortion);
//        }

//        meal.Mutations++;
//    }


//    private static bool CoinFlip()
//    {
//        return rng.Next(2) == 1;
//    }


//    private static Meal Duel()
//    {
//        int popIndexA = rng.Next(mealPopulation.Count);
//        int popIndexB = rng.Next(mealPopulation.Count);

//        if (popIndexB == popIndexA)
//        {
//            if (popIndexA != mealPopulation.Count - 1)
//                popIndexB = Math.Min(popIndexB + 1, mealPopulation.Count - 1);
//            else
//                popIndexB = 0;
//        }

//        return Tournament(MaximiseFitness, new() { mealPopulation[popIndexA], mealPopulation[popIndexB] });
//    }


//    private static Winner MaximiseFitness(Meal L, Meal R)
//    {
//        Winner winner = Winner.None;

//        Console.WriteLine("Tournament: Fitness");
//        L.Print();
//        Console.WriteLine();
//        R.Print();
//        Console.WriteLine();

//        if (L.LastFitness >= R.LastFitness)
//        {
//            winner |= Winner.Left;
//        }
//        if (R.LastFitness >= L.LastFitness)
//        {
//            winner |= Winner.Right;
//        }

//        Console.WriteLine($"Left : {L.LastFitness}, Right : {R.LastFitness}, Winner : {winner}\n");
//        return winner;
//    }


//    /// <summary>
//    /// A generic tournament where 2 or more population members fight "to the death". If they are
//    /// equal, a coin flip decides the winner.
//    /// </summary>
//    /// <param name="comparison">A generic function which returns:
//    /// 1 if arg 1 beats arg 2. -1 if arg 2 beats arg 1. 0 if arg 1 and arg 2 are equals.</param>
//    /// <param name="competitors">A list of potential competitors.</param>
//    /// <returns>The winning meal.</returns>
//    private static Meal Tournament(Func<Meal, Meal, Winner> comparison, List<Meal> competitors)
//    {
//        int left = 0;
//        int right = 1;

//        // 1 competitor => they have already won; 0 competitors => No tournament
//        while (competitors.Count > 1)
//        {
//            Winner winner = comparison(competitors[left], competitors[right]);

//            if (winner == Winner.Left)
//            {
//                competitors.RemoveAt(right);
//            }
//            else if (winner == Winner.Right)
//            {
//                competitors.RemoveAt(left);
//            }
//            else
//            {
//                // Randomly strike one of the two competitors down by the equivalent of an
//                // "act of God" - there are no ties.

//                float lightningBolt = (float)rng.NextDouble();
//                if (lightningBolt >= 0.5f)
//                    competitors.RemoveAt(right);
//                else
//                    competitors.RemoveAt(left);
//            }
//        }

//        if (competitors.Count == 0)
//        {
//            Console.WriteLine("Empty tournament led to no winners.");
//            return null;
//        }

//        //competitors[0].Print();
//        //Console.WriteLine();

//        return competitors[0];
//    }


//    /// <summary>
//    /// Calls CalculateFitness on every meal in the population.
//    /// </summary>
//    private static void CalculateFitnesses()
//    {
//        for (int i = 0; i < mealPopulation.Count; i++)
//        {
//            mealPopulation[i].TotalFitness(Constraints, NUM_MEALS);
//        }
//    }


//    /// <summary>
//    /// Calculates the fitness of a meal in the population.
//    /// Cached under the LastFitness property.
//    /// </summary>
//    private static void CalculateFitness(Meal meal)
//    {
//        meal.TotalFitness(Constraints, NUM_MEALS);
//    }


//    private static List<Meal> SortFitnesses()
//    {
//        List<Meal> meals = new();

//        for (int i = 0; i < mealPopulation.Count; i++)
//        {
//            if (meals.Count == 0)
//                meals.Add(mealPopulation[i]);
//            else
//            {
//                for (int j = 0; j < meals.Count; j++)
//                {
//                    if (mealPopulation[i].LastFitness > meals[j].LastFitness)
//                    {
//                        if (j == meals.Count - 1)
//                            meals.Add(mealPopulation[i]);
//                        continue;
//                    }
//                    meals.Insert(j, mealPopulation[i]);
//                    break;
//                }
//            }
//        }

//        return meals;
//    }


//    private static void Display()
//    {
//        float totFitness = 0f;
//        foreach (Meal m in mealPopulation)
//        {
//            m.Print();
//            Console.WriteLine($"Fitness: {m.LastFitness}\n");
//            totFitness += m.LastFitness;
//        }
//        Console.WriteLine($"Avg. Fitness: {totFitness / mealPopulation.Count}");
//    }
//}