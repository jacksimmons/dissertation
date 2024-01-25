using System;
using System.Collections.Generic;
using UnityEngine;
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
    private float MutationMassChangeMin = 0f;
    private float MutationMassChangeMax = 20f;

    // Chances, as a probability (0 to 1)
    private float ChanceToMutatePortion = 0.5f;
    private float ChanceToAddOrRemovePortion = 0.01f;


    public override void Run()
    {
        // Generate population and constraints
        m_population = GetStartingPopulation();
        Debug.Log(DayListToString(m_population));

        while (true)
        {
            MainLoop();
        }
    }


    private void MainLoop()
    {
        // Selection
        Tuple<Day, Day> selected = Selection(m_population);

        for (int i = 0; i < m_population.Count; i++)
        {
            Fitness fitness = GetFitness(m_population[i]);
            Debug.Log($"Fitness: {fitness.Value}");
        }

        // Crossover
        Tuple<Day, Day> children = Crossover(selected);

        // Mutation
        MutateDay(children.Item1);
        MutateDay(children.Item2);

        // Integration
    }


    protected Tuple<Day, Day> Selection(List<Day> days)
    {
        float lowestFitness = float.PositiveInfinity;
        float secondLowestFitness = float.PositiveInfinity;
        Day fittestDay = null;
        Day secondFittestDay = null;

        foreach (Day day in days)
        {
            float dayFitness = GetFitness(day).Value;

            // Only allow a day to get one of 1st and 2nd, not both
            if (dayFitness < lowestFitness)
            {
                fittestDay = day;
                lowestFitness = dayFitness;
            }
            else if (dayFitness < secondLowestFitness)
            {
                secondFittestDay = day;
                secondLowestFitness = dayFitness;
            }
        }

        return new(fittestDay, secondFittestDay);
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
    /// It then goes down to the mass-level, and splits the portion at the first gram
    /// where total mass of the Day so far equals (total mass of both days) * crossoverPoint.
    /// This split mass will then be calculated into a Quantity parameter for each of the two
    /// new portions.
    /// 
    /// The rest of the portion, and the other portions after that (including the entirety
    /// of day 2, if the crossover point < 0.5) goes into the other child.
    /// </summary>
    /// <param name="parents">The two parents.</param>
    /// <returns>Two children with only crossover applied.</returns>
    private Tuple<Day, Day> Crossover(Tuple<Day, Day> parents)
    {
        float crossoverPoint = Random.Range(0, 1);

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
            float mass = 100 * allPortions[i].Quantity;
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

        // If the cutoff index was never defined, then the cutoffMass was never reached
        // i.e. cutoffMass > total mass of both parents
        if (cutoffPortionIndex == -1)
            throw new ArgumentOutOfRangeException("Selected cutoff mass > sum of all masses in both parents.");

        float localCutoffPoint =
            (crossoverPoint - massSum) // The remaining cutoff after last whole portion
            * allPortions[cutoffPortionIndex].Quantity * 100; // Convert cutoff into local cutoff
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
            sum += 100 * day.Portions[i].Quantity;
        }
        return sum;
    }


    /// <summary>
    /// Splits a portion into two, at a given cutoff point in grams.
    /// </summary>
    /// <param name="portion">The portion to split.</param>
    /// <param name="cutoffPoint">The local cutoff point - proportion of the portion
    /// which goes into the left portion.</param>
    /// <returns></returns>
    private Tuple<Portion, Portion> SplitPortion(Portion portion, float cutoffPoint)
    {
        Portion left = new(portion.Food, portion.Quantity * cutoffPoint);
        Portion right = new(portion.Food, portion.Quantity * (1 - cutoffPoint));
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
        foreach (Portion portion in day.Portions)
        {
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
        float sign = Random.Range(0f, 1f) < ChanceToMutatePortion ? 1 : -1;
        float massDiff = Random.Range(MutationMassChangeMin, MutationMassChangeMax);

        float newMass = 100 * portion.Quantity + sign * massDiff;
        portion.Quantity = newMass / 100;

        // If the mass is zero or negative, the portion ceases to exist.
        if (newMass <= 0) return false;
        return true;
    }
}