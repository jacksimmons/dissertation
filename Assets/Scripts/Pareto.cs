using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;

public static class Pareto
{
    /// <summary>
    /// Converts a regular pareto comparison to one which doesn't include strict domination.
    /// </summary>
    public static ParetoComparison UnstrictComparison(ParetoComparison p)
    {
        return p switch
        {
            ParetoComparison.StrictlyDominates or ParetoComparison.Dominates => ParetoComparison.Dominates,
            ParetoComparison.StrictlyDominated or ParetoComparison.Dominated => ParetoComparison.Dominated,
            _ => ParetoComparison.MutuallyNonDominating,
        };
    }


    public static bool DominatesOrMND(ParetoComparison p)
    {
        return p switch
        {
            ParetoComparison.StrictlyDominates or ParetoComparison.Dominates or ParetoComparison.MutuallyNonDominating => true,
            _ => false
        };
    }


    public static bool DominatedOrMND(ParetoComparison p)
    {
        return p switch
        {
            ParetoComparison.StrictlyDominated or ParetoComparison.Dominated or ParetoComparison.MutuallyNonDominating => true,
            _ => false
        };
    }


    public static bool IsNonDominated(Day day, List<Day> population)
    {
        foreach (Day other in population)
        {
            if (day == other) continue;

            switch (UnstrictComparison(Day.Compare(day, other)))
            {
                // Only case where this day is NOT non-dominated.
                case ParetoComparison.Dominated:
                    return false;
            }
        }

        return true;
    }
}


public class NonDominatedSorting
{
    public const int NUM_SORTING_SETS = 10;

    private List<ComparisonSet> _sets;
    public ReadOnlyCollection<ComparisonSet> Sets;


    public NonDominatedSorting()
    {
        _sets = new()
        {
            new()
        };

        Sets = new(_sets);
    }


    /// <summary>
    /// Adds a day (if possible) to one of the comparison sets.
    /// If the day gets dominated by all other comparison set members, it will not get added to any.
    /// </summary>
    public void TryAddDayToSort(Day day)
    {
        for (int i = 0; i < Sets.Count; i++)
        {
            ComparisonSet set = Sets[i];
            switch (Pareto.UnstrictComparison(set.Compare(day)))
            {
                // If the first set it beats, it dominates it, then we need a new set for this Day.
                case ParetoComparison.Dominates:
                    ComparisonSet newSet = new();
                    newSet.Add(day);
                    _sets.Insert(i, newSet);
                    return;

                // MND => This day belongs in this set.
                case ParetoComparison.MutuallyNonDominating:
                    _sets[i].Add(day);
                    return;

                // Continue until we find a set matching the above criteria
                case ParetoComparison.Dominated:
                default:
                    continue;
            }
        }
    }


    /// <summary>
    /// If the day is in a comparison set, returns the set's rank (set number).
    /// Otherwise, returns -1.
    /// </summary>
    public int TryGetDayRank(Day day)
    {
        for (int i = 0; i < Sets.Count; i++)
        {
            ComparisonSet set = Sets[i];
            if (set.Days.Contains(day))
            {
                return i;
            }
        }
        return -1;
    }


    /// <summary>
    /// Will remove a day from the sort, if it is in a comparison set.
    /// </summary>
    public void TryRemoveDayFromSort(Day day)
    {
        for (int i = 0; i < Sets.Count; i++)
        {
            ComparisonSet set = Sets[i];
            if (set.Days.Contains(day))
            {
                set.Remove(day);
                // Remove resulting empty set
                if (set.Days.Count == 0)
                    _sets.Remove(set);
            }
        }
    }
}


public class ComparisonSet
{
    private List<Day> _days;
    public ReadOnlyCollection<Day> Days;


    public ComparisonSet()
    {
        _days = new();
        Days = new(_days);
    }


    /// <summary>
    /// Compares the given day to the rest of the set. Returns the worst comparison for
    /// the provided day that occurs in the set.
    /// </summary>
    public ParetoComparison Compare(Day day)
    {
        ParetoComparison comparison = ParetoComparison.StrictlyDominates;
        foreach (Day other in Days)
        {
            if (other == null) continue;

            ParetoComparison otherComp = Day.Compare(day, other);
            if ((int)otherComp > (int)comparison)
            {
                comparison = otherComp;
            }
        }
        return comparison;
    }


    public void Add(Day day)
    {
        _days.Add(day);
    }


    public void Remove(Day day)
    {
        _days.Remove(day);
    }
}