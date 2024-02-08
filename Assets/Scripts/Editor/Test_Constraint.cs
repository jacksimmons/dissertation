using NUnit.Framework;
using System;
using UnityEngine;

public class Test_Constraint
{
    private void ConvergenceTest(float goal, float steepness, float tolerance)
    {
        // This constraint should support a range of goal - tolerance < x < goal + tolerance.
        ConvergeConstraint cc = new(goal, steepness, tolerance);

        // Test that the goal gives a fitness of 0.
        Assert.True(Mathf.Approximately(cc._GetFitness(100), 0));

        // Test that values get increasingly worse as the input deviates from the goal. (Both directions)
        float prevFitness = 0;
        for (float i = goal + 1; i < goal + tolerance; i++)
        {
            float thisFitness = cc._GetFitness(i);
            Assert.True(thisFitness > prevFitness);
            prevFitness = thisFitness;
        }
        prevFitness = 0;
        for (float i = goal - 1; i > goal - tolerance; i--)
        {
            float thisFitness = cc._GetFitness(i);
            Assert.True(thisFitness > prevFitness);
            prevFitness = thisFitness;
        }

        // Test limits
        Assert.True(float.IsPositiveInfinity(cc._GetFitness(90)));
        Assert.True(float.IsPositiveInfinity(cc._GetFitness(110)));
    }


    private void MinimiseTest(float limit)
    {
        MinimiseConstraint mc = new(limit);

        // Test that the limit gives an infinite fitness
        Assert.True(float.IsPositiveInfinity(mc._GetFitness(limit)));

        // Test that values get increasingly worse as the input approaches the limit.
        float prevFitness = 0;
        for (float i = 1; i < limit; i++)
        {
            float thisFitness = mc._GetFitness(i);
            Assert.True(thisFitness > prevFitness);
            prevFitness = thisFitness;
        }
    }


    [Test]
    public void NormalTest()
    {
        float goal = 100; float steepness = 1; float tolerance = 10;
        ConvergenceTest(goal: goal, steepness, tolerance);
        MinimiseTest(limit: goal);
    }


    private void CCThrowsOutOfRangeTest(float goal, float steepness, float tolerance)
    {
        Assert.Throws(
            typeof(ArgumentOutOfRangeException),
            new(() => ConvergenceTest(goal, steepness, tolerance)));
    }


    private void MCThrowsOutOfRangeTest(float limit)
    {
        Assert.Throws(
            typeof(ArgumentOutOfRangeException),
            new(() => MinimiseTest(limit)));
    }


    [Test]
    public void ErroneousTest()
    {
        float goal = -100; float steepness = -1; float tolerance = -10;
        CCThrowsOutOfRangeTest(goal, steepness, tolerance);
        MCThrowsOutOfRangeTest(goal);

        //
        // Test it throws with just one parameter out of range
        //

        // Goal
        steepness = 1; tolerance = 10;
        CCThrowsOutOfRangeTest(goal, steepness, tolerance);
        MCThrowsOutOfRangeTest(goal);

        // Weight
        goal = 100;
        CCThrowsOutOfRangeTest(goal, steepness, tolerance);
        MCThrowsOutOfRangeTest(goal);

        //
        // Convergence constraint only - no other constraints have steepness or tolerance parameters.
        //

        // Steepness
        steepness = 0;
        CCThrowsOutOfRangeTest(goal, steepness, tolerance);

        // Tolerance
        steepness = 1; tolerance = -10;
        CCThrowsOutOfRangeTest(goal, steepness, tolerance);
    }
}