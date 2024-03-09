using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Test_Algorithm : AlgSFGA
{
    public Test_Algorithm() : base()
    {
        Instance = this;
    }


    /// <summary>
    /// Tests a few key properties of population generation:
    /// - Number of random Days, and portions
    /// - Randomness of portions (Quantity and Food type)
    /// </summary>
    [Test]
    public void TestPopulationGeneration()
    {
        // Assert number of random days and portions
        Assert.IsTrue(Population.Count == Preferences.Instance.populationSize);
        foreach (Day day in Population)
        {
            Assert.IsTrue(day.portions.Count == Preferences.Instance.numStartingPortionsPerDay);

            // Assert that the random ranges are satisfied for every portion
            for (int j = 0; j < day.portions.Count; j++)
            {
                float mass = day.portions[j].Mass;
                Assert.IsTrue(mass >= Preferences.Instance.portionMinStartMass && mass <= Preferences.Instance.portionMaxStartMass);
            }
        }
    }


    protected override void NextIteration() { }
}