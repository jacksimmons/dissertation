using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


public abstract class AlgGA : Algorithm
{
    protected int m_numParents = (int)(Prefs.populationSize * (Prefs.proportionParents / 2)) * 2; // Cannot be odd
    protected int m_tournamentSize = Math.Min((int)(Prefs.populationSize * 0.2f), 2);


    public override void Init()
    {
        LoadStartingPopulation();


        // Handle erroneous properties
        if (Prefs.mutationMassChangeMin < 0 || Prefs.mutationMassChangeMin > Prefs.mutationMassChangeMax)
            Logger.Error("Invalid parameter: mutation min < 0 or mutation min > mutation max.");

        if (Prefs.mutationMassChangeMax < 0 || Prefs.mutationMassChangeMin > Prefs.mutationMassChangeMax)
            Logger.Error("Invalid parameter: mutation max < 0 or mutation max < mutation min.");

        if (Prefs.chanceToMutatePortion < 0)
            Logger.Error("Invalid parameter: mutation chance was < 0.");

        if (m_numParents <= 0)
            Logger.Error("Invalid parameter: numparents was <= 0.");
    }


    /// <summary>
    /// Populates the Population data structure with randomly generated Days.
    /// </summary>
    /// <returns>The created population data structure, WITHOUT evaluated fitnesses.</returns>
    private void LoadStartingPopulation()
    {
        for (int i = 0; i < Prefs.populationSize; i++)
        {
            // Add a number of days to the population (each has random foods)
            Day day = new(this);
            for (int j = 0; j < Prefs.numStartingPortionsPerDay; j++)
            {
                // Add random foods to the day
                day.AddPortion(RandomPortion);
            }

            AddToPopulation(day);
        }
    }


    protected virtual void AddToPopulation(Day day)
    {
        m_population.Add(day);
    }


    protected virtual void RemoveFromPopulation(Day day)
    {
        m_population.Remove(day);
    }


    protected override abstract void NextIteration();


    protected Tuple<Day, Day> Reproduction(Tuple<Day, Day> parents)
    {
        // --- Crossover ---
        Tuple<Day, Day> children = Crossover(parents);

        // --- Mutation ---
        MutateDay(children.Item1);
        MutateDay(children.Item2);

        return children;
    }


    /// <summary>
    /// Applies genetic crossover from parents to children.
    /// This crossover works by turning each Day into fine data, by using the
    /// mass of each portion as a unit of measurement.
    /// 
    /// So it first looks at the proportion of portions entirely included by
    /// the crossover point (in day 1 then day 2), and finds the first portion not fully
    /// included (this can be in day 1 or 2).
    /// 
    /// It then goes down to the mass-level, and splits the portion at the point where total
    /// mass of the Day so far equals (total mass of both days) * crossoverPoint.
    /// 
    /// The rest of the portion, and the other portions after that (including the entirety
    /// of day 2, if the crossover point < 0.5) goes into the other child.
    /// </summary>
    /// <param name="parents">The two parents. Expects non-null Days.</param>
    /// <returns>Two children with only crossover applied.</returns>
    public Tuple<Day, Day> Crossover(Tuple<Day, Day> parents)
    {
        CrossoverRunner runner = new(parents, this);
        Tuple<Day, Day> children = runner.NPointCrossover();
        return children;
    }


    /// <summary>
    /// Handles mutation for a single Day object, and delegates to MutatePortion
    /// for all portions in its list.
    /// </summary>
    /// <param name="day">The day to mutate.</param>
    private void MutateDay(Day day)
    {
        // Only mutate if day has more than 0 portions.
        if (day.portions.Count == 0)
        { 
            Logger.Log("Day has no portions", Severity.Error);
            return;
        }

        // Mutate existing portions (add/remove mass)
        for (int i = 0; i < day.portions.Count; i++)
        {
            // Exit early if the portion is not to be mutated
            if (Rand.NextDouble() > Prefs.chanceToMutatePortion / day.portions.Count)
                continue;

            Tuple<bool, int> result = MutatePortion(day.portions[i]);
            if (!result.Item1)
                day.RemovePortion(i);
            else
                day.SetPortionMass(i, result.Item2);
        }

        // Add or remove portions entirely (rarely)
        bool addPortion = Rand.NextDouble() < Prefs.chanceToAddOrRemovePortion;
        bool removePortion = Rand.NextDouble() < Prefs.chanceToAddOrRemovePortion;

        if (addPortion)
            day.AddPortion(RandomPortion);

        if (removePortion)
            day.RemovePortion(Rand.Next(day.portions.Count));
    }


    /// <summary>
    /// Applies mutation to a single Portion object.
    /// </summary>
    /// <param name="portion">The portion to mutate.</param>
    /// <returns>Tuple of:
    /// Item1:
    /// A boolean of whether the portion remains.
    /// True => Leave the portion, False => Delete the portion.
    /// Item2:
    /// The new mass of the portion.</returns>
    private Tuple<bool, int> MutatePortion(Portion portion)
    {
        // The sign of the mass change (1 => add, -1 => subtract)
        int sign = Rand.Next(2) == 1 ? 1 : -1;
        int mass = portion.Mass;
        int massDiff = Rand.Next(Prefs.mutationMassChangeMin, Prefs.mutationMassChangeMax);

        // If the new mass is zero or negative, the portion ceases to exist.
        if (mass + sign * massDiff <= 0)
            return new(false, mass);

        // Otherwise, add to the portion's mass.
        mass += sign * massDiff;
        return new(true, mass);
    }
}