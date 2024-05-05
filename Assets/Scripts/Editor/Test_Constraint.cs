using NUnit.Framework;
using UnityEngine;


/// <summary>
/// Test suite for the Constraint class and its subclasses.
/// Note that these tests are also covered by the UI tests.
/// </summary>
public class Test_Constraint
{
    private void HardTest(HardConstraint hc)
    {
        for (float i = hc.min + 1; i < hc.max; i++)
        {
            Assert.True(Mathf.Approximately(hc.GetFitness(i), 0));
        }

        // Test limits
        Assert.True(float.IsPositiveInfinity(hc.GetFitness(hc.min - 1)));
        Assert.True(float.IsPositiveInfinity(hc.GetFitness(hc.max + 1)));
    }


    private void ConvergenceTest(ConvergeConstraint cc)
    {
        // Test that the goal gives a fitness of 0.
        Assert.True(Mathf.Approximately(cc.GetFitness(cc.goal), 0));

        // Test that values get increasingly worse as the input deviates from the goal. (Both directions)
        float prevFitness = 0;
        for (float i = cc.goal + 1; i < cc.max; i++)
        {
            float thisFitness = cc.GetFitness(i);
            Assert.True(thisFitness > prevFitness);
            prevFitness = thisFitness;
        }
        prevFitness = 0;
        for (float i = cc.goal - 1; i > cc.min; i--)
        {
            float thisFitness = cc.GetFitness(i);
            Assert.True(thisFitness > prevFitness);
            prevFitness = thisFitness;
        }
    }


    private void MinimiseTest(MinimiseConstraint mc)
    {
        // Test that the after the limit gives an infinite fitness
        Assert.True(float.IsPositiveInfinity(mc.GetFitness(mc.max + 1)));

        // Test that values get increasingly worse as the input approaches the limit.
        float prevFitness = 0;
        for (float i = 1; i < mc.max; i++)
        {
            float thisFitness = mc.GetFitness(i);
            Assert.True(thisFitness > prevFitness);
            prevFitness = thisFitness;
        }
    }


    private void WeightTest()
    {
        static void Test012Weights(Constraint w0, Constraint w1, Constraint w2)
        {
            float f0 = w0.GetFitness(100);
            float f1 = w1.GetFitness(100);
            float f2 = w2.GetFitness(100);

            // Transitive tests: (f0 == 0 * f1 && f1 = 0.5 * f2 => f0 == 0 * 0.5 * f2 == 0 * f2)
            Assert.True(f0 == 0 * f1);
            Assert.True(f1 == 0.5f * f2);
        }

        HardConstraint hc0 = new(90, 110, 0);
        HardConstraint hc1 = new(90, 110, 1);
        HardConstraint hc2 = new(90, 110, 2);
        Test012Weights(hc0, hc1, hc2);

        MinimiseConstraint mc0 = new(110, 0);
        MinimiseConstraint mc1 = new(110, 1);
        MinimiseConstraint mc2 = new(110, 2);
        Test012Weights(mc0, mc1, mc2);

        // Set goal to != 100, so that the test doesn't give a fitness of 0 for all.
        ConvergeConstraint cc0 = new(99, 90, 110, 0);
        ConvergeConstraint cc1 = new(99, 90, 110, 1);
        ConvergeConstraint cc2 = new(99, 90, 110, 2);
        Test012Weights(cc0, cc1, cc2);
    }


    [Test]
    public void NormalTest()
    {
        float goal = 100; float min = 90; float max = 110; float weight = 1;

        // For non-GA, hard constraints are not imposed. Tests are for the more restrictive AlgGA case.
        Preferences.Instance.algorithmType = typeof(AlgGA).FullName;

        HardTest(new(min, max, weight));
        ConvergenceTest(new(goal, min, max, weight));
        MinimiseTest(new(max, weight));
        WeightTest();

        Saving.LoadPreferences();
    }


    [Test]
    public void ErroneousTest()
    {
        Constraint BuildConstraint(float min, float max, float weight, string type, float goal = 0)
        {
            return Constraint.Build(new() { Min = min, Max = max, Weight = weight, Type = type });
        }
        void HCThrowsOutOfRangeTest(float min, float max, float weight = 1)
        {
            Assert.Throws(
                typeof(WarnException),
                new(() => HardTest((HardConstraint)BuildConstraint(min, max, weight, "HardConstraint"))));
        }
        void CCThrowsOutOfRangeTest(float goal, float min, float max, float weight = 1)
        {
            Assert.Throws(
                typeof(WarnException),
                new(() => ConvergenceTest((ConvergeConstraint)BuildConstraint(min, max, weight, "HardConstraint", goal))));
        }
        void MCThrowsOutOfRangeTest(float max, float weight = 1)
        {
            Assert.Throws(
                typeof(WarnException),
                new(() => MinimiseTest((MinimiseConstraint)BuildConstraint(0, max, weight, "HardConstraint"))));
        }


        // Test one parameter being invalid each time (only test parameters that are introduced with each subclass; no
        // need to test HC params again with CC.
        HCThrowsOutOfRangeTest(2, 1); // max < min (also max < 0)
        HCThrowsOutOfRangeTest(1, 2, -1); // Invalid weight

        MCThrowsOutOfRangeTest(-1); // max < min (also max < 0)
        MCThrowsOutOfRangeTest(0, -1); // Invalid weight

        CCThrowsOutOfRangeTest(-1, 1, 2); // goal < 0
        CCThrowsOutOfRangeTest(5, 6, 7); // goal < min
        CCThrowsOutOfRangeTest(5, 3, 4); // goal > max
        CCThrowsOutOfRangeTest(10, 0, 20, -1); // Invalid weight
    }
}