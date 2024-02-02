using NUnit.Framework;
using System;
using UnityEngine;

public class Test_Constraint
{
    private void ConvergenceTest(float goal, float weight, float steepness, float tolerance)
    {
        // This constraint should support a range of goal - tolerance < x < goal + tolerance.
        ConvergeConstraint cc = new(goal, weight, steepness, tolerance);

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


    private void MinimiseTest(float limit, float weight)
    {
        MinimiseConstraint mc = new(limit, weight);

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
        float goal = 100; float weight = 1; float steepness = 1; float tolerance = 10;
        ConvergenceTest(goal: goal, weight, steepness, tolerance);
        MinimiseTest(limit: goal, weight);
    }


    private void CCThrowsOutOfRangeTest(float goal, float weight, float steepness, float tolerance)
    {
        Assert.Throws(
            typeof(ArgumentOutOfRangeException),
            new(() => ConvergenceTest(goal, weight, steepness, tolerance)));
    }


    private void MCThrowsOutOfRangeTest(float limit, float weight)
    {
        Assert.Throws(
            typeof(ArgumentOutOfRangeException),
            new(() => MinimiseTest(limit, weight)));
    }


    [Test]
    public void ErroneousTest()
    {
        float goal = -100; float weight = -1; float steepness = -1; float tolerance = -10;
        CCThrowsOutOfRangeTest(goal, weight, steepness, tolerance);
        MCThrowsOutOfRangeTest(goal, weight);

        //
        // Test it throws with just one parameter out of range
        //

        // Goal
        weight = 0; steepness = 1; tolerance = 10;
        CCThrowsOutOfRangeTest(goal, weight, steepness, tolerance);
        MCThrowsOutOfRangeTest(goal, weight);

        // Weight
        goal = 100; weight = -1;
        CCThrowsOutOfRangeTest(goal, weight, steepness, tolerance);
        MCThrowsOutOfRangeTest(goal, weight);

        //
        // Convergence constraint only - no other constraints have steepness or tolerance parameters.
        //

        // Steepness
        weight = 0; steepness = 0;
        CCThrowsOutOfRangeTest(goal, weight, steepness, tolerance);

        // Tolerance
        steepness = 1; tolerance = -10;
        CCThrowsOutOfRangeTest(goal, weight, steepness, tolerance);
    }
}