using NUnit.Framework;
using System;
using System.Reflection;


/// <summary>
/// Test suite for the Algorithm class. Inherits from a subclass of Algorithm to access
/// each algorithm's protected fields.
/// </summary>
public class Test_Algorithm
{
    private readonly Assembly m_assembly = Assembly.GetAssembly(typeof(Algorithm));


    [Test]
    public void RunTests()
    {
        // Load in some sensible constraints to test on
        Preferences.Instance.Reset();

        NormalTest();
        BoundaryTest();
        ErroneousTest();

        // Reload user's specified constraints
        Saving.LoadPreferences();
    }


    private void NormalTest()
    {
        foreach (string algType in Preferences.ALG_TYPES)
        {
            // PSO doesn't generate a population of Days unlike the others.
            if (algType == typeof(AlgPSO).FullName) continue;
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
        int actualPopSize = alg.Population.Count;

        Assert.IsTrue(expectedPopSize == actualPopSize);


        // Test each population member is valid
        foreach (Day day in alg.Population)
        {
            Assert.IsTrue(day.portions.Count == Preferences.Instance.numStartingPortionsPerDay);

            // Assert that the random ranges are satisfied for every portion
            for (int j = 0; j < day.portions.Count; j++)
            {
                float mass = day.portions[j].Mass;
                Assert.IsTrue(mass >= Preferences.Instance.minPortionMass && mass <= Preferences.Instance.maxPortionMass);
            }
        }
    }


    /// <summary>
    /// Assertation test which asserts that NormalTest throws an error, given
    /// the provided preference assigned to a provided value.
    /// </summary>
    /// <typeparam name="T">The preference type.</typeparam>
    /// <param name="pref">Reference to the preference to assign to.</param>
    /// <param name="value">The erroneous value which should cause an Exception.</param>
    private void AssertPrefValueThrows<T>(ref T pref, T value)
    {
        // Set pref value to provided value; store old pref value
        T temp = pref;
        pref = value;

        // Assert that this pref value would cause a Warn in normal execution.
        Assert.Throws(typeof(WarnException), NormalTest);

        // Reset old pref value and return
        pref = temp;
    }


    private void BoundaryTest()
    {
        AssertPrefValueThrows(ref Preferences.Instance.populationSize, 1);
        AssertPrefValueThrows(ref Preferences.Instance.populationSize, 2);
    }


    private void ErroneousTest()
    {
        // Try to build an invalid type.
        Assert.Throws(typeof(Exception), () => Algorithm.Build(typeof(Test_Algorithm)));


        // Try to provide invalid Preferences parameters.
        AssertPrefValueThrows(ref Preferences.Instance.acoAlpha, -1);
        AssertPrefValueThrows(ref Preferences.Instance.acoBeta, -1);
        Assert.Throws(typeof(Exception), () => Algorithm.Build(Type.GetType("Test_Algorithm")));
        AssertPrefValueThrows(ref Preferences.Instance.minPortionMass, 0);
        AssertPrefValueThrows(ref Preferences.Instance.maxPortionMass, Preferences.Instance.minPortionMass - 1);
        AssertPrefValueThrows(ref Preferences.Instance.numStartingPortionsPerDay, 0);
        AssertPrefValueThrows(ref Preferences.Instance.pheroEvapRate, -1);
        AssertPrefValueThrows(ref Preferences.Instance.pheroImportance, -1);
        AssertPrefValueThrows(ref Preferences.Instance.populationSize, 0);

        Assert.True(false);
    }
}