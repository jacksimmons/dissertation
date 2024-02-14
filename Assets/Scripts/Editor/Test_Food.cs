using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Test suite for Food, Portion and Day classes.
/// </summary>
public class Test_Food : SummedFitnessGA
{
    public Test_Food() : base()
    {
        Instance = this;
    }


    [Test]
    public void FoodErrorTest()
    {
        // Assertation to ensure the next test works
        Assert.IsTrue(Nutrients.Count < int.MaxValue);

        // Ensure GetProximateUnit throws an exception when given an out-of-range input.
        // This error is impossible to achieve unless casting an int to a ProximateType.
        Assert.Throws(
            typeof(ArgumentOutOfRangeException),
            new(() => Nutrients.GetUnit((Nutrient)int.MaxValue)));
    }


    [Test]
    public void FitnessTest()
    {
        foreach (Day day in Population)
        {
            float fitness = day.GetFitness();

            // Fitness can only be positive or 0.
            Assert.IsTrue(fitness >= 0);

            for (int i = 0; i < Nutrients.Count; i++)
            {
                float proxFitness = Constraints[i]._GetFitness(day.GetNutrientAmount((Nutrient)i));
                // Fitness for each proximate can only be positive or 0.
                Assert.IsTrue(proxFitness >= 0);
            }
        }
    }


    /// <summary>
    /// Tests the pareto comparison method in the Day class.
    /// </summary>
    [Test]
    public void ParetoComparisonTest()
    {
        // Create a best Day (this will dominate any Day object)
        Day bestDay = new();
        float[] bestNutrients = new float[Nutrients.Count];
        bestDay.AddPortion(new(new("", "", "", "", bestNutrients), 100));

        // Create the worst Day (will be dominated by any Day object which doesn't
        // have a fitness of PositiveInfinity).
        Day worstDay = new();
        float[] worstNutrients = new float[Nutrients.Count];
        worstDay.AddPortion(new(new("", "", "", "", worstNutrients), 100));

        for (int i = 0; i < Nutrients.Count; i++)
        {
            bestNutrients[i] = Constraints[i].BestValue;
            worstNutrients[i] = Constraints[i].WorstValue;
        }

        foreach (Day day in Population)
        {
            // Test that the best day is always at least as good as any randomly selected day.
            Assert.IsFalse(Day.Compare(bestDay, day) == ParetoComparison.Dominated);
            Assert.IsFalse(Day.SimpleCompare(bestDay, day) == ParetoComparison.Dominated);

            // Test that the worst day is always at least as bad as any randomly selected day.
            Assert.IsFalse(Day.Compare(worstDay, day) == ParetoComparison.Dominates);
            Assert.IsFalse(Day.SimpleCompare(worstDay, day) == ParetoComparison.Dominates);

            // Test that the best day STRICTLY dominates the worst day
            Assert.IsTrue(Day.Compare(bestDay, worstDay) == ParetoComparison.StrictlyDominates);
            Assert.IsTrue(Day.Compare(worstDay, bestDay) == ParetoComparison.StrictlyDominated);
        }
    }
}