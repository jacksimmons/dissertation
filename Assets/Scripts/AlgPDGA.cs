//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;


//#if UNITY_64
//using Random = System.Random;
//#endif


//public class AlgPDGA : AlgGA
//{
//    // The index of sorting set the algorithm goes up to during a tiebreak.
//    // The higher it is, the more likely to resolve a tiebreak.
//    //
//    // [0, PopHierarchy.Length)
//    protected const int SELECTION_PRESSURE = 5;
//    public const int NUM_SORTING_SETS = 10;

//    private List<MNDSet> m_sets;
//    public ReadOnlyCollection<MNDSet> Sets;


//    public override void Init()
//    {
//        m_sets = new();
//        Sets = new(m_sets);
     
//        base.Init();
//    }


//    ///// <summary>
//    ///// Pareto-dominance tournament selection.
//    ///// </summary>
//    //protected override Day Selection(List<Day> candidates, bool selectBest = true)
//    //{
//    //    int indexA = Random.Range(0, candidates.Count);
//    //    // Ensure B is different to A by adding an amount less than the list size, then %-ing it.
//    //    int indexB = (indexA + Random.Range(1, candidates.Count - 1)) % candidates.Count;

//    //    // If one dominates the other, the selection is simple.
//    //    // Don't make use of PopHierarchy, because this method is sometimes called before PopHierarchy
//    //    // is updated with the new population.
//    //    switch (Day.Compare(candidates[indexA], candidates[indexB]))
//    //    {
//    //        case ParetoComparison.Dominates:
//    //            return selectBest ? candidates[indexA] : candidates[indexB];
//    //        case ParetoComparison.Dominated:
//    //            return selectBest ? candidates[indexB] : candidates[indexA];
//    //    }

//    //    // Otherwise, tiebreak by comparison set
//    //    return SelectionTiebreak(candidates[indexA], candidates[indexB]);
//    //}


//    /// <summary>
//    /// Adds a day (if possible) to one of the comparison sets.
//    /// If the day gets dominated by all other comparison set members, it will not get added to any.
//    /// </summary>
//    protected override void AddToPopulation(Day day)
//    {
//        base.AddToPopulation(day);

//        for (int i = 0; i < Sets.Count; i++)
//        {
//            MNDSet set = Sets[i];
//            switch (Pareto.UnstrictComparison(set.Compare(day)))
//            {
//                // If the first set it beats, it dominates it, then we need a new set for this Day.
//                case ParetoComparison.Dominates:
//                    MNDSet newSet = new();
//                    newSet.Add(day);
//                    m_sets.Insert(i, newSet);
//                    return;

//                // MND => This day belongs in this set.
//                case ParetoComparison.MutuallyNonDominating:
//                    m_sets[i].Add(day);
//                    return;

//                // Continue until we find a set matching the above criteria
//                case ParetoComparison.Dominated:
//                default:
//                    continue;
//            }
//        }
//    }


//    /// <summary>
//    /// Will remove a day from the sort, if it is in a comparison set.
//    /// </summary>
//    protected override void RemoveFromPopulation(Day day)
//    {
//        base.RemoveFromPopulation(day);

//        for (int i = 0; i < Sets.Count; i++)
//        {
//            MNDSet set = Sets[i];
//            if (set.Days.Contains(day))
//            {
//                set.Remove(day);
//                // Remove resulting empty set
//                if (set.Days.Count == 0)
//                    m_sets.Remove(set);
//            }
//        }
//    }


//    protected override void Selection()
//    {
//        //List<Day> days = new(m_population.Days);
//        //Day a = PickRandomDay(days);
//        //days.Remove(a);
//        //Day b = PickRandomDay(days);
//        //days.Remove(b);

//        //int rankA = TryGetDayRank(a);
//        //int rankB = TryGetDayRank(b);

