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
    private const float ChanceToMutatePortion = 0.5f;
    
    // Impacts determinism. Low value => High determinism, High value => Random chaos
    // 0 or 1 -> No convergence
    private const float ChanceToAddOrRemovePortion = 0.01f;


    protected override void RunIteration()
    {
        // Selection
        List<Day> candidates = new(Population);

        Day selectedDayA = Selection(candidates);
        candidates.Remove(selectedDayA);
        Day selectedDayB = Selection(candidates);
        candidates.Remove(selectedDayB);


        // Crossover
        Tuple<Day, Day> children = Crossover(new(selectedDayA, selectedDayB));

        // Mutation
        MutateDay(children.Item1);
        MutateDay(children.Item2);

        // Next generation - The only fitness changes occur here
        // Add children
        AddToPopulation(children.Item1);
        AddToPopulation(children.Item2);
        candidates.Add(children.Item1);
        candidates.Add(children.Item2);

        Day worstDayA = Selection(candidates, false);
        candidates.Remove(worstDayA);
        Day worstDayB = Selection(candidates, false);

        // Slight elitism - children can be killed off as soon as they are added if they are bad
        RemoveFromPopulation(worstDayA);
        RemoveFromPopulation(worstDayB);
    }


    protected abstract Day Selection(List<Day> candidates, bool selectBest = true);


    protected abstract Day SelectionTiebreak(Day a, Day b);


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
        int massSum = 0;
        int cutoffMass = GetCutoffMass(parents);

        // List to store every portion for iteration
        List<Portion> allPortions = new(parents.Item1.Portions);
        allPortions.AddRange(parents.Item2.Portions);

        Tuple<Day, Day> children = new(new(), new());
        int splitPortionIndex = -1;
        
        //
        // Loop to crossover portions between left and right children.
        //
        for (int i = 0; i < allPortions.Count; i++)
        {
            int mass = allPortions[i].Mass;

            // Add any portions that don't exceed the cutoff to the left child.
            if (massSum + mass <= cutoffMass)
            {
                massSum += mass;

                // Move a portion over to the left child
                children.Item1.AddPortion(allPortions[i]);
            }

            // Once exceeded the cutoff, identify the portion to be split.
            else if (splitPortionIndex == -1)
            {
                splitPortionIndex = i;
            }

            // Add every remaining portion to the right child.
            else
            {
                children.Item2.AddPortion(allPortions[i]);
            }
        }

        if (splitPortionIndex == -1)
        {
            // If the cutoff index was never defined, then the cutoffMass was never reached
            // i.e. cutoffMass > total mass of both parents
            Static.Log("No portion was set to be split.", Severity.Log);
        }

        HandleCutoffPortion(children, cutoffMass - massSum, allPortions[splitPortionIndex]);
        return children;
    }


    /// <summary>
    /// Generates a cutoff mass value, from a random proportion of 0 to 1.
    /// </summary>
    /// <param name="parents">The parents to crossover from.</param>
    /// <returns>The mass where crossover applies to the genetic material.</returns>
    protected int GetCutoffMass(Tuple<Day, Day> parents)
    {
        // A random split between the two parents
        float crossoverPoint = (float)m_rand.NextDouble();

        // Total mass stored in both days
        int massGrandTotal = parents.Item1.GetMass() + parents.Item2.GetMass();

        float floatCutoffMass = massGrandTotal * crossoverPoint;
        int cutoffMass = (int)(floatCutoffMass);

        // Remove LB edge case, where left gets all portions and right gets none
        if (cutoffMass == 0)
            cutoffMass++;
        // Remove UB edge case (need to confirm if this is possible)
        else if (cutoffMass == massGrandTotal)
            cutoffMass--;

        return cutoffMass;
    }


    /// <summary>
    /// Handles splitting of the middle (cutoff) portion in the portion list.
    /// Generally splits the portion at the cutoff mass provided, giving the "left" side to the left
    /// child and the rest to the right child.
    /// </summary>
    /// <param name="localCutoffMass">The remaining cutoff after last whole portion</param>
    protected void HandleCutoffPortion(Tuple<Day, Day> children, int localCutoffMass, Portion portion)
    {
        if (localCutoffMass != 0)
        {
            Tuple<Portion, Portion> split = SplitPortion(portion, localCutoffMass);

            children.Item1.AddPortion(split.Item1);
            children.Item2.AddPortion(split.Item2);
        }

        // If the local cutoff point is 0, then the right child takes 100% of the portion.
        // Note: The opposite case for the left child is already handled in the portion split for loop,
        // so unlike this case, it will not lead to empty portions.
        else
        {
            children.Item2.AddPortion(portion);
        }
    }


    /// <summary>
    /// Splits a portion into two, at a given cutoff point in grams.
    /// </summary>
    /// <param name="portion">The portion to split.</param>
    /// <param name="cutoffMass">The local cutoff point - amount of mass
    /// which goes into the left portion. The rest of the portion goes to
    /// the right portion.</param>
    /// <returns>Two new portions, split from the original.</returns>
    private Tuple<Portion, Portion> SplitPortion(Portion portion, int cutoffMass)
    {
        Portion left = new(portion.Food, cutoffMass);
        Portion right = new(portion.Food, portion.Mass - cutoffMass);

        if (cutoffMass < 0 || portion.Mass - cutoffMass < 0)
        {
            Static.Log($"Mass: {portion.Mass}, cutoff pt: {cutoffMass}", Severity.Warning);
        }

        return new(left, right);
    }


    /// <summary>
    /// Handles mutation for a single Day object, and delegates to MutatePortion
    /// for all Portions in its list.
    /// </summary>
    /// <param name="day">The day to mutate.</param>
    private void MutateDay(Day day)
    {
        // Only mutate if day has more than 0 portions.
        if (day.Portions.Count == 0)
        { 
            Static.Log("Day has no portions", Severity.Error);
            return; 
        } 

        // Mutate existing portions (add/remove mass)
        for (int i = 0; i < day.Portions.Count; i++)
        {
            // Exit early if the portion is not to be mutated
            if (m_rand.NextDouble() > ChanceToMutatePortion / day.Portions.Count)
                continue;

            Tuple<bool, int> result = MutatePortion(day.Portions[i]);
            if (!result.Item1)
                day.RemovePortion(i);
            else
                day.SetPortionMass(i, result.Item2);
        }

        // Add or remove portions entirely (rarely)
        bool addPortion = m_rand.NextDouble() < ChanceToAddOrRemovePortion;
        bool removePortion = m_rand.NextDouble() < ChanceToAddOrRemovePortion;

        if (addPortion)
            day.AddPortion(GenerateRandomPortion());

        if (removePortion)
            day.RemovePortion(m_rand.Next(day.Portions.Count));
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
        int sign = m_rand.Next(2) == 1 ? 1 : -1;
        int mass = portion.Mass;
        int massDiff = m_rand.Next(MutationMassChangeMin, MutationMassChangeMax);

        // If the new mass is zero or negative, the portion ceases to exist.
        if (mass + sign * massDiff <= 0)
            return new(false, mass);

        // Otherwise, add to the portion's mass.
        mass += sign * massDiff;
        return new(true, mass);
    }
}