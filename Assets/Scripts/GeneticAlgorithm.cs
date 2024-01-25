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
    public override void Run()
    {
        // Generate population and constraints
        m_population = GetStartingPopulation();
        Debug.Log(DayListToString(m_population));

        m_constraints = new Dictionary<ProximateType, Constraint>
        {
            { ProximateType.Kcal, new(ConstraintType.Minimise, 20, 3000) }
        };

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
            Debug.Log($"{i}: {m_population[i].GetFitness(m_constraints)}");
        }

        // Crossover

        // Mutation

        // Integration
    }


    private Tuple<Day, Day> Selection(List<Day> days)
    {
        float lowestFitness = float.PositiveInfinity;
        float secondLowestFitness = float.PositiveInfinity;
        Day fittestDay = null;
        Day secondFittestDay = null;

        foreach (Day day in days)
        {
            float dayFitness = day.GetFitness(m_constraints);

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

        // By default, left child portions are empty, and right are full.
        List<Portion> leftPortions = new();
        List<Portion> rightPortions = new();

        int cutoffPortionIndex = -1;

        for (int i = 0; i < allPortions.Count; i++)
        {
            float mass = 100 * allPortions[i].Quantity;
            if (massSum + mass < cutoffMass)
            {
                massSum += mass;

                // Move a portion over to the left child
                leftPortions.Add(allPortions[i]);
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
            rightPortions.Add(allPortions[i]);
        }

        // If the cutoff index was never defined, then the cutoffMass was never reached
        // i.e. cutoffMass > total mass of both parents
        if (cutoffPortionIndex == -1)
            throw new ArgumentOutOfRangeException("Selected cutoff mass > sum of all masses in both parents.");

        float localCutoffPoint =
            (crossoverPoint - massSum) // The remaining cutoff after last whole portion
            * allPortions[cutoffPortionIndex].Quantity * 100; // Convert cutoff into local cutoff
        Tuple<Portion, Portion> split = SplitPortion(allPortions[cutoffPortionIndex], localCutoffPoint);

        leftPortions.Add(split.Item1);
        rightPortions.Add(split.Item2);

        // Construct a tuple of two child Days from the split portions.
        return new(new(leftPortions), new(rightPortions));
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
}