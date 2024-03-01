using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


/// <summary>
/// Tests for Genetic Algorithm functions.
/// </summary>
public class Test_GA : AlgSFGA
{
    [Test]
    public void TestCrossoverOperator()
    {
        // Test that crossover submethods work
        foreach (Day day in Population)
        {
            // Test two different methods of summing
            int massSum = 0;
            for (int i = 0; i < day.Portions.Count; i++)
                massSum += day.Portions[i].Mass;

            Assert.AreEqual(massSum, day.GetMass());
        }
    }
}
