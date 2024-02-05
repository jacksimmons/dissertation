using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;


// 32-Bit Bit Field.

// Saves on memory usage for boolean values
// bool = 1 byte, but T/F can be stored in 1 bit.
// + BitField can store data 8x more memory efficiently.
// - Accessing a single value is more computationally expensive.
// + But performing bitwise operations negates this issue.
//public class BitField8
//{
//    // 8 bit integer - 8 fields
//    // Undefined behaviour if !(0 <= index <= 7)
//    public byte Data { get; private set; }

//    // Set every bit to 0 by default
//    public BitField8(byte data = 0) { Data = data; }

//    public void SetBit(int index, bool value)
//    {
//        byte mask = (byte)(1 << index);
//        Data = (byte)(value ? (Data | mask) : (Data & ~mask));
//    }

//    public bool GetBit(int index)
//    {
//        int mask = 1 << index;
//        return (Data & mask) != 0;
//    }
//}


public class GeneticAlgorithm : Algorithm
{
    private const int MutationMassChangeMin = 0;
    private const int MutationMassChangeMax = 10;

    // Chances, as a probability (0 to 1)
    private const float ChanceToMutatePortion = 0.5f;
    private const float ChanceToAddOrRemovePortion = 0.001f;


    protected override void RunIteration()
    {
        // Selection
        List<Day> candidates = Population.Keys.ToList();

        Day selectedDayA = Selection(candidates);
        candidates.Remove(selectedDayA);
        Day selectedDayB = Selection(candidates);
        candidates.Remove(selectedDayB);

        // Crossover
        Tuple<Day, Day> children = Crossover(new(selectedDayA, selectedDayB));

        // Mutation
        MutateDay(children.Item1);
        MutateDay(children.Item2);

        // Integration
        Day worstDayA = Selection(candidates, false);
        candidates.Remove(worstDayA);
        Day worstDayB = Selection(candidates, false);

        // The only fitness changes occur here
        Population.Remove(worstDayA);
        Population.Remove(worstDayB);
        Population.Add(children.Item1, children.Item1.GetFitness());
        Population.Add(children.Item2, children.Item2.GetFitness());

        NumIterations++;
    }


    /// <summary>
    /// Selects the best day out of a random duel.
    /// </summary>
    protected Day Selection(List<Day> candidates, bool selectBest = true)
    {
        int indexA = Random.Range(0, candidates.Count);
        // Ensure B is different to A by adding an amount less than the list size, then %-ing it.
        int indexB = (indexA + Random.Range(0, candidates.Count - 1)) % candidates.Count;

        switch (Day.Compare(candidates[indexA], candidates[indexB]))
        {
            case ParetoComparison.Dominates:
                return selectBest ? candidates[indexA] : candidates[indexB];
            case ParetoComparison.Dominated:
                return selectBest ? candidates[indexB] : candidates[indexA];
        }
        return SelectionTiebreak(candidates[indexA], candidates[indexB]);
    }


    protected Day SelectionTiebreak(Day a, Day b)
    {
        if (Random.Range(0, 2) == 0)
            return a;
        return b;
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
    private Tuple<Day, Day> Crossover(Tuple<Day, Day> parents)
    {
        float crossoverPoint = Random.Range(0f, 1f);

        int massSum = 0;
        int massGrandTotal = 
            CalculateMassTotal(parents.Item1) + CalculateMassTotal(parents.Item2);

        float floatCutoffMass = massGrandTotal * crossoverPoint;
        int cutoffMass = (int)(floatCutoffMass);

        // Remove LB edge case, where left gets all portions and right gets none
        if (cutoffMass == 0)
            cutoffMass++;
        // Remove UB edge case (need to confirm if this is possible)
        else if (cutoffMass == massGrandTotal)
            cutoffMass--;


        // List to store every portion for iteration
        List<Portion> allPortions = new(parents.Item1.Portions);
        allPortions.AddRange(parents.Item2.Portions);

        Day left = new();
        Day right = new();

        int splitPortionIndex = -1;
        for (int i = 0; i < allPortions.Count; i++)
        {
            int mass = allPortions[i].Mass;

            // Add any portions that don't exceed the cutoff to the left child.
            if (massSum + mass <= cutoffMass)
            {
                massSum += mass;

                // Move a portion over to the left child
                left.AddPortion(allPortions[i]);
            }

            // Once exceeded the cutoff, identify the portion to be split.
            else if (splitPortionIndex == -1)
            {
                splitPortionIndex = i;
            }

            // Add every remaining portion to the right child.
            else
            {
                right.AddPortion(allPortions[i]);
            }
        }

        if (splitPortionIndex == -1)
        {
            // If the cutoff index was never defined, then the cutoffMass was never reached
            // i.e. cutoffMass > total mass of both parents
            Debug.LogError("No portion was set to be split.");
        }

        //
        // Sub-portion splitting (split a portion into two smaller portions according to the local cutoff point
        //
        int localCutoffMass = cutoffMass - massSum; // The remaining cutoff after last whole portion

        if (localCutoffMass != 0)
        {
            Tuple<Portion, Portion> split = SplitPortion(allPortions[splitPortionIndex], localCutoffMass);

            left.AddPortion(split.Item1);
            right.AddPortion(split.Item2);
        }
        // If the local cutoff point is 0, then no sub-portion splitting needs to be done. We just need to add the
        // portion to the right child.
        else
        {
            right.AddPortion(allPortions[splitPortionIndex]);
        }

        // Construct a tuple of two child Days from the portion split.
        return new(left, right);
    }


    private int CalculateMassTotal(Day day)
    {
        int sum = 0;
        for (int i = 0; i < day.Portions.Count; i++)
        {
            sum += day.Portions[i].Mass;
        }
        return sum;
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
            Debug.Log($"Mass: {portion.Mass}, cutoff pt: {cutoffMass}");
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
            Debug.LogError("Day has no portions");
            return; 
        } 

        // Mutate existing portions (add/remove mass)
        foreach (Portion portion in day.Portions.ToArray())
        {
            // Exit early if the portion is not to be mutated
            if (Random.Range(0f, 1f) > ChanceToMutatePortion)
                continue;

            bool portionRemains = MutatePortion(portion);
            if (!portionRemains)
                day.RemovePortion(portion);
        }

        // Add or remove portions entirely (rarely)
        bool addPortion = Random.Range(0f, 1f) < ChanceToAddOrRemovePortion;
        bool removePortion = Random.Range(0f, 1f) < ChanceToAddOrRemovePortion;

        if (addPortion)
            day.AddPortion(GenerateRandomPortion());

        if (removePortion)
            day.RemovePortion(day.Portions[Random.Range(0, day.Portions.Count)]);
    }


    /// <summary>
    /// Applies mutation to a single Portion object.
    /// </summary>
    /// <param name="portion">The portion to mutate.</param>
    /// <returns>A boolean of whether the portion remains.
    /// True => Leave the portion, False => Delete the portion.</returns>
    private bool MutatePortion(Portion portion)
    {
        // The sign of the mass change (1 => add, -1 => subtract)
        int sign = Random.Range(0, 2) == 1 ? 1 : -1;
        int massDiff = Random.Range(MutationMassChangeMin, MutationMassChangeMax);

        // If the new mass is zero or negative, the portion ceases to exist.
        if (portion.Mass + sign * massDiff <= 0)
            return false;

        // Otherwise, add to the portion's mass.
        portion.Mass += sign * massDiff;
        return true;
    }
}