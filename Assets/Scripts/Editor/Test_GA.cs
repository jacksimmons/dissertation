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
        Preferences.Instance.Reset();

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
        Tuple<Day, Day> parents = ga.PerformPairSelection(gaDays, true);

        // Mutation
        Tuple<Day, Day> children = MutationTest(ga, parents);

        // Crossover
        CrossoverTest(ga, children);
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
    }


    private void ErroneousTest(AlgGA ga)
    {
        // Selection of no days
        Assert.Throws(typeof(WarnException), () => SelectionTest(ga, new()));

        // Mutation with no portions
        Day mutParentA = new(ga);
        Day mutParentB = new(ga);
        Assert.Throws(typeof(WarnException), () => MutationTest(ga, new(mutParentA, mutParentB)));

        // Crossover with no portions
        Day parentA = new(ga);
        Day parentB = new(ga);
        Assert.Throws(typeof(WarnException), () => CrossoverTest(ga, new(mutParentA, mutParentB)));
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


    private Tuple<Day, Day> MutationTest(AlgGA ga, Tuple<Day, Day> parents)
    {
        // The children that result from a mutation must be the same as their parents, with maximum deviation
        // being one portion added / removed, and changed mass on all portions.
        Tuple<Day, Day> children = new(new(parents.Item1), new(parents.Item2));

        ga.MutateDay(children.Item1);
        ga.MutateDay(children.Item2);

        int CountUniquePortions(Day a, Day b)
        {
            int uniquePortions = 0;
            foreach (Portion pa in a.portions)
            {
                foreach (Portion pb in b.portions)
                {
                    // A portion match was found, so skip incrementing uniquePortions,
                    // while still breaking out of the inner loop.
                    if (pa.FoodType == pb.FoodType)
                    {
                        goto noDeviation;
                    }
                }
                uniquePortions++;

            noDeviation:
                continue;
            }
            return uniquePortions;
        }


        // Ensure that the child has a maximum of one portion more than the parent.
        void CheckParentAndChild(Day p, Day c)
        {
            Assert.True(CountUniquePortions(c, p) <= 1);
        }


        CheckParentAndChild(parents.Item1, children.Item1);
        CheckParentAndChild(parents.Item2, children.Item2);
        return children;
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

            for (int i = 0; i < Constraint.Count; i++)
            {
                // Fitness for each nutrient can only be positive or 0.
                float nutrientFitness = alg.Constraints[i].GetFitness(day.GetConstraintAmount((EConstraintType)i));
                Assert.IsTrue(nutrientFitness >= 0);
            }
        }
    }
}