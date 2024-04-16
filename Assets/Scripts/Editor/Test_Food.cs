using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// Test suite for Food, Portion and Day classes.
/// </summary>
public class Test_Food
{
    private ConstraintData[] m_minConstraints;
    private ConstraintData[] m_convConstraints;
    private ConstraintData[] m_rangeConstraints;


    public Test_Food()
    {
        m_minConstraints = new ConstraintData[Constraint.Count];
        m_convConstraints = new ConstraintData[Constraint.Count];
        m_rangeConstraints = new ConstraintData[Constraint.Count];

        for (int i = 0; i < Constraint.Count; i++)
        {
            m_minConstraints[i] = new()
            {
                Max = 10_000,
                Weight = 1,
                Type = typeof(MinimiseConstraint).FullName
            };

            m_convConstraints[i] = new()
            {
                Goal = 10_000,
                Min = 0f,
                Max = 10_000,
                Weight = 1,
                Type = typeof(ConvergeConstraint).FullName
            };

            m_rangeConstraints[i] = new()
            {
                Min = 0,
                Max = 10_000,
                Weight = 1,
                Type = typeof(HardConstraint).FullName
            };
        }
    }


    /// <summary>
    /// Tests the pareto comparison method in the Day class.
    /// </summary>
    [Test]
    public void ParetoComparisonTest()
    {
        List<ConstraintData[]> constraintsLists = new() { m_minConstraints, m_convConstraints, m_rangeConstraints };
        for (int i = 0; i < constraintsLists.Count; i++)
        {
            WorstVsBestTest(constraintsLists[i]);
        }
    }


    private void WorstVsBestTest(ConstraintData[] constraints)
    {
        // Set preferences
        Preferences.Instance.Reset();
        Preferences.Instance.constraints = constraints;

        AlgEmpty algTest = new();

        float[] bestNutrients = new float[Constraint.Count];
        float[] worstNutrients = new float[Constraint.Count];
        // Set the nutrients attribs for both worst and best days (bestNutrients and worstNutrients are
        // references to these attribs)
        for (int i = 0; i < Constraint.Count; i++)
        {
            bestNutrients[i] = Constraint.Build(constraints[i]).BestValue;
            worstNutrients[i] = Constraint.Build(constraints[i]).WorstValue;
        }

        Day bestDay = new(algTest);
        // Add the best possible portion
        bestDay.AddPortion(new(new(new() { Nutrients = bestNutrients })));

        Day worstDay = new(algTest);
        // Add the worst possible portion
        worstDay.AddPortion(new(new(new() { Nutrients = worstNutrients })));

        // Assert that the best day dominates the worst day
        Assert.IsTrue(bestDay < worstDay);

        // Reload old preferences
        Saving.LoadPreferences();
    }
}