using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TestTools;


/// <summary>
/// Tests for Genetic Algorithm functions.
/// </summary>
public class Test_GA
{
    [Test]
    public void RunTests()
    {
        string[] gaAlgs = { "AlgSFGA", "AlgPDGA" };

        AlgSFGA sfga = (AlgSFGA)Algorithm.Build(typeof(AlgSFGA));
        sfga.Init();

        NormalTest(sfga);
        BoundaryTest(sfga);
        ErroneousTest(sfga);
    }


    private void NormalTest(AlgSFGA sfga)
    {
        // Selection
        List<Day> sfgaDays = sfga.DayFitnesses.Keys.ToList();
        SelectionTest(sfga, sfgaDays);

        // Crossover
        CrossoverTest(sfga, sfga.PerformPairSelection(sfga.DayFitnesses.Keys.ToList(), true));
    }


    private void BoundaryTest(AlgSFGA sfga)
    {
        // Selection of just 2
        List<Day> sfgaDays = sfga.DayFitnesses.Keys.ToList().GetRange(0, 2);
        SelectionTest(sfga, sfgaDays);

        // Selection of just 1 (should return itself)
        Day sfgaDay = sfga.DayFitnesses.Keys.ToList()[0];
        sfgaDays = new() { sfgaDay };
        SelectionTest(sfga, sfgaDays);

        // Crossover with no portions
        Day parentA = new(sfga); // No portions
        Day parentB = new(sfga);
        FoodData food = new();
        food.nutrients[0] = 300;
        CrossoverTest(sfga, sfga.PerformPairSelection(new() { parentA, parentB }, true));
    }


    private void ErroneousTest(AlgSFGA sfga)
    {
        // Selection of 0
        Assert.Throws(typeof(Exception), () => SelectionTest(sfga, new()));
    }


    private void SelectionTest(AlgSFGA sfga, List<Day> sfgaDays)
    {
        if (sfgaDays.Count == 1)
        {
            Assert.True(sfga.TournamentSelection(sfgaDays, true) == sfgaDays[0]);
            Assert.True(sfga.TournamentSelection(sfgaDays, false) == sfgaDays[0]);

            Assert.True(sfga.RankSelection(sfgaDays, true) == sfgaDays[0]);
            Assert.True(sfga.RankSelection(sfgaDays, false) == sfgaDays[0]);
        }
        else
        {
            Assert.NotNull(sfga.TournamentSelection(sfgaDays, true));
            Assert.NotNull(sfga.TournamentSelection(sfgaDays, false));
    
            Assert.NotNull(sfga.RankSelection(sfgaDays, true));
            Assert.NotNull(sfga.RankSelection(sfgaDays, false));
        }
    }


    private void CrossoverTest(AlgSFGA sfga, Tuple<Day, Day> parents)
    {
        // For every crossover, all of the childrens' Foods must be a member of one of the parents.
        // Also, the childrens' mass must equal the parents' mass, unless the max portion mass was reached

        int parentMass = parents.Item1.Mass + parents.Item2.Mass;

        Tuple<Day, Day> children = sfga.Crossover(parents);
        int childMass = children.Item1.Mass + children.Item2.Mass;

        Assert.True(parentMass >= childMass);

        List<Portion> childPortions = new(children.Item1.portions);
        childPortions.AddRange(children.Item2.portions);

        // Ensure child mass <= parentMass for good reasons
        if (childMass < parentMass)
        {
            bool isValid = false;
            foreach (Portion p in childPortions)
            {
                if (p.Mass == Preferences.Instance.maxPortionMass)
                {
                    // Explains why childMass < parentMass; the child got too much of this Food type from
                    // the crossover, and had to be sliced.
                    isValid = true; break;
                }
            }

            Logger.Warn("childMass was less than parentMass.");
            Assert.True(isValid);
        }

        List<Portion> parentPortions = new(parents.Item1.portions);
        parentPortions.AddRange(parents.Item2.portions);

        List<Food> parentFoods = parentPortions.Select(x => x.food).ToList();

        // Ensure all child portions share a Food with one of the parents
        foreach (Portion p in childPortions)
        {
            Assert.True(parentFoods.Contains(p.food));
        }
    }
}
