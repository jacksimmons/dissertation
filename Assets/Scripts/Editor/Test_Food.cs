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
    public void NonNegativeFitnessTest()
    {
        List<Day> days = GetStartingPopulation();

        foreach (Day day in days)
        {
            float fitness = GetFitness(day).Value;
            Debug.Log($"{day}\nFitness: {fitness}");

            // Fitness can only be positive or 0.
            Assert.IsTrue(fitness >= 0);
        }
    }
}