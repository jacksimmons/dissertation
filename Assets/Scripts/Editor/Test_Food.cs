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
        m_minConstraints = new Constraint[Nutrient.Count];
        m_convConstraints = new Constraint[Nutrient.Count];
        m_rangeConstraints = new Constraint[Nutrient.Count];

        for (int i = 0; i < Nutrient.Count; i++)
        {
            m_minConstraints[i] = new MinimiseConstraint(10_000, 1);
            m_convConstraints[i] = new ConvergeConstraint(10_000, 0.001f, 10_000, 1);
            m_rangeConstraints[i] = new HardConstraint(0, 10_000, 1);
        }
    }


    [Test]
    public void FoodErrorTest()
    {
        // Assertation to ensure the next test works
        Assert.IsTrue(Nutrient.Count < int.MaxValue);

        // Ensure GetProximateUnit throws an exception when given an out-of-range input.
        // This error is impossible to achieve unless casting an int to a ProximateType.
        Assert.Throws(
            typeof(ArgumentOutOfRangeException),
            new(() => Nutrient.GetUnit((ENutrient)int.MaxValue)));
    }


    [Test]
    public void FitnessTest()
    {
        foreach (var kvp in m_population.DayFitnesses)
        {
            float fitness = kvp.Value;

            // Fitness can only be positive or 0.
            Assert.IsTrue(fitness >= 0);

            for (int i = 0; i < Nutrient.Count; i++)
            {
                float proxFitness = Constraints[i].GetFitness(kvp.Key.GetNutrientAmount((ENutrient)i));
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
        Day bestDay = new(this);
        float[] bestNutrients = new float[Nutrient.Count];

        bestDay.AddPortion(new Portion(new Food(new FoodData())));

        // Create the worst Day (will be dominated by any Day object which doesn't
        // have a fitness of PositiveInfinity).
        Day worstDay = new(this);
        float[] worstNutrients = new float[Nutrient.Count];
        worstDay.AddPortion(new(new(new())));


        List<Constraint[]> constraintsLists = new() { m_minConstraints, m_convConstraints, m_rangeConstraints };
        for (int i = 0; i < constraintsLists.Count; i++)
        {
            WorstVsBestTest(constraintsLists[i], bestNutrients, worstNutrients, bestDay, worstDay);
        }
    }


    private void WorstVsBestTest(Constraint[] constraints, float[] bestNutrients, float[] worstNutrients,
        Day bestDay, Day worstDay)
    {
        // Set the nutrients attribs for both worst and best days (bestNutrients and worstNutrients are
        // references to these attribs)
        for (int i = 0; i < Nutrient.Count; i++)
        {
            bestNutrients[i] = constraints[i].BestValue;
            worstNutrients[i] = constraints[i].WorstValue;
        }

        ReadOnlyCollection<Constraint> roConstraints = new(constraints);
        foreach (var kvp in m_population.DayFitnesses)
        {
            // Test that the best day is always at least as good as any randomly selected day.
            Assert.IsFalse(Pareto.DominatesOrMND(bestDay.CompareTo(kvp.Key, roConstraints)));

            // Test that the worst day is always at least as bad as any randomly selected day.
            Assert.IsFalse(Pareto.DominatedOrMND(worstDay.CompareTo(kvp.Key, roConstraints)));

            // Test that the best day STRICTLY dominates the worst day
            Assert.IsTrue(bestDay.CompareTo(worstDay) == ParetoComparison.StrictlyDominates);
            Assert.IsTrue(worstDay.CompareTo(bestDay) == ParetoComparison.StrictlyDominated);
        }
    }
}