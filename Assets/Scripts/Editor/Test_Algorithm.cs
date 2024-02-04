using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Test_Algorithm : Algorithm
{
    public Test_Algorithm() : base()
    {
        Instance = this;
    }


    /// <summary>
    /// Tests a few key properties of population generation:
    /// - Number of random Days, and Portions
    /// - Randomness of Portions (Quantity and Food type)
    /// </summary>
    [Test]
    public void TestPopulationGeneration()
    {
        // Assert number of random days and portions
        Assert.IsTrue(Population.Count == NumStartingDaysInPop);
        foreach (Day day in Population.Keys)
        {
            Assert.IsTrue(day.Portions.Count == NumStartingPortionsInDay);

            // Assert that the random ranges are satisfied for every portion
            for (int j = 0; j < day.Portions.Count; j++)
            {
                float mass = day.Portions[j].Mass;
                Assert.IsTrue(mass >= StartingPortionMassMin && mass <= StartingPortionMassMax);
            }
        }
    }


    protected override void RunIteration() { }
}