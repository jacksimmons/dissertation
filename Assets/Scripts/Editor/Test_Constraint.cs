using NUnit.Framework;
using System;
using UnityEngine;


/// <summary>
/// Test suite for the Constraint class and its subclasses.
/// </summary>
public class Test_Constraint
{
    private void HardTest(HardConstraint hc)
    {
        for (float i = hc.min; i < hc.max; i++)
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
        // Test that the limit gives an infinite fitness
        Assert.True(float.IsPositiveInfinity(mc.GetFitness(mc.max)));

        // Test that values get increasingly worse as the input approaches the limit.
        float prevFitness = 0;
        for (float i = 1; i < mc.max; i++)
        {
            float thisFitness = mc.GetFitness(i);
            Assert.True(thisFitness > prevFitness);
            prevFitness = thisFitness;
        }
    }


    [Test]
    public void NormalTest()
    {
        float goal = 100; float min = 90; float max = 110;
        HardTest(new(min, max));
        ConvergenceTest(new(goal, min, max));
        MinimiseTest(new(max));
    }


    [Test]
    public void ErroneousTest()
    {
        void HCThrowsOutOfRangeTest(float min, float max)
        {
            Assert.Throws(
                typeof(Exception),
                new(() => HardTest(new(min, max))));
        }
        void CCThrowsOutOfRangeTest(float goal, float min, float max)
        {
            Assert.Throws(
                typeof(Exception),
                new(() => ConvergenceTest(new(goal, min, max))));
        }
        void MCThrowsOutOfRangeTest(float max)
        {
            Assert.Throws(
                typeof(Exception),
                new(() => MinimiseTest(new(max))));
        }


        // Test one parameter being invalid each time (only test parameters that are introduced with each subclass; no
        // need to test HC params again with CC.
        HCThrowsOutOfRangeTest(-1, 1); // min < 0
        HCThrowsOutOfRangeTest(2, 1); // max < min (encompasses max < 0)

        MCThrowsOutOfRangeTest(0); // max <= 0

        CCThrowsOutOfRangeTest(-1, 1, 2); // goal < 0
        CCThrowsOutOfRangeTest(5, 6, 7); // goal < min
        CCThrowsOutOfRangeTest(5, 3, 4); // goal > max
    }
}