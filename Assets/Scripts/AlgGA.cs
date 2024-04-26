// Commented 7/4
using System;
using System.Collections.Generic;


/// <summary>
/// A genetic algorithm, which has multiple selection methods and tunable parameters. (See Preferences class)
/// </summary>
public partial class AlgGA : Algorithm
{
    /// <summary>
    /// The selection method used by the GA, as a function reference.
    /// </summary>
    private Func<List<Day>, bool, Day> m_selectionMethod;

    /// <summary>
    /// Tournament size of the GA (if Tournament Selection in use). Minimum value is 1, maximum is the entire population.
    /// </summary>
    protected int m_tournamentSize = (int)MathF.Max(1, Prefs.populationSize * Prefs.selectionPressure);

    /// <summary>
    /// A sorted list of mutually non-dominated sets for all days in the population (if ParetoDominance in use).
    /// </summary>
    public readonly ParetoHierarchy Hierarchy = new();


    public override string Init()
    {
		// ----- Preference error checking -----
		string errorText = base.Init();
		if (errorText != "") return errorText; // Parent initialisation, and ensure no errors have occurred.
		
        if (Prefs.mutationMassChangeMin > Prefs.mutationMassChangeMax)
            errorText = $"Invalid parameters: mutationMassChangeMin ({Prefs.mutationMassChangeMin}) must be less than"
                + $" or equal to mutationMassChangeMax ({Prefs.mutationMassChangeMax})";

        if (Prefs.changePortionMassMutationProb > 1)
            errorText = $"Invalid parameter: changePortionMassMutationProb ({Prefs.changePortionMassMutationProb}) must be in the range [0,1]";

        if (Prefs.addOrRemovePortionMutationProb > 1)
            errorText = $"Invalid parameter: addOrRemovePortionMutationProb ({Prefs.addOrRemovePortionMutationProb}) must be in the range [0,1]";

        if (Prefs.selectionPressure > 1)
            errorText = $"Invalid parameter: selectionPressure ({Prefs.selectionPressure}) must be in the range [0,1]";

        if (Prefs.numCrossoverPoints < 1)
            errorText = $"Invalid parameter: numCrossoverPoints ({Prefs.numCrossoverPoints}) must be greater than 0.";

        if (errorText != "")
        {
            return errorText;
        }
		// ----- END -----

        LoadStartingPopulation();

        // Initialise selection function
        switch (Prefs.selectionMethod)
        {
            case ESelectionMethod.Tournament:
                m_selectionMethod = TournamentSelection;
                break;
            case ESelectionMethod.Rank:
                m_selectionMethod = RankSelection;
                break;
            default:
                return "No valid selection method was picked.";
        }

        return "";
    }


    protected override void AddToPopulation(Day day)
    {
        base.AddToPopulation(day);
        if (Prefs.fitnessApproach == EFitnessApproach.ParetoDominance)
        {
            Hierarchy.Add((Day.ParetoFitness)day.TotalFitness);
        }
    }


    protected override void RemoveFromPopulation(Day day)
    {
        base.RemoveFromPopulation(day);
        if (Prefs.fitnessApproach == EFitnessApproach.ParetoDominance)
        {
            Hierarchy.Remove((Day.ParetoFitness)day.TotalFitness);
        }
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


    protected override void NextIteration()
    {
        if (Prefs.populationSize == 1) return;

        // --- Elitism + Reproduction ---
        // Iterate over the number of demanded parents
        // MODEL: Each parent pair produces a pair of children.
        List<Day> included = new(Population);

        // Need to sort the list for Rank selection
        if (m_selectionMethod == RankSelection)
        {
            included.Sort();
        }

        Tuple<Day, Day> parents = PairSelection(included, true);
        Tuple<Day, Day> children = Reproduction(parents);

        // --- Elimination ---
        // For each parent, kill off 1 candidate. (Reproduction - Elimination) === 0
        included = new(Population); // Any day can get eliminated, even the children

        // Need to sort the list for Rank selection
        if (m_selectionMethod == RankSelection)
        {
            // Sorts the list based on the Day IComparer implementation.
            included.Sort();
            included.Reverse();
        }

        List<Day> allDead = new();
        for (int i = 0; i < 2; i++)
        {
            // Select and eliminate one day per parent (to keep population size stable)
            Day selectedDay = Selection(included, false);
            included.Remove(selectedDay);

            // Remove the selected day from the population
            allDead.Add(selectedDay);
        }


        // --- Population Update ---
        AddToPopulation(children.Item1);
        AddToPopulation(children.Item2);

        for (int i = 0; i < allDead.Count; i++)
        {
            RemoveFromPopulation(allDead[i]);
        }
    }


    /// <summary>
    /// Handles crossover and mutation.
    /// </summary>
    /// <returns>The two children.</returns>
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
    /// Selects a pair of days from the population.
    /// </summary>
    /// <param name="included">The days that can be selected.</param>
    /// <param name="selectBest">`true` if selecting the best, `false` if selecting the worst.</param>
    /// <returns>The selected pair of days.</returns>
    public Tuple<Day, Day> PairSelection(List<Day> included, bool selectBest)
    {
        Day selectedDayA = Selection(included, selectBest);
        included.Remove(selectedDayA);
        Day selectedDayB = Selection(included, selectBest);
        included.Remove(selectedDayB);

        return new(selectedDayA, selectedDayB);
    }


    public Day Selection(List<Day> included, bool selectBest)
    {
        if (included.Count == 0) Logger.Warn("Cannot select a Day from an included list of length 0.");

        return m_selectionMethod(included, selectBest);
    }


    /// <summary>
    /// Returns the "first" best day in a list of randomly picked days, based on a tournament selection process.
    /// ("first" meaning if two have identical fitness, the first in the list gets returned)
    /// </summary>
    /// <param name="included">The list of days to select from.</param>
    /// <param name="selectBest">`true` => Select the lowest fitness. `false` => Select the highest fitness.</param>
    public Day TournamentSelection(List<Day> included, bool selectBest)
    {
        if (included.Count == 1) return included[0];
        int tournamentSize = Math.Min(m_tournamentSize, included.Count);

        List<Day> days = new(included);
        Day bestDay = null;

        // Repeat (tournamentSize) times:
        //  Select random individual, if its fitness < bestFitness
        //  Then update bestFitness and set it as the best day.
        // End Repeat
        // Return best da
        for (int i = 0; i < tournamentSize; i++)
        {
            int index = Rand.Next(days.Count);
            Day day = days[index];
            days.RemoveAt(index);

            if (bestDay == null || selectBest && day < bestDay || !selectBest && day > bestDay)
                bestDay = day;
        }

        return bestDay;
    }


    /// <summary>
    /// Returns a selected day from a sorted list of days; the lower the sort index, the higher the chance of selection.
    /// </summary>
    /// <param name="sortedIncluded">The sorted list of days to select from.</param>
    /// <param name="selectBest">`true` => Select the lowest fitness. `false` => Select the highest fitness.</param>
    public Day RankSelection(List<Day> sortedIncluded, bool selectBest)
    {
        if (sortedIncluded.Count == 1) return sortedIncluded[0];

        // Number of elements to select from
        int n = sortedIncluded.Count;
        float[] rankProbs = new float[n];

        // Get probability of each rank getting selected
        float bakerSP = Prefs.selectionPressure + 1;
        for (int i = 0; i < n; i++)
        {
            rankProbs[i] = (1.0f / n) * (bakerSP - (2 * bakerSP - 2) * i / (n - 1.0f));
        }

        // Select a rank with weighted random. This rank will correspond to an element in both population and `included`.
        int selectedRank = MathTools.GetFirstSurpassedProbability(rankProbs, Rand);

        return sortedIncluded[selectedRank];
    }


    /// <summary>
    /// Handles mutation for a single Day object, and delegates to MutatePortion
    /// for all portions in its list.
    /// </summary>
    /// <param name="day">The day to mutate.</param>
    public void MutateDay(Day day)
    {
        // Mutate existing portions (add/remove mass)
        for (int i = 0; i < day.portions.Count; i++)
        {
            // Exit early if the portion is not to be mutated
            if ((float)Rand.NextDouble() > Prefs.changePortionMassMutationProb / day.portions.Count)
                continue;

            Tuple<bool, int> result = MutatePortion(day.portions[i]);

            // If the portion isn't to remain (the mutation led to a negative or 0 mass), remove it.
            // Otherwise, set the new mutated mass.
            if (!result.Item1)
                day.RemovePortion(i);
            else
                day.SetPortionMass(i, result.Item2);
        }

        // Add and/or remove portions entirely as a mutation
        bool addPortion = (float)Rand.NextDouble() < Prefs.addOrRemovePortionMutationProb;
        bool removePortion = ((float)Rand.NextDouble() < Prefs.addOrRemovePortionMutationProb) && day.portions.Count > 0;

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

        // Otherwise, add to the portion's mass
        mass += sign * massDiff;
        return new(true, mass);
    }
}