using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


public abstract class AlgGA : Algorithm
{
    private const int MutationMassChangeMin = 0;
    private const int MutationMassChangeMax = 10;

    // Chance for any portion to be mutated in an algorithm pass.
    // Is divided by the number of portions in the calculation.
    private const float ChanceToMutatePortion = 1f;

    // Impacts determinism. Low value => High determinism, High value => Random chaos
    // 0 or 1 -> No convergence
    private const float ChanceToAddOrRemovePortion = 0.01f;


    public override void Init()
    {
        LoadStartingPopulation();
    }


    /// <summary>
    /// Populates the Population data structure with randomly generated Days.
    /// </summary>
    /// <returns>The created population data structure, WITHOUT evaluated fitnesses.</returns>
    private void LoadStartingPopulation()
    {
        for (int i = 0; i < Preferences.Instance.populationSize; i++)
        {
            // Add a number of days to the population (each has random foods)
            Day day = new();
            for (int j = 0; j < Preferences.Instance.numStartingPortionsPerDay; j++)
            {
                // Add random foods to the day
                day.AddPortion(GenerateRandomPortion());
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


    /// <summary>
    /// Generates a random portion (a random food selected from the dataset, with a random
    /// quantity multiplier).
    /// </summary>
    /// <returns></returns>
    protected Portion GenerateRandomPortion()
    {
        Food food = Foods[Rand.Next(Foods.Count)];
        return new(food, Rand.Next(Preferences.Instance.portionMinStartMass, Preferences.Instance.portionMaxStartMass));
    }


    protected override void NextIteration()
    {
        List<Day> included = new(m_population.Days);

        // --- (Best) Selection ---
        Day selectedDayA = Selection(included);
        // Exclude the first best day, as need to find a second best day.
        included.Remove(selectedDayA);
        Day selectedDayB = Selection(included);
        // Exclude the second best day, to simplify worst-selection later.
        included.Remove(selectedDayB);


        // --- Crossover ---
        Tuple<Day, Day> children = Crossover(new(selectedDayA, selectedDayB));


        // --- Mutation ---
        MutateDay(children.Item1);
        MutateDay(children.Item2);

        // Optimistically add the two children (assuming they are not the worst
        // days in the population, a reasonable assumption)
        AddToPopulation(children.Item1);
        AddToPopulation(children.Item2);


        // --- (Worst) Selection --- i.e. Elitism
        // Search through the days that weren't the best days (excluded list still
        // applies).
        Day worstDayA = Selection(included, false);
        // Exclude the first worst day, as need to find a second worst day.
        included.Remove(worstDayA);
        Day worstDayB = Selection(included, false);

        // Remove the worst two days (these could be the just-added children)
        RemoveFromPopulation(worstDayA);
        RemoveFromPopulation(worstDayB);
    }


    protected abstract Day Selection(List<Day> excluded, bool selectBest = true);


    /// <summary>
    /// Selects two days from an inclusion list. The two days are guaranteed to be different.
    /// Assumes the population size is >= 2.
    /// </summary>
    /// <param name="included">The list of included days.</param>
    /// <returns>The two days selected.</returns>
    protected Tuple<Day, Day> SelectDays(List<Day> included)
    {
        int indexA = Rand.Next(included.Count);
        int indexB = (indexA + Rand.Next(1, included.Count - 1)) % included.Count;

        return new(included[indexA], included[indexB]);
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
    protected Tuple<Day, Day> Crossover(Tuple<Day, Day> parents)
    {
        CrossoverRunner runner = new(parents);
        Tuple<Day, Day> children = runner.NPointCrossover();

        if (children.Item1.portions.Count == 0) Logger.Log("Oops", Severity.Error);
        if (children.Item2.portions.Count == 0) Logger.Log("Oops", Severity.Error);
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
            if (Rand.NextDouble() > ChanceToMutatePortion / day.portions.Count)
                continue;

            Tuple<bool, int> result = MutatePortion(day.portions[i]);
            if (!result.Item1)
                day.RemovePortion(i);
            else
                day.SetPortionMass(i, result.Item2);
        }

        // Add or remove portions entirely (rarely)
        bool addPortion = Rand.NextDouble() < ChanceToAddOrRemovePortion;
        bool removePortion = Rand.NextDouble() < ChanceToAddOrRemovePortion;

        if (addPortion)
            day.AddPortion(GenerateRandomPortion());

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
        int massDiff = Rand.Next(MutationMassChangeMin, MutationMassChangeMax);

        // If the new mass is zero or negative, the portion ceases to exist.
        if (mass + sign * massDiff <= 0)
            return new(false, mass);

        // Otherwise, add to the portion's mass.
        mass += sign * massDiff;
        return new(true, mass);
    }
}