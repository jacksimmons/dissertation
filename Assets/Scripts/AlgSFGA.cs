using System;
using System.Collections.Generic;
using System.Linq;


public class AlgSFGA : AlgGA
{
    private Func<List<Day>, bool, Day> m_selectionMethod;

    // Increase selection pressure as fitness decreases
    private float SP
    {
        get
        {
            //if (BestFitness >= Preferences.MAX_FITNESS)
            //    return 1;
            //if (BestFitness <= Preferences.CRITICAL_FITNESS)
            //    return 2;

            //// Graph 1 + (1/M)(M-x)
            //float p = 1f + (1f / Preferences.MAX_FITNESS) * (Preferences.MAX_FITNESS - BestFitness);
            //return p;

            // 500k iterations => Max pressure
            return 1.5f;
            return 2 - (1/IterNum);
        }
    }


    public override void Init()
    {
        base.Init();

        m_selectionMethod = RankSelection;
    }


    protected override void NextIteration()
    {
        // --- Elitism + Reproduction ---
        // Iterate over the number of demanded parents
        // MODEL: Each parent pair produces a pair of children.
        List<Day> included = new(m_population.DayFitnesses.Keys);

        // Need to sort the list for Rank selection
        if (m_selectionMethod == RankSelection)
        {
            m_population.SortDayList(included);
        }

        List<Day> allChildren = new();
        for (int i = 0; i < m_numParents; i += 2)
        {
            Tuple<Day, Day> parents = PerformPairSelection(included, true);

            // Get the children and add to the population.
            Tuple<Day, Day> children = Reproduction(parents);
            allChildren.Add(children.Item1);
            allChildren.Add(children.Item2);
        }


        // --- Elimination ---
        // For each parent, kill off 1 candidate. (Reproduction - Elimination) === 0
        included = new(m_population.DayFitnesses.Keys); // Any day can get eliminated, even the children

        // Need to sort the list for Rank selection
        if (m_selectionMethod == RankSelection)
        {
            m_population.SortDayList(included, true);
        }

        List<Day> allDead = new();
        for (int i = 0; i < m_numParents; i++)
        {
            // Select and eliminate two days per parent (to keep population size stable)
            Day selectedDay = PerformSelection(included, false);
            included.Remove(selectedDay);

            // Remove the selected day from the population
            allDead.Add(selectedDay);
        }


        // --- Population Update ---
        // Updates are performed after selection to reduce Population class updates during it.
        for (int i = 0; i < allChildren.Count; i++)
        {
            AddToPopulation(allChildren[i]);
        }
        for (int i = 0; i < allDead.Count; i++)
        {
            RemoveFromPopulation(allDead[i]);
        }
    }


    public Tuple<Day, Day> PerformPairSelection(List<Day> included, bool selectBest)
    {
        Day selectedDayA = PerformSelection(included, selectBest);
        included.Remove(selectedDayA);
        Day selectedDayB = PerformSelection(included, selectBest);
        included.Remove(selectedDayB);

        return new(selectedDayA, selectedDayB);
    }


    private Day PerformSelection(List<Day> included, bool selectBest)
    {
        if (included.Count == 0) Logger.Error("Included cannot be empty when performing selection.");
        if (included.Count == 1) return included[0];

        return m_selectionMethod(included, selectBest);
    }


    /// <summary>
    /// Returns the "first" best day in a list of randomly picked days.
    /// ("first" meaning if two have identical fitness, the first in the list gets returned)
    /// </summary>
    /// <param name="included">The list of days to select from.</param>
    /// <param name="selectBest">`true` => Select the lowest fitness. `false` => Select the highest fitness.</param>
    /// <returns></returns>
    public Day TournamentSelection(List<Day> included, bool selectBest)
    {
        int tournamentSize = Math.Min(included.Count, m_tournamentSize);

        if (tournamentSize == 0)
            Logger.Error("Tournament size must be positive and non-zero.");

        List<Day> days = new(included);
        Day bestDay = null;
        float bestFitness = selectBest ? float.PositiveInfinity : 0;

        for (int i = 0; i < tournamentSize; i++)
        {
            int index = Rand.Next(days.Count);
            Day day = days[index];
            days.RemoveAt(index);

            float fitness = m_population.GetFitness(day);

            if (selectBest && fitness < bestFitness || !selectBest && fitness > bestFitness || bestDay == null)
            {
                bestDay = day;
                bestFitness = fitness;
            }
        }

        return bestDay!; // Guaranteed to not be null; if they all have inf fitness the first picked day will be returned.
    }


    public Day RankSelection(List<Day> sortedIncluded, bool selectBest)
    {
        // Number of elements to select from
        int n = sortedIncluded.Count;
        float[] rankProbs = new float[n];

        // Get probability of each rank getting selected
        for (int i = 0; i < rankProbs.Length; i++)
        {
            rankProbs[i] = (1.0f / n) * (SP - (2 * SP - 2) * (i - 1.0f) / (n - 1.0f));
        }

        // Select a rank with weighted random. This rank will correspond to an element in both population and `included`.
        int selectedRank = MathfTools.GetFirstSurpassedProbability(rankProbs);
        return sortedIncluded[selectedRank];
    }


    ///// <summary>
    ///// In a summed fitness algorithm, just randomly pick the tiebreak winner.
    ///// </summary>
    //protected Day Tiebreak(Day a, Day b)
    //{
    //    if (Rand.Next(0, 2) == 1)
    //        return a;
    //    return b;
    //}
}