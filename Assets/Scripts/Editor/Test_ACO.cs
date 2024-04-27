using NUnit.Framework;
using System.Linq;
using UnityEngine;
using static AlgACO;

public class Test_ACO : Test_HasPopulation
{
    [Test]
    public void RunTests()
    {
        Preferences.Instance.Reset();

        AlgACO aco = (AlgACO)Algorithm.Build(typeof(AlgACO));
        aco.Init();

        NormalTest();
        BoundaryTest();
        ErroneousTest();
        FitnessTest(aco);

        // Reset preferences so this test doesn't affect them.
        Saving.LoadPreferences();
    }


    private void NormalTest()
    {
    }


    /// <summary>
    /// Tests ACO methods at extreme values.
    /// </summary>
    private void BoundaryTest()
    {
        // Test probability calculation for small portion count
        Preferences.Instance.colonyPortions = 1;
        AlgACO aco = (AlgACO)Algorithm.Build(typeof(AlgACO));
        aco.Init();
        ProbabilityCalcTest(aco);

        // Test probability calculation for large portion count
        Preferences.Instance.colonyPortions = 1000;
        aco = (AlgACO)Algorithm.Build(typeof(AlgACO));
        aco.Init();
        ProbabilityCalcTest(aco);
    }


    private void ErroneousTest()
    {

    }


    private void PheromoneDepositionTest(Day day)
    {
    }


    /// <summary>
    /// Asserts that probability distributions created by an ant always sum to 1.
    /// </summary>
    /// <param name="aco"></param>
    private void ProbabilityCalcTest(AlgACO aco)
    {
        foreach (Ant ant in aco.Ants)
        {
            // It is OK to have a probability of 0 for all movements IF there is only one portion considered by the
            // ants' paths (as there is nowhere for the ant to go, we assert this must be the case).
            if (Preferences.Instance.colonyPortions == 1)
            {
                Assert.That(MathTools.Approx(ant.GetAllVertexProbabilities().Sum(), 0));
            }
            else
            {
                Logger.Log(ant.GetAllVertexProbabilities().Sum());
                Assert.That(MathTools.Approx(ant.GetAllVertexProbabilities().Sum(), 1));
            }
        }
    }
}