using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#if UNITY_64
using Random = System.Random;
#endif


public class AlgPDGA : AlgGA
{
    // The index of the list a day is in represents the non-dominated set it is in.
    // The higher the index, the more dominated the set is.
    // Set 0 - Mutually non-dominated by all
    // Set 1 - Mutually non-dominated by all once set 0 is removed
    // etc...
    // Up to Set N - The maximum sorting set
    private NonDominatedSorting m_sorting;
    public NonDominatedSorting Sorting
    {
        get
        {
            m_sorting ??= new();
            return m_sorting;
        }
    }


    // The index of sorting set the algorithm goes up to during a tiebreak.
    // The higher it is, the more likely to resolve a tiebreak.
    //
    // [0, PopHierarchy.Length)
    protected const int SELECTION_PRESSURE = 10;


    public override void PostConstructorSetup()
    {
        foreach (Day day in Population)
        {
            Sorting.TryAddDayToSort(day);
        }
    }


    protected override void RunIteration()
    {
        base.RunIteration();
    }


    protected override void AddToPopulation(Day day)
    {
        base.AddToPopulation(day);

        Sorting.TryAddDayToSort(day);
    }


    protected override void RemoveFromPopulation(Day day)
    {
        base.RemoveFromPopulation(day);

        Sorting.TryRemoveDayFromSort(day);
    }


    ///// <summary>
    ///// Pareto-dominance tournament selection.
    ///// </summary>
    //protected override Day Selection(List<Day> candidates, bool selectBest = true)
    //{
    //    int indexA = Random.Range(0, candidates.Count);
    //    // Ensure B is different to A by adding an amount less than the list size, then %-ing it.
    //    int indexB = (indexA + Random.Range(1, candidates.Count - 1)) % candidates.Count;

    //    // If one dominates the other, the selection is simple.
    //    // Don't make use of PopHierarchy, because this method is sometimes called before PopHierarchy
    //    // is updated with the new population.
    //    switch (Day.Compare(candidates[indexA], candidates[indexB]))
    //    {
    //        case ParetoComparison.Dominates:
    //            return selectBest ? candidates[indexA] : candidates[indexB];
    //        case ParetoComparison.Dominated:
    //            return selectBest ? candidates[indexB] : candidates[indexA];
    //    }

    //    // Otherwise, tiebreak by comparison set
    //    return SelectionTiebreak(candidates[indexA], candidates[indexB]);
    //}


    protected override Day Selection(List<Day> candidates, bool selectBest = true)
    {
        int indexA = m_rand.Next(candidates.Count);
        // Ensure B is different to A by adding an amount less than the list size, then %-ing it.
        int indexB = (indexA + m_rand.Next(1, candidates.Count - 1)) % candidates.Count;

        int rankA = Sorting.TryGetDayRank(candidates[indexA]);
        int rankB = Sorting.TryGetDayRank(candidates[indexB]);

        if (rankA < rankB)
            return candidates[indexA];
        if (rankB < rankA)
            return candidates[indexB];

        // Otherwise, tiebreak by comparison set
        return SelectionTiebreak(candidates[indexA], candidates[indexB]);
    }


    /// <summary>
    /// Pick the winner based on their total dominance over the population.
    /// </summary>
    protected override Day SelectionTiebreak(Day a, Day b)
    {
        List<Day> comparisonSet = new(Population);
        for (int i = 0; i < Sorting.Sets.Count; i++)
        {
            comparisonSet.AddRange(Sorting.Sets[i].Days);
        }

        comparisonSet.Remove(a);
        comparisonSet.Remove(b);

        int dominanceA = GetDominanceOverComparisonSet(a, comparisonSet);
        int dominanceB = GetDominanceOverComparisonSet(b, comparisonSet);

        if (dominanceA > dominanceB)
            return a;
        if (dominanceB > dominanceA)
            return b;

        if (m_rand.Next(2) == 1)
            return a;
        return b;
    }


    /// <summary>
    /// Calculates how dominant a day is over a comparison set.
    /// </summary>
    /// <param name="day"></param>
    /// <param name="comparisonSet"></param>
    /// <returns>An integer, which starts at 0 and increments every time the day dominates,
    /// and decrements every time it is dominated.</returns>
    protected int GetDominanceOverComparisonSet(Day day, List<Day> comparisonSet)
    {
        int dominance = 0;
        for (int i = 0; i < comparisonSet.Count; i++)
        {
            int rankDay = Sorting.TryGetDayRank(day);
            int rankOther = Sorting.TryGetDayRank(comparisonSet[i]);

            if (rankDay < rankOther)
                dominance++;
            if (rankOther < rankDay)
                dominance--;
        }

        return dominance;
    }
}