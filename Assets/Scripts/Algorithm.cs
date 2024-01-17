using System;
using System.Linq;
using System.Collections.Generic;
using Random = System.Random;
using UnityEngine;


public struct Food
{
    public string Name;
    public string Description;
    public string FoodGroup;
    public string Reference;

    public float Protein;
    public float Fat;
    public float Carbohydrates;
    public float Kcal;
}


public class Algorithm : MonoBehaviour
{
    [SerializeField]
    // A CSV file containing all proximate data. In the context of the McCance Widdowson dataset,
    // this is most major nutrients (Protein, Fat, Carbs, Sugar, Sat Fats, Energy, Water, etc...)
    private readonly TextAsset m_proximatesFile;
    private List<Food> m_foods;


    private void Awake()
    {
        m_foods = ReadFoods(m_proximatesFile.text);
    }


    /// <summary>
    /// This method converts a dataset file into a list of Food structs.
    /// </summary>
    /// <param name="csvText">The CSV data as a unformatted string.
    /// Each line must be separated by `\n` and values by `,`.</param>
    private List<Food> ReadFoods(string csvText)
    {
        List<Food> foods = new List<Food>();
        int currentWordIndex = 0;
        string currentWord = "";

        // A struct type so this can be edited and not affect foods in the list.
        Food currentFood = new();

        // Each line is a food. Each field is a food property.
        // Iterate over every character in the CSV file.
        foreach (char ch in csvText)
        {
            switch (ch)
            {
                case ',': // Delimiter, move onto the next word

                    // If the current word is not a null field, then add it to the food.
                    // The property we are setting here is determined by the word index.
                    if (currentWord == "" || currentWord == "N")
                        currentWord = "-1";

                    if (currentWord == "Tr")
                        currentWord = "0";

                    // For string fields, just assign the value.
                    // For float or int fields, we can safely parse the string, as
                    // the only string values these fields can have are checked
                    // in the containing if-clause.
                    switch (currentWordIndex)
                    {
                        case 1:
                            currentFood.Name = currentWord; break;
                        case 2:
                            currentFood.Description = currentWord; break;
                        case 3:
                            currentFood.FoodGroup = currentWord; break;
                        case 5:
                            currentFood.Reference = currentWord; break;
                        case 9:
                            currentFood.Protein = float.Parse(currentWord); break;
                        case 10:
                            currentFood.Fat = float.Parse(currentWord); break;
                        case 11:
                            currentFood.Carbohydrates = float.Parse(currentWord); break;
                        case 12:
                            currentFood.Kcal = float.Parse(currentWord); break;
                        case 16:
                            string sugars = currentWord;
                            break;
                        case 27:
                            string saturates = currentWord;
                            break;
                        case 45:
                            string trans = currentWord;
                            break;
                    }

                    currentWordIndex++;
                    break;
                case '\n': // New line
                    currentWordIndex = 0;

                    // Check the current food isn't missing essential data (due to N, Tr or "")
                    // This missing data is given the value -1, as seen in the delimiter ',' case.
                    if (currentFood.Protein < 0 || currentFood.Carbohydrates < 0 || currentFood.Fat < 0
                        || currentFood.Kcal < 0)
                    {
                        goto reset_food;
                    }

                    // Check the current food doesn't conflict with the user's dietary needs.
                    if (!IsFoodGroupAllowed(currentFood.FoodGroup))
                    {
                        print(currentFood.FoodGroup);
                        goto reset_food;
                    }

                    foods.Add(currentFood);

                    reset_food:
                    // Reset the current food's values, just as a precaution.
                    currentFood = new();
                    break;
                default: // Regular character, i.e. part of the next value
                    currentWord += ch;
                    break;
            }
        }

        return foods;
    }


    private bool IsFoodGroupAllowed(string foodGroup)
    {
        // Categories
        switch (foodGroup[0])
        {
            case 'M': // Meat
                if (!Preferences.Saved.eatsLandMeat)
                    return false;
                break;
            case 'J': // Fish
                if (!Preferences.Saved.eatsSeafood)
                    return false;
                break;
            case 'C': // Eggs
                if (!Preferences.Saved.eatsAnimalProduce)
                    return false;
                break;
            case 'B': // Milk
                if (!Preferences.Saved.eatsAnimalProduce || !Preferences.Saved.eatsLactose)
                    return false;
                break;
            case 'Q': // Alcohol - Excluded
                return false;
        }

        // Unique cases
        switch (foodGroup)
        {
            case "OB": // Animal fats
                if (!Preferences.Saved.eatsLandMeat)
                    return false;
                break;
        }

        return true;
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