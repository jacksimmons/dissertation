using System;
using System.Collections.Generic;
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
    private const float MutationMassChangeMin = 0f;
    private const float MutationMassChangeMax = 20f;

    // Chances, as a probability (0 to 1)
    private const float ChanceToMutatePortion = 0.5f;
    private const float ChanceToAddOrRemovePortion = 0.01f;


    protected override void RunIteration()
    {
        // Selection
        Tuple<Day, Day> bestTwo = Selection();

        if (bestTwo.Item1 == null || bestTwo.Item2 == null)
            throw new InvalidOperationException("No elements remain in the population.");

        // Crossover
        Tuple<Day, Day> children = Crossover(bestTwo);

        // Mutation
        MutateDay(children.Item1);
        MutateDay(children.Item2);

        // Integration
        Tuple<Day, Day> worstTwo = Selection(false);
        m_population.Remove(worstTwo.Item1);
        m_population.Remove(worstTwo.Item2);
        m_population.Add(children.Item1, GetFitness(children.Item1));
        m_population.Add(children.Item2, GetFitness(children.Item2));

        NumIterations++;
    }


    /// <summary>
    /// Selects the best (or worst) two days in the population provided.
    /// </summary>
    /// <param name="days">The population to select from.</param>
    /// <param name="selectBest">`true` if selecting the best two, `false` if selecting the worst two.</param>
    /// <returns></returns>
    protected Tuple<Day, Day> Selection(bool selectBest = true)
    {
        Day selectedDay = null;
        Day secondSelectedDay = null;

        float selectedFitness = selectBest ? float.PositiveInfinity : 0;
        float secondSelectedFitness = selectBest ? float.PositiveInfinity : 0;

        // Find the best day in the list
        foreach (Day day in m_population.Keys)
        {
            float fitness = m_population[day].Value;

            // If the day's fitness is the best fitness for our goals
            if (selectBest && (fitness < selectedFitness) || !selectBest && (fitness > selectedFitness))
            {
                // The old selected fitness is still better (or as good) for our goals as 2nd place
                secondSelectedFitness = selectedFitness;
                secondSelectedDay = selectedDay;

                // Replace the best (for our goals) fitness
                selectedDay = day;
                selectedFitness = fitness;
            }

            // If the day's fitness is better for our goals than 2nd place
            else if (selectBest && (fitness < secondSelectedFitness) || !selectBest && (fitness > secondSelectedFitness))
            {
                secondSelectedDay = day;
                secondSelectedFitness = fitness;
            }
        }

        return new(selectedDay, secondSelectedDay);
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

        float massSum = 0;
        float massGrandTotal = 
            CalculateMassTotal(parents.Item1) + CalculateMassTotal(parents.Item2);
        float cutoffMass = massGrandTotal * crossoverPoint;

        // List to store every portion for iteration
        List<Portion> allPortions = new(parents.Item1.Portions);
        allPortions.AddRange(parents.Item2.Portions);

        Day left = new();
        Day right = new();

        int cutoffPortionIndex = -1;

        for (int i = 0; i < allPortions.Count; i++)
        {
            float mass = allPortions[i].Mass;
            if (massSum + mass < cutoffMass)
            {
                massSum += mass;

                // Move a portion over to the left child
                left.AddPortion(allPortions[i]);
                continue;
            }

            // If the new mass would make the massSum >= the cutoffMass,
            // then identify the cutoff index, add all remaining portions
            // to the right child, and split the cutoff portion at its local
            // cutoff point.

            // Define the cutoff portion when it is reached (only once)
            if (cutoffPortionIndex == -1)
                cutoffPortionIndex = i;

            // Add all remaining portions to the right child
            right.AddPortion(allPortions[i]);
        }


        if (cutoffPortionIndex == -1)
        {
            // If the cutoff index was never defined, then the cutoffMass was never reached
            // i.e. cutoffMass > total mass of both parents
            throw new ArgumentOutOfRangeException("Selected cutoff mass > sum of all masses in both parents.");
        }

        float localCutoffPoint = cutoffMass - massSum; // The remaining cutoff after last whole portion
        Tuple<Portion, Portion> split = SplitPortion(allPortions[cutoffPortionIndex], localCutoffPoint);

        left.AddPortion(split.Item1);
        right.AddPortion(split.Item2);

        // Construct a tuple of two child Days from the split portions.
        return new(left, right);
    }


    private float CalculateMassTotal(Day day)
    {
        float sum = 0;
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
    /// <param name="cutoffPoint">The local cutoff point - amount of mass
    /// which goes into the left portion. The rest of the portion goes to
    /// the right portion.</param>
    /// <returns>Two new portions, split from the original.</returns>
    private Tuple<Portion, Portion> SplitPortion(Portion portion, float cutoffPoint)
    {
        Portion left = new(portion.Food, cutoffPoint);
        Portion right = new(portion.Food, portion.Mass - cutoffPoint);

        if (cutoffPoint < 0 || portion.Mass - cutoffPoint < 0)
        {
            Debug.Log($"Mass: {portion.Mass}, cutoff pt: {cutoffPoint}");
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
        // Mutate existing portions (add/remove mass)
        for (int i = 0; i < day.Portions.Count; i++)
        {
            bool portionRemains = MutatePortion(day.Portions[i]);
            if (!portionRemains)
            {
                day.RemovePortion(day.Portions[i]);
                i--; // The following items move down one index
            }
        }

        // Add or remove portions entirely (rarely)
        // Only if the day has more than 0 portions.
        if (day.Portions.Count == 0) return;

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
        float sign = Random.Range(0f, 1f) < ChanceToMutatePortion ? 1 : -1;
        float massDiff = Random.Range(MutationMassChangeMin, MutationMassChangeMax);

        portion.Mass += sign * massDiff;

        // If the mass is zero or negative, the portion ceases to exist.
        if (portion.Mass <= 0) return false;
        return true;
    }
}