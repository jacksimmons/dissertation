using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.XR;


/// <summary>
/// Tests for Genetic Algorithm functions.
/// </summary>
public class Test_GA : Test_HasPopulation
{
    [Test]
    public void RunTests()
    {
        Preferences.Instance.Reset();

        AlgGA ga = (AlgGA)Algorithm.Build(typeof(AlgGA));
        ga.Init();

        NormalTest(ga);
        BoundaryTest(ga);
        ErroneousTest();

        // Try fitness test for summed fitness
        FitnessTest(ga);

        // Build a new GA with pareto dominance, try the fitness test again
        Preferences.Instance.fitnessApproach = EFitnessApproach.ParetoDominance;
        ga = (AlgGA)Algorithm.Build(typeof(AlgGA));
        FitnessTest(ga);

        Saving.LoadPreferences();
    }


    /// <summary>
    /// Tests GA methods under normal conditions.
    /// </summary>
    private void NormalTest(AlgGA ga)
    {
        // Selection
        List<Day> gaDays = new(ga.Population);
        SelectionTest(ga, gaDays);
        Tuple<Day, Day> parents = ga.PairSelection(gaDays, true);

        // Mutation
        Tuple<Day, Day> children = MutationTest(ga, parents);

        // Crossover
        CrossoverTest(ga, children);
    }


    /// <summary>
    /// Tests GA methods with boundary parameters.
    /// </summary>
    private void BoundaryTest(AlgGA ga)
    {
        // Selection of just 2
        List<Day> gaDays = ga.Population.ToList().GetRange(0, 2);
        SelectionTest(ga, gaDays);

        // Selection of just 1 (should return itself)
        gaDays = gaDays.GetRange(0, 1);
        SelectionTest(ga, gaDays);

        // Mutation with no portions
        Day mutParentA = new(ga);
        MutationTest(ga, new(mutParentA, new(mutParentA)));

        // Crossover with no portions
        Day crsParentA = new(ga);
        CrossoverTest(ga, new(crsParentA, new(crsParentA)));

        // Mutation with all portions (to see if duplicate portion gets mutated correctly)
        // In most instances, expect two "Added duplicate portion" messages in the Unity console.
        Preferences.Instance.minPortionMass = Preferences.Instance.mutationMassChangeMax + 1; // So adding a portion is distinguishable from mutation
        Preferences.Instance.addOrRemovePortionMutationProb = 1; // Ensure an add/remove mutation takes place
        Preferences.Instance.changePortionMassMutationProb = 0; // Ensure no mass changes occur
        ga = (AlgGA)Algorithm.Build(typeof(AlgGA));
        Day mutAllParentA = new(ga);

        // Add all possible unique portions
        for (int i = Preferences.Instance.minPortionMass; i < Preferences.Instance.maxPortionMass; i++)
        {
            for (int j = 0; j < ga.Foods.Count; j++)
            {
                mutAllParentA.AddPortion(new(ga.Foods[j], i));
            }
        }

        Tuple<Day, Day> children = MutationTest(ga, new(mutAllParentA, new(mutAllParentA)));

        // Number of portions in children can only be the same as the parent or one less.
        int[] validChildPortionCounts = new int[] { mutAllParentA.portions.Count, mutAllParentA.portions.Count - 1 };
        Assert.True(validChildPortionCounts.Contains(children.Item1.portions.Count));
        Assert.True(validChildPortionCounts.Contains(children.Item2.portions.Count));

        // Statistical anomalies - if these messages occur regularly (if at all), something isn't right.
        if (children.Item1.portions.Count == validChildPortionCounts[0])
        {
            Logger.Log("Child 1 was very lucky and re-added the just-removed portion during mutation.");
        }

        if (children.Item2.portions.Count == validChildPortionCounts[0])
        {
            Logger.Log("Child 2 was very lucky and re-added the just-removed portion during mutation.");
        }
    }


    /// <summary>
    /// Tests erroneous conditions are caught by GA methods.
    /// </summary>
    private void ErroneousTest()
    {
        // Selection from an empty list; test on both selection methods
        Preferences.Instance.selectionMethod = ESelectionMethod.Tournament;
        AlgGA ga = (AlgGA)Algorithm.Build(typeof(AlgGA));
        Assert.Throws(typeof(WarnException), () => ga.Selection(new(), true));
        Assert.Throws(typeof(WarnException), () => ga.Selection(new(), false));

        Preferences.Instance.selectionMethod = ESelectionMethod.Rank;
        ga = (AlgGA)Algorithm.Build(typeof(AlgGA));
        Assert.Throws(typeof(WarnException), () => ga.Selection(new(), true));
        Assert.Throws(typeof(WarnException), () => ga.Selection(new(), false));
    }


    /// <summary>
    /// Ensures selection works as intended for both selection methods.
    /// </summary>
    /// <param name="gaDays">The selection list.</param>
    private void SelectionTest(AlgGA ga, List<Day> gaDays)
    {
        if (gaDays.Count == 1)
        {
            Assert.True(ga.TournamentSelection(gaDays, true) == gaDays[0]);
            Assert.True(ga.TournamentSelection(gaDays, false) == gaDays[0]);

            // Sort for rank selection
            gaDays.Sort();
            Assert.True(ga.RankSelection(gaDays, true) == gaDays[0]);
            Assert.True(ga.RankSelection(gaDays, false) == gaDays[0]);
        }
        else
        {
            Assert.NotNull(ga.TournamentSelection(gaDays, true));
            Assert.NotNull(ga.TournamentSelection(gaDays, false));

            // Sort for rank selection
            gaDays.Sort();
            Assert.NotNull(ga.RankSelection(gaDays, true));
            Assert.NotNull(ga.RankSelection(gaDays, false));
        }
    }


