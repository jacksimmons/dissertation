using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Test suite for Food, Portion and Day classes.
/// </summary>
public class Test_Food : GeneticAlgorithm
{
    public Test_Food() : base()
    {
        Instance = this;
    }


    [Test]
    public void FoodErrorTest()
    {
        // Assertation to ensure the next test works
        Assert.IsTrue(Enum.GetValues(typeof(Proximate)).Length < int.MaxValue);

        // Ensure GetProximateUnit throws an exception when given an out-of-range input.
        // This error is impossible to achieve unless casting an int to a ProximateType.
        Assert.Throws(
            typeof(ArgumentOutOfRangeException),
            new(() => Food.GetProximateUnit((Proximate)int.MaxValue)));
    }


    [Test]
    public void FitnessTest()
    {
        foreach (Day day in Population.Keys)
        {
            float fitness = day.GetFitness();

            // Fitness can only be positive or 0.
            Assert.IsTrue(fitness >= 0);

            foreach (Proximate proximate in Constraints.Keys)
            {
                float proxFitness = Constraints[proximate]._GetFitness(day.GetProximateAmount(proximate));
                // Fitness for each proximate can only be positive or 0.
                Assert.IsTrue(proxFitness >= 0);
            }
        }
    }
}