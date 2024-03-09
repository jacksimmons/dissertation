using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

/// <summary>
/// Test suite for Food, Portion and Day classes.
/// </summary>
public class Test_Food : AlgSFGA
{
    private Constraint[] m_minConstraints;
    private Constraint[] m_convConstraints;
    private Constraint[] m_rangeConstraints;


    public Test_Food() : base()
    {
        Instance = this;

        m_minConstraints = new Constraint[Nutrients.Count];
        m_convConstraints = new Constraint[Nutrients.Count];
        m_rangeConstraints = new Constraint[Nutrients.Count];

        for (int i = 0; i < Nutrients.Count; i++)
        {
            m_minConstraints[i] = new MinimiseConstraint(10_000);
            m_convConstraints[i] = new ConvergeConstraint(10_000, 0.001f, 10_000);
            m_rangeConstraints[i] = new RangeConstraint(0, 10_000);
        }
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


        List<Constraint[]> constraintsLists = new() { m_minConstraints, m_convConstraints, m_rangeConstraints };
        for (int i = 0; i < constraintsLists.Count; i++)
        {
            Logger.Log(i);
            WorstVsBestTest(constraintsLists[i], bestNutrients, worstNutrients, bestDay, worstDay);
        }
    }


    private void WorstVsBestTest(Constraint[] constraints, float[] bestNutrients, float[] worstNutrients,
        Day bestDay, Day worstDay)
    {
        // Set the nutrients attribs for both worst and best days (bestNutrients and worstNutrients are
        // references to these attribs)
        for (int i = 0; i < Nutrients.Count; i++)
        {
            bestNutrients[i] = constraints[i].BestValue;
            worstNutrients[i] = constraints[i].WorstValue;
        }

        ReadOnlyCollection<Constraint> roConstraints = new(constraints);
        foreach (Day day in Population)
        {
            // Test that the best day is always at least as good as any randomly selected day.
            Assert.IsFalse(Pareto.DominatesOrMND(Day.Compare(bestDay, day, roConstraints, constraints.Length)));

            // Test that the worst day is always at least as bad as any randomly selected day.
            Assert.IsFalse(Pareto.DominatedOrMND(Day.Compare(worstDay, day, roConstraints, constraints.Length)));

            // Test that the best day STRICTLY dominates the worst day
            Assert.IsTrue(Day.Compare(bestDay, worstDay) == ParetoComparison.StrictlyDominates);
            Assert.IsTrue(Day.Compare(worstDay, bestDay) == ParetoComparison.StrictlyDominated);
        }
    }
}