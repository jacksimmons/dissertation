using NUnit.Framework;
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


    private void ErroneousTest()
    {
        // Try to build an invalid type.
        Assert.Throws(typeof(WarnException), () => Algorithm.Build(typeof(Test_Algorithm)));
    }
}