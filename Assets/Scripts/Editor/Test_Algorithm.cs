using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


/// <summary>
/// Test suite for the Algorithm class. Inherits from a subclass of Algorithm to access
/// each algorithm's protected fields.
/// </summary>
public class Test_Algorithm
{
    private Assembly m_assembly = Assembly.GetAssembly(typeof(Algorithm));


    [Test]
    public void RunTests()
    {
        NormalTest();
        BoundaryTest();
        ErroneousTest();
    }


    private void NormalTest()
    {
        foreach (string algType in Preferences.ALG_TYPES)
        {
            TestPopulationGeneration(Algorithm.Build(m_assembly.GetType(algType)!));
        }
    }


    /// <summary>
    /// Tests a few key properties of population generation:
    /// - Number of random Days
    /// - Number of random Portions, and that they satisfy the portion mass ranges
    /// </summary>
    private void TestPopulationGeneration(Algorithm alg)
    {
        alg.Init();


        // Assert number of days is correct
        int expectedPopSize = Preferences.Instance.populationSize;
        int actualPopSize = alg.DayFitnesses.Count;
        Assert.IsTrue(expectedPopSize == actualPopSize);


        // Test each population member is valid
        foreach (var kvp in alg.DayFitnesses)
        {
            Assert.IsTrue(kvp.Key.portions.Count == Preferences.Instance.numStartingPortionsPerDay);

            // Assert that the random ranges are satisfied for every portion
            for (int j = 0; j < kvp.Key.portions.Count; j++)
            {
                float mass = kvp.Key.portions[j].Mass;
                Assert.IsTrue(mass >= Preferences.Instance.minPortionMass && mass <= Preferences.Instance.maxPortionMass);
            }
        }
    }


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "False Positive")]
    private bool PrefValueThrows<T>(ref T pref, T value)
    {
        bool throws = false;
        // Set pref value to provided value; store old pref value
        T temp = pref;
        pref = value;

        try
        {
            NormalTest();
        }
        catch (Exception)
        {
            throws = true;
        }

        // Reset old pref value and return
        pref = temp;
        return throws;
    }


    private void BoundaryTest()
    {
        Assert.True(PrefValueThrows(ref Preferences.Instance.populationSize, 1));
        Assert.True(PrefValueThrows(ref Preferences.Instance.populationSize, 2));
    }


    private void ErroneousTest()
    {
        // Try to build an invalid type.
        Assert.Throws(typeof(Exception), () => Algorithm.Build(typeof(Test_Algorithm)));


        // Try to provide invalid Preferences parameters.
        Assert.True(PrefValueThrows(ref Preferences.Instance.acoAlpha, -1));
        Assert.True(PrefValueThrows(ref Preferences.Instance.acoBeta, -1));
        Assert.Throws(typeof(Exception), () => Algorithm.Build(Type.GetType("Test_Algorithm")));
        Assert.True(PrefValueThrows(ref Preferences.Instance.minPortionMass, 0));
        Assert.True(PrefValueThrows(ref Preferences.Instance.maxPortionMass, Preferences.Instance.minPortionMass - 1));
        Assert.True(PrefValueThrows(ref Preferences.Instance.numStartingPortionsPerDay, 0));
        Assert.True(PrefValueThrows(ref Preferences.Instance.pheroEvapRate, -1));
        Assert.True(PrefValueThrows(ref Preferences.Instance.pheroImportance, -1));
        Assert.True(PrefValueThrows(ref Preferences.Instance.populationSize, 0));
    }
}