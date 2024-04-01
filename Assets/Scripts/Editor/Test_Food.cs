using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

/// <summary>
/// Test suite for Food, Portion and Day classes.
/// </summary>
public class Test_Food
{
    private ConstraintData[] m_minConstraints;
    private ConstraintData[] m_convConstraints;
    private ConstraintData[] m_rangeConstraints;


    public Test_Food()
    {
        m_minConstraints = new ConstraintData[Nutrient.Count];
        m_convConstraints = new ConstraintData[Nutrient.Count];
        m_rangeConstraints = new ConstraintData[Nutrient.Count];

        for (int i = 0; i < Nutrient.Count; i++)
        {
            m_minConstraints[i] = new()
            {
                Max = 10_000,
                Weight = 1,
                Type = typeof(MinimiseConstraint).FullName
            };

            m_convConstraints[i] = new()
            {
                Goal = 10_000,
                Min = 0.001f,
                Max = 10_000,
                Weight = 1,
                Type = typeof(ConvergeConstraint).FullName
            };

            m_rangeConstraints[i] = new()
            {
                Min = 0,
                Max = 10_000,
                Weight = 1,
                Type = typeof(HardConstraint).FullName
            };
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
        AlgTest alg = new();
        foreach (Day day in alg.Population)
        {
            float fitness = day.TotalFitness.Value;

            // Fitness can only be positive or 0.
            Assert.IsTrue(fitness >= 0);

            for (int i = 0; i < Nutrient.Count; i++)
            {
                // Fitness for each nutrient can only be positive or 0.
                float nutrientFitness = alg.Constraints[i].GetFitness(day.GetNutrientAmount((ENutrient)i));
                Assert.IsTrue(nutrientFitness >= 0);
            }
        }
    }


    /// <summary>
    /// Tests the pareto comparison method in the Day class.
    /// </summary>
    [Test]
    public void ParetoComparisonTest()
    {
        AlgTest algTest = new();

        // Create a best Day (this will dominate any Day object)
        Day bestDay = new(algTest);
        float[] bestNutrients = new float[Nutrient.Count];

        bestDay.AddPortion(new Portion(new Food(new FoodData())));

        // Create the worst Day (will be dominated by any Day object which doesn't
        // have a fitness of PositiveInfinity).
        Day worstDay = new(algTest);
        float[] worstNutrients = new float[Nutrient.Count];
        worstDay.AddPortion(new(new(new())));


        List<ConstraintData[]> constraintsLists = new() { m_minConstraints, m_convConstraints, m_rangeConstraints };
        for (int i = 0; i < constraintsLists.Count; i++)
        {
            WorstVsBestTest(constraintsLists[i], bestNutrients, worstNutrients, bestDay, worstDay);
        }
    }


    private void WorstVsBestTest(ConstraintData[] constraints, float[] bestNutrients, float[] worstNutrients,
        Day bestDay, Day worstDay)
    {
        // Set the nutrients attribs for both worst and best days (bestNutrients and worstNutrients are
        // references to these attribs)
        for (int i = 0; i < Nutrient.Count; i++)
        {
            bestNutrients[i] = Constraint.Build(constraints[i]).BestValue;
            worstNutrients[i] = Constraint.Build(constraints[i]).WorstValue;
        }

        // Set preferences
        Preferences.Instance.constraints = constraints;
        AlgTest algTest = new();
        foreach (Day day in algTest.Population)
        {
            // Test that the best day is always at least as good as any randomly selected day.
            Assert.IsFalse(day < bestDay);

            // Test that the worst day is always at least as bad as any randomly selected day.
            Assert.IsFalse(day > worstDay);

            // Test that the best day STRICTLY dominates the worst day
            Assert.IsTrue(bestDay < worstDay);
        }
        // Reload old preferences
        Saving.LoadPreferences();
    }
}