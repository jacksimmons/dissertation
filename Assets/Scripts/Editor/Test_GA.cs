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
        foreach (var kvp in Population)
        {
            // Test two different methods of summing
            int massSum = 0;
            for (int i = 0; i < kvp.Key.portions.Count; i++)
                massSum += kvp.Key.portions[i].Mass;

            Assert.AreEqual(massSum, kvp.Key.Mass);
        }
    }
}
