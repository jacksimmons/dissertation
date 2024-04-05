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
        Preferences.Instance.CalculateDefaultConstraints();

        AlgGA ga = (AlgGA)Algorithm.Build(typeof(AlgGA));
        ga.Init();

        NormalTest(ga);
        BoundaryTest(ga);
        ErroneousTest(ga);
        FitnessTest(ga);

        Saving.LoadPreferences();
    }


    private void NormalTest(AlgGA ga)
    {
        // Selection
        List<Day> gaDays = new(ga.Population);
        SelectionTest(ga, gaDays);

        // Crossover
        gaDays = new(ga.Population);
        CrossoverTest(ga, ga.PerformPairSelection(gaDays, true));
    }


    private void BoundaryTest(AlgGA ga)
    {
        // Selection of just 2
        List<Day> gaDays = ga.Population.ToList().GetRange(0, 2);
        SelectionTest(ga, gaDays);

        // Selection of just 1 (should return itself)
        Day gaDay = ga.Population.ToList()[0];
        gaDays = new() { gaDay };
        SelectionTest(ga, gaDays);

        // Crossover with no portions
        Day parentA = new(ga); // No portions
        Day parentB = new(ga);
        FoodData food = new();
        food.Nutrients[0] = 300;
        CrossoverTest(ga, ga.PerformPairSelection(new() { parentA, parentB }, true));
    }


    private void ErroneousTest(AlgGA ga)
    {
        // Selection of 0
        Assert.Throws(typeof(Exception), () => SelectionTest(ga, new()));
    }


    private void SelectionTest(AlgGA ga, List<Day> gaDays)
    {
        if (gaDays.Count == 1)
        {
            Assert.True(ga.TournamentSelection(gaDays, true) == gaDays[0]);
            Assert.True(ga.TournamentSelection(gaDays, false) == gaDays[0]);

            Assert.True(ga.RankSelection(gaDays, true) == gaDays[0]);
            Assert.True(ga.RankSelection(gaDays, false) == gaDays[0]);
        }
        else
        {
            Assert.NotNull(ga.TournamentSelection(gaDays, true));
            Assert.NotNull(ga.TournamentSelection(gaDays, false));

            Assert.NotNull(ga.RankSelection(gaDays, true));
            Assert.NotNull(ga.RankSelection(gaDays, false));
        }
    }


    private void CrossoverTest(AlgGA ga, Tuple<Day, Day> parents)
    {
        // For every crossover, all of the childrens' Foods must be a member of one of the parents.
        // Also, the childrens' mass must equal the parents' mass, unless the max portion mass was reached

        int parentMass = parents.Item1.Mass + parents.Item2.Mass;

        Tuple<Day, Day> children = ga.Crossover(parents);
        int childMass = children.Item1.Mass + children.Item2.Mass;

        Assert.True(parentMass >= childMass);

        List<Portion> childPortions = new(children.Item1.portions);
        childPortions.AddRange(children.Item2.portions);

        // Ensure child mass <= parentMass for good reasons
        if (childMass < parentMass)
        {
            Assert.Fail("childMass was less than parentMass.");
        }

        List<Portion> parentPortions = new(parents.Item1.portions);
        parentPortions.AddRange(parents.Item2.portions);

        List<Food> parentFoods = parentPortions.Select(x => x.FoodType).ToList();

        // Ensure all child portions share a Food with one of the parents
        foreach (Portion p in childPortions)
        {
            Assert.True(parentFoods.Contains(p.FoodType));
        }
    }


    private void FitnessTest(AlgGA alg)
    {
        foreach (Day day in alg.Population)
        {
            float fitness = day.TotalFitness.Value;

            // Fitness can only be positive or 0.
            Assert.IsTrue(fitness >= 0);

            for (int i = 0; i < Nutrient.Count; i++)
            {
                // Fitness for each nutrient can only be positive or 0.
                float nutrientFitness = alg.Constraints[i].GetFitness(day.GetNutrientAmount((ENutrient)i));
                Assert.IsTrue(nutrientFitness >= 0);
            }
        }
    }
}