    private Tuple<Day, Day> MutationTest(AlgGA ga, Tuple<Day, Day> parents)
    {
        // The children that result from a mutation must be the same as their parents, with maximum deviation
        // being one portion added / removed, and changed mass on all portions.
        Tuple<Day, Day> children = new(new(parents.Item1), new(parents.Item2));

        Logger.Log($"Child 1 before: {children.Item1.portions.Count}");
        Logger.Log($"Child 2 before: {children.Item2.portions.Count}");

        ga.MutateDay(children.Item1);
        Logger.Log($"Child 1 after: {children.Item1.portions.Count}");
        ga.MutateDay(children.Item2);
        Logger.Log($"Child 2 after: {children.Item2.portions.Count}");

        // Counts the number of additional portions a has over b.
        int CountAddedPortions(Day a, Day b)
        {
            int addedPortions = 0;
            foreach (Portion pa in a.portions)
            {
                foreach (Portion pb in b.portions)
                {
                    // A portion match was found, so skip incrementing uniquePortions,
                    // while still breaking out of the inner loop.
                    if (pa.FoodType == pb.FoodType)
                    {
                        float dPortionMass = MathF.Abs(pa.Mass - pb.Mass);

                        // Erroneous behaviour that could occur with invalid code. An exhaustive check for this case to not flag
                        // a false "too many portions added" error which may occur if not captured here.
                        if (dPortionMass > 0 && dPortionMass < Preferences.Instance.mutationMassChangeMin)
                            throw new Exception("A mutation occurred, but it was smaller than allowed.");

                        // If the difference is 0 (no mutation), or a portion mass mutation occurred (a mass mutation in the range
                        // [mutMin, mutMax]) then no portions were added and say "not added"
                        if (MathTools.Approx(dPortionMass, 0) || (dPortionMass <= Preferences.Instance.mutationMassChangeMax
                            && dPortionMass >= Preferences.Instance.mutationMassChangeMin))
                            goto notAdded;

                        // A "mutation" occurred in range (mutMax, inf), i.e. a portion must have been added.
                        Logger.Log("Added duplicate portion in mutation.");
                    }
                }
                addedPortions++;

            notAdded:
                continue;
            }
            return addedPortions;
        }


        // Ensure that the child has a maximum of one portion more than the parent.
        void CheckParentAndChild(Day p, Day c)
        {
            Assert.True(CountAddedPortions(c, p) <= 1);
        }

        CheckParentAndChild(parents.Item1, children.Item1);
        CheckParentAndChild(parents.Item2, children.Item2);
        return children;
    }


    private void CrossoverTest(AlgGA ga, Tuple<Day, Day> parents)
    {
        Tuple<Day, Day> children = ga.Crossover(parents);
        int parentMass = parents.Item1.Mass + parents.Item2.Mass;
        int childMass = children.Item1.Mass + children.Item2.Mass;

        // The sum of the childrens' masses must equal the sum of the parents' masses.
        Assert.True(parentMass == childMass);

        //
        // Assert: Storing the mass for each Food in a dictionary gives parentFoodToMass = childFoodToMass
        //

        // Get a list of all portions the child Days contain.
        List<Portion> childPortions = new(children.Item1.portions);
        childPortions.AddRange(children.Item2.portions);

        // Get the amount of mass for each food type the child Days contain.
        Dictionary<Food, int> childFoodToMass = new();
        foreach (Portion p in childPortions)
        {
            if (!childFoodToMass.ContainsKey(p.FoodType))
                childFoodToMass.Add(p.FoodType, p.Mass);
            else
                childFoodToMass[p.FoodType] += p.Mass;
        }

        // Do the same for parents
        List<Portion> parentPortions = new(parents.Item1.portions);
        parentPortions.AddRange(parents.Item2.portions);

        Dictionary<Food, int> parentFoodToMass = new();
        foreach (Portion p in parentPortions)
        {
            if (!parentFoodToMass.ContainsKey(p.FoodType))
                parentFoodToMass.Add(p.FoodType, p.Mass);
            else
                parentFoodToMass[p.FoodType] += p.Mass;
        }

        // Compare the two dictionaries
        var childKeys = childFoodToMass.Keys;
        var parentKeys = parentFoodToMass.Keys;

        // Check that childFoodToMass ⊆ parentFoodToMass
        foreach (var key in childKeys)
        {
            if (!parentKeys.Contains(key))
            {
                Assert.Fail("No matching parent key.");
            }

            Assert.True(childFoodToMass[key] == parentFoodToMass[key]);
        }

        // Check that the dictionaries have the same number of keys
        Assert.True(childKeys.Count == parentKeys.Count);

        // Assertations succeed means:
        // childFoodToMass ⊆ parentFoodToMass && dicts have the same number of keys, therefore:
        // childFoodToMass[k] == parentFoodToMass[k] for k in either dict.
    }
}