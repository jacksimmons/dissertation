using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;

public static class Pareto
{
    public static Dictionary<Day, uint> GetDominanceHierarchy(ReadOnlyCollection<Day> population)
    {
        if (population.Count < 1) throw new IndexOutOfRangeException("Population was empty.");

        List<Day> days = new(population);
        Dictionary<Day, uint> popHierarchy = new();


        // Init:   Set `days` equal to `population`.
        // Repeat: Find every element in `days` that is not dominated by the elements in `days`.
        //         Once iteration completes, remove these from `days` and continue. Take a note of the
        //         set number.

        uint setNumber = 0;
        while (popHierarchy.Count < population.Count)
        {
            List<Day> nonDominatedSet = new();
            foreach (Day day in days)
            {
                if (IsNonDominated(day, days))
                {
                    nonDominatedSet.Add(day);
                }
            }

            foreach (Day day in nonDominatedSet)
            {
                popHierarchy[day] = setNumber;
                days.Remove(day);
            }

            setNumber++;
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