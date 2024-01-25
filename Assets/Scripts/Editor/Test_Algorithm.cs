using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Test_Alg : Algorithm
{
    /// <summary>
    /// Tests a few key properties of population generation:
    /// - Number of random Days, and Portions
    /// - Randomness of Portions (Quantity and Food type)
    /// </summary>
    [Test]
    public void TestPopulationGeneration()
    {
        List<Day> pop = GetStartingPopulation();

        // Assert number of random days and portions
        Assert.IsTrue(pop.Count == NumStartingDaysInPop);
        for (int i = 0; i < pop.Count; i++)
        {
            Assert.IsTrue(pop[i].Portions.Count == NumStartingPortionsInDay);

            // Assert that the random ranges are satisfied for every portion
            for (int j = 0; j < pop[i].Portions.Count; j++)
            {
                float quantity = pop[i].Portions[j].Quantity;
                Assert.IsTrue(quantity >= StartingFoodQuantityMin && quantity <= StartingFoodQuantityMax);
            }
        }

        // It is easier to assert that portions are random by eye
        Debug.Log(DayListToString(pop));
    }


    public override void Run() { }
}