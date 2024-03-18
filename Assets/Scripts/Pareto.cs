using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;


public enum ParetoComparison
{
    StrictlyDominates,
    Dominates,
    MutuallyNonDominating,
    Dominated,
    StrictlyDominated
}


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

            switch (UnstrictComparison(day.CompareTo(other)))
            {
                // Only case where this day is NOT non-dominated.
                case ParetoComparison.Dominated:
                    return false;
            }
        }

        return true;
    }
}