//        //if (selectBest)
//        //{
//        //    if (rankA < rankB) return a;
//        //    if (rankB < rankA) return b;
//        //}
//        //else
//        //{
//        //    if (rankA > rankB) return a;
//        //    if (rankB > rankA) return b;
//        //}

//        //// Otherwise, tiebreak by comparison set
//        //return Tiebreak(a, b, selectBest);
//    }


//    /// <summary>
//    /// If the day is in a comparison set, returns the set's rank (set number).
//    /// Otherwise, returns -1.
//    /// </summary>
//    public int TryGetDayRank(Day day)
//    {
//        for (int i = 0; i < Sets.Count; i++)
//        {
//            MNDSet set = Sets[i];
//            if (set.Days.Contains(day))
//            {
//                return i;
//            }
//        }
//        return -1;
//    }


//    /// <summary>
//    /// First, tiebreak based on their total dominance over the population.
//    /// If a has a domination of n, and b has a domination of n + 1, b wins.
//    /// 
//    /// If it still hasn't been resolved, tiebreak based on fitness.
//    /// 
//    /// If it still hasn't, select randomly.
//    /// </summary>
//    private Day Tiebreak(Day a, Day b, bool selectBest)
//    {
//        List<Day> comparisonSet = new(m_population.DayFitnesses.Keys);
//        for (int i = 0; i < Sets.Count; i++)
//        {
//            comparisonSet.AddRange(Sets[i].Days);
//        }

//        comparisonSet.Remove(a);
//        comparisonSet.Remove(b);

//        int dominanceA = GetDominanceOverComparisonSet(a, comparisonSet);
//        int dominanceB = GetDominanceOverComparisonSet(b, comparisonSet);
//        if (selectBest)
//        {
//            if (dominanceA > dominanceB) return a;
//            if (dominanceB > dominanceA) return b;
//            if (m_population.GetFitness(a) < m_population.GetFitness(b)) return a;
//            if (m_population.GetFitness(b) < m_population.GetFitness(a)) return b;
//        }
//        else
//        {
//            if (dominanceA < dominanceB) return a;
//            if (dominanceB < dominanceA) return b;
//            if (m_population.GetFitness(a) > m_population.GetFitness(b)) return a;
//            if (m_population.GetFitness(b) > m_population.GetFitness(a)) return b;
//        }

//        if (Rand.Next(2) == 1)
//            return a;
//        return b;
//    }


//    /// <summary>
//    /// Calculates how dominant a day is over a mutually-non-dominating set.
//    /// </summary>
//    /// <returns>An integer, which starts at 0 and increments every time the day dominates,
//    /// and decrements every time it is dominated.</returns>
//    protected int GetDominanceOverComparisonSet(Day day, List<Day> set)
//    {
//        int dominance = 0;
//        for (int i = 0; i < set.Count; i++)
//        {
//            int rankDay = TryGetDayRank(day);
//            int rankOther = TryGetDayRank(set[i]);

//            if (rankDay < rankOther)
//                dominance++;
//            if (rankOther < rankDay)
//                dominance--;
//        }

//        return dominance;
//    }
//}


//public class MNDSet
//{
//    private List<Day> _days;
//    public ReadOnlyCollection<Day> Days;


//    public MNDSet()
//    {
//        _days = new();
//        Days = new(_days);
//    }


//    /// <summary>
//    /// Compares the given day to the rest of the set. Returns the worst comparison for
//    /// the provided day that occurs in the set.
//    /// </summary>
//    public ParetoComparison Compare(Day day)
//    {
//        ParetoComparison comparison = ParetoComparison.StrictlyDominates;
//        foreach (Day other in Days)
//        {
//            if (other == null) continue;

//            ParetoComparison otherComp = day.CompareTo(other);
//            if ((int)otherComp > (int)comparison)
//            {
//                comparison = otherComp;
//            }
//        }
//        return comparison;
//    }


//    public void Add(Day day)
//    {
//        _days.Add(day);
//    }


//    public void Remove(Day day)
//    {
//        _days.Remove(day);
//    }
//}