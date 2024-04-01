using System;
using System.Collections.Generic;


/// <summary>
/// A genetic algorithm, which has multiple selection methods and tunable parameters. (See Preferences class)
/// </summary>
public partial class AlgGA : Algorithm
{
    protected int m_numParents = (int)(Prefs.populationSize * (Prefs.proportionParents / 2)) * 2; // Cannot be odd
    protected int m_tournamentSize = Math.Min((int)(Prefs.populationSize * 0.2f), 2);

    private Func<List<Day>, bool, Day> m_selectionMethod;


    public override bool Init()
    {
        if (!base.Init()) return false;


        LoadStartingPopulation();


        // Handle potential errors
        string errorText = "";

        if (Prefs.mutationMassChangeMin < 0 || Prefs.mutationMassChangeMin > Prefs.mutationMassChangeMax)
            errorText = "Invalid parameter: mutation min < 0 or mutation min > mutation max.";

        if (Prefs.mutationMassChangeMax < 0 || Prefs.mutationMassChangeMin > Prefs.mutationMassChangeMax)
            errorText = "Invalid parameter: mutation max < 0 or mutation max < mutation min.";

        if (Prefs.changePortionMassMutationProb < 0 || Prefs.addOrRemovePortionMutationProb < 0)
            errorText = "Invalid parameter: mutation probability was < 0.";

        if (m_numParents <= 0)
            errorText = "Invalid parameter: numparents was <= 0.";

        if (errorText != "")
        {
            Logger.Warn(errorText);
            return false;
        }

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
                Logger.Warn("No valid selection method was selected.");
                return false;
        }

        return true;
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
        // --- Elitism + Reproduction ---
        // Iterate over the number of demanded parents
        // MODEL: Each parent pair produces a pair of children.
        List<Day> included = new(Population);

        // Need to sort the list for Rank selection
        if (m_selectionMethod == RankSelection)
        {
            // ! m_population.SortDayList(included);
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
        included = new(Population); // Any day can get eliminated, even the children

        // Need to sort the list for Rank selection
        if (m_selectionMethod == RankSelection)
        {
            // ! m_population.SortDayList(included, true);
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
    /// Handles mutation for a single Day object, and delegates to MutatePortion
    /// for all portions in its list.
    /// </summary>
    /// <param name="day">The day to mutate.</param>
    private void MutateDay(Day day)
    {
        // Only mutate if day has more than 0 portions.
        if (day.portions.Count == 0)
            Logger.Error("Day has no portions");

        // Mutate existing portions (add/remove mass)
        for (int i = 0; i < day.portions.Count; i++)
        {
            // Exit early if the portion is not to be mutated
            if ((float)Rand.NextDouble() > Prefs.changePortionMassMutationProb / day.portions.Count)
                continue;

            Tuple<bool, int> result = MutatePortion(day.portions[i]);
            if (!result.Item1)
                day.RemovePortion(i);
            else
                day.SetPortionMass(i, result.Item2);
        }

        // Add or remove portions entirely (rarely)
        bool addPortion = (float)Rand.NextDouble() < Prefs.addOrRemovePortionMutationProb;
        bool removePortion = (float)Rand.NextDouble() < Prefs.addOrRemovePortionMutationProb;

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
        if (included.Count == 0)
        {
            Logger.Error("Included cannot be empty when performing selection.");
            return null;
        }
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

        for (int i = 0; i < tournamentSize; i++)
        {
            int index = Rand.Next(days.Count);
            Day day = days[index];
            days.RemoveAt(index);

            if (bestDay == null || selectBest && day < bestDay || !selectBest && day > bestDay)
                bestDay = day;
        }

        // Guaranteed to not be null; if they all have infinite fitness, the first picked day will be returned.
        return bestDay!;
    }


    public Day RankSelection(List<Day> sortedIncluded, bool selectBest)
    {
        // Number of elements to select from
        int n = sortedIncluded.Count;
        float[] rankProbs = new float[n];

        // Get probability of each rank getting selected
        for (int i = 0; i < rankProbs.Length; i++)
        {
            rankProbs[i] = (1.0f / n) * (Prefs.selectionPressure - (2 * Prefs.selectionPressure - 2) * (i - 1.0f) / (n - 1.0f));
        }

        // Select a rank with weighted random. This rank will correspond to an element in both population and `included`.
        int selectedRank = MathTools.GetFirstSurpassedProbability(rankProbs);
        return sortedIncluded[selectedRank];
    }


    public Day RandomSelection(List<Day> included, bool selectBest)
    {
        return included[Rand.Next(included.Count)];
    }
}