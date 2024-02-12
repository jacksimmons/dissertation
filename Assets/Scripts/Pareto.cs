using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;

public static class Pareto
{
    public static List<List<Day>> GetSortedNonDominatedSets(ReadOnlyCollection<Day> population)
    {
        if (population.Count < 1) throw new IndexOutOfRangeException("Population was empty.");

        List<Day> days = new(population);
        List<List<Day>> popHierarchy = new();

        int sortedCount = 0;


        // Init:   Set `days` equal to `population`.
        // Repeat: Find every element in `days` that is not dominated by the elements in `days`.
        //         Once iteration completes, remove these from `days` and continue. Take a note of the
        //         set number.

        while (sortedCount < population.Count)
        {
            List<Day> nonDominatedSet = new();
            foreach (Day day in days)
            {
                if (IsNonDominated(day, days))
                {
                    nonDominatedSet.Add(day);
                    sortedCount++;
                }
            }

            popHierarchy.Add(nonDominatedSet);
            foreach (Day day in nonDominatedSet)
            {
                days.Remove(day);
            }
        }

        return popHierarchy;
    }


    public static bool IsNonDominated(Day day, List<Day> population)
    {
        foreach (Day other in population)
        {
            if (day == other) continue;

            switch (Day.SimpleCompare(day, other))
            {
                // Only case where this day is NOT non-dominated.
                case ParetoComparison.Dominated:
                    return false;
            }
        }

        return true;
    }
}