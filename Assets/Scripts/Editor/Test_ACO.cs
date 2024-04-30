using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using static AlgACO;


public class Test_ACO : Test_HasPopulation
{
    [Test]
    public void RunTests()
    {
        // Set baseline values
        Preferences.Instance.Reset();

        AlgACO aco = (AlgACO)Algorithm.Build(typeof(AlgACO));
        aco.Init();

        NormalTest();
        BoundaryTest();
        FitnessTest(aco);

        // Reset preferences so this test doesn't affect them.
        Saving.LoadPreferences();
    }


    /// <summary>
    /// Tests ACO methods at normal values.
    /// </summary>
    private void NormalTest()
    {
        AlgACO aco = (AlgACO)Algorithm.Build(typeof(AlgACO));
        aco.Init();

        SearchSpaceTest(aco);
        ResetAntTest(aco);
        RunAntTest(aco);
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
        RunAntTest(aco);
        SearchSpaceTest(aco);

        // Test probability calculation for large portion count
        Preferences.Instance.colonyPortions = 1000;
        aco = (AlgACO)Algorithm.Build(typeof(AlgACO));
        aco.Init();
        RunAntTest(aco);
        SearchSpaceTest(aco);

        Preferences.Instance.Reset();
        aco = (AlgACO)Algorithm.Build(typeof(AlgACO));
        aco.Init();

        // Create infinite-fitness Day => 0 phero diff
        float[] nutrients = new float[Constraint.Count];
        Day badDay = new(aco);

        // Two portions so the algorithm deposits pheromone
        Portion badPortion1 = new(new(new() { Nutrients = nutrients }), 0);
        Portion badPortion2 = new(new(new() { Nutrients = nutrients }), 0);
        badDay.AddPortion(badPortion1);
        badDay.AddPortion(badPortion2);

        foreach (Ant ant in aco.Ants)
        {
            Tuple<float, float> phero = ComparePheromone(aco, ant, badDay);
            Assert.That(MathTools.Approx(phero.Item1, phero.Item2));
        }

        // Create 0-fitness Day => +Infinity phero diff
        Day bestDay = new(aco);
        nutrients = new float[Constraint.Count];
        for (int i = 0; i < Constraint.Count; i++)
        {
            nutrients[i] = aco.Constraints[i].BestValue;
        }

        // Two portions so the algorithm deposits pheromone
        bestDay.AddPortion(new(new(new() { Nutrients = nutrients })));
        bestDay.AddPortion(new(new(new() { Nutrients = new float[Constraint.Count] }), 1));

        foreach (Ant ant in aco.Ants)
        {
            Tuple<float, float> phero = ComparePheromone(aco, ant, new(bestDay));
            Assert.That(float.IsPositiveInfinity(phero.Item2));
        }
    }


    /// <summary>
    /// Tests regeneration of the search space.
    /// </summary>
    private void SearchSpaceTest(AlgACO aco)
    {
        Portion[] searchSpaceAfter = (Portion[])typeof(AlgACO).GetField("m_vertices", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(aco);
        Portion[] searchSpaceBefore = new Portion[searchSpaceAfter.Length];
        Array.Copy(searchSpaceAfter, searchSpaceBefore, searchSpaceAfter.Length);

        // Ensure after stagnation iterations have passed, one portion is removed
        for (int i = 0; i < Preferences.Instance.colonyStagnationIters + 1; i++)
        {
            aco.RunIteration();
        }

        Assert.That(searchSpaceBefore.Length == searchSpaceAfter.Length);

        // Expect one difference between search space before regeneration and after.
        int differences = 0;
        for (int i = 0; i < searchSpaceBefore.Length; i++)
        {
            if (searchSpaceAfter[i].FoodType != searchSpaceBefore[i].FoodType)
            {
                differences++;
            }
        }
        Assert.That(differences == 1);
    }


    /// <summary>
    /// Ensures resetting an ant reduces its path to the start index only.
    /// </summary>
    private void ResetAntTest(AlgACO aco)
    {
        // Add second index
        foreach (Ant ant in aco.Ants)
        {
            ant.AddIndex(1);
        }

        // Reset path, only start index remains
        foreach (Ant ant in aco.Ants)
        {
            ant.ResetPath();
            Assert.That(ant.PathLength == 1);
            Assert.That(ant.PathContains(0));
        }
    }


    /// <summary>
    /// Test ants produce paths of correct length, using sensible probability
    /// calculations.
    /// </summary>
    private void RunAntTest(AlgACO aco)
    {
        // Asserts probability calculation sums to 1.
        void ProbabilityCalcTest(AlgACO aco)
        {
            foreach (Ant ant in aco.Ants)
            {
                // It is OK to have a probability sum of 0 if all edges were Infinity.
                // Problems would arise if we had a total probability of 0.5, for example.
                float sum = ant.GetAllVertexProbabilities().Sum();
                Assert.That(MathTools.Approx(sum, 1) || MathTools.Approx(sum, 0));
            }
        }
     
        ProbabilityCalcTest(aco);

        foreach (Ant ant in aco.Ants)
        {
            ant.RunAnt();

            // If ant encounters an edge with fitness Infinity, it is not added to the path.
            Assert.That(ant.PathLength <= Preferences.Instance.colonyPortions);
        }
    }


    /// <summary>
    /// Returns the pheromone before and after depositing pheromone
    /// corresponding to the day provided.
    /// </summary>
    /// <returns>Item1: pheromone before, Item2: pheromone after</returns>
    private Tuple<float, float> ComparePheromone(AlgACO aco, Ant ant, Day day)
    {
        float[,] pheroBefore = (float[,])typeof(AlgACO).GetField("m_pheromone", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(aco);
        float pheroBeforeVal = -1;
        ActOnMatrix(pheroBefore, (int i, int j, float v) =>
        {
            if (i == 0 && j == 1)
            {
                pheroBeforeVal = v;
            }
        });

        // Frame the 1st index as causing the following...
        ant.AddIndex(1);

        // Use reflection to modify the Path to the bad day
        typeof(Ant).GetProperty("Path", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ant, day);
        ant.DepositPheromone();

        float[,] phero = (float[,])typeof(AlgACO).GetField("m_pheromone", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(aco);
        float pheroAfterVal = -2;
        ActOnMatrix(phero, (int i, int j, float v) =>
        {
            if (i == 0 && j == 1)
            {
                pheroAfterVal = v;
            }
        });

        return new(pheroBeforeVal, pheroAfterVal);
    }
}