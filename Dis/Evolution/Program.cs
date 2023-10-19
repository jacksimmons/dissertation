using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace EA;


enum Winner
{
    None = 0,
    Left = 1, // == Left | None
    Right = 2, // == Right | None
    Draw = 3 // == Left | Right
}


class Algorithm
{
    private const float MIN_PORTION_SCALE = 0.5f;
    private const float MAX_PORTION_SCALE = 2.0f;

    private const bool VEGETARIAN = true;

    private readonly static Random rng = new();

    private static List<Portion> globalPopulation = new();
    private static List<Portion> localPopulation = new();


    public static void Main()
    {
        // Create global population
        Evolution evo = new(true);
        globalPopulation = evo.ExtractProximates();


        // Randomly generate a smaller, local population, with no mutations
        for (int i = 0; i < 10; i++)
        {
            int index = rng.Next(globalPopulation.Count);
            localPopulation.Add(globalPopulation[index]);
        }

        foreach (Portion p in localPopulation)
        {
            p.Print();
        }

        Run();
    }


    private static void Run()
    {
        //// Tournament selection
        //Meal winner1 = Duel(population);
        //Meal winner2 = Duel(population);

        //// Crossover
        //Meal crossover = 

        // Make a portion of both


        // Mutation

    }


    private static Meal Duel(List<Meal> population)
    {
        int popIndexA = rng.Next(population.Count);
        int popIndexB = rng.Next(population.Count);

        if (popIndexB == popIndexA)
        {
            if (popIndexA != population.Count - 1)
                popIndexB = Math.Min(popIndexB + 1, population.Count - 1);
            else
                popIndexB = 0;
        }

        return Tournament(MinimiseCalories, new() { population[popIndexA], population[popIndexB] });
    }


    private static Winner MinimiseCalories(Meal L, Meal R)
    {
        Winner winner = Winner.None;
        float LCalories = L.Portions[0].KCAL;
        float RCalories = R.Portions[0].KCAL;

        Console.WriteLine("Tournament: Minimise");
        L.Print();
        R.Print();

        if (LCalories <= RCalories)
        {
            winner |= Winner.Left;
        }
        if (RCalories <= LCalories)
        {
            winner |= Winner.Right;
        }

        Console.WriteLine("Winner: " + winner);
        return winner;
    }


    /// <summary>
    /// A generic tournament where 2 or more population members fight "to the death". If they are
    /// equal, a coin flip decides the winner.
    /// </summary>
    /// <param name="comparison">A generic function which returns:
    /// 1 if arg 1 beats arg 2. -1 if arg 2 beats arg 1. 0 if arg 1 and arg 2 are equals.</param>
    /// <param name="competitors">A list of potential competitors.</param>
    /// <returns>The winning meal.</returns>
    private static Meal Tournament(Func<Meal, Meal, Winner> comparison, List<Meal> competitors)
    {
        int left = 0;
        int right = 1;

        // 1 competitor => they have already won; 0 competitors => No tournament
        while (competitors.Count > 1)
        {
            Winner winner = comparison(competitors[left], competitors[right]);

            if (winner == Winner.Left)
            {
                competitors.RemoveAt(right);
            }
            else if (winner == Winner.Right)
            {
                competitors.RemoveAt(left);
            }
            else
            {
                // Randomly strike one of the two competitors down by the equivalent of an
                // "act of God" - there are no ties.

                float lightningBolt = rng.NextSingle();
                if (lightningBolt >= 0.5f)
                    competitors.RemoveAt(right);
                else
                    competitors.RemoveAt(left);
            }
        }

        if (competitors.Count == 0)
        {
            Console.WriteLine("Empty tournament led to no winners.");
            return null;
        }
        competitors[0].Print();
        return competitors[0];
    }
}