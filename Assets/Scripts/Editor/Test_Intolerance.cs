using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;

/// <summary>
/// Tests for user intolerances - ensuring the necessary food groups are removed
/// from the collection.
/// </summary>
public class Test_Intolerance
{
    /// <summary>
    /// Gets the list of foods contained in the whole dataset (Nutrient.csv).
    /// Then asserts that all foods obtained are valid for the given preferences.
    /// (DatasetReader should do this automatically)
    /// </summary>
    private void AssertAllFoodsValid(Preferences prefs)
    {
        DatasetReader dr = new(prefs);
        List<Food> foods = dr.ProcessFoods();

        foreach (Food food in foods)
        {
            Assert.IsTrue(prefs.IsFoodAllowed(food.FoodGroup, food.Name));
        }
    }


    /// <summary>
    /// Tests vegan preferences.
    /// </summary>
    [Test]
    public void VeganTest()
    {
        Preferences prefs = new();
        prefs.MakeVegan();

        // Assert MakeVegan works.
        Assert.IsFalse(prefs.eatsLandMeat);
        Assert.IsFalse(prefs.eatsSeafood);
        Assert.IsFalse(prefs.eatsAnimalProduce);

        AssertAllFoodsValid(prefs);
    }


    /// <summary>
    /// Tests vegetarian preferences.
    /// </summary>
    [Test]
    public void VegetarianTest()
    {
        Preferences prefs = new();
        prefs.MakeVegetarian();

        // Assert MakeVegetarian works
        Assert.IsFalse(prefs.eatsLandMeat);
        Assert.IsFalse(prefs.eatsSeafood);

        AssertAllFoodsValid(prefs);
    }


    /// <summary>
    /// Tests pescatarian preferences.
    /// </summary>
    [Test]
    public void PescatarianTest()
    {
        Preferences prefs = new();
        prefs.MakePescatarian();

        // Assert MakePescatarian works
        Assert.IsFalse(prefs.eatsLandMeat);

        AssertAllFoodsValid(prefs);
    }


    [Test]
    public void BannedFoodsNormalTest()
    {
        Preferences.Instance.bannedFoodKeys = new();

        // Get list of foods before banning
        List<Food> foods = Algorithm.Build(typeof(AlgSFGA)).Foods.ToList();
        int numFoodsBeforeBan = foods.Count;

        // Ban 2nd and 3rd foods
        Food bannedA = foods[2];
        Food bannedB = foods[3];
        Preferences.Instance.bannedFoodKeys.Add(bannedA.CompositeKey);
        Preferences.Instance.bannedFoodKeys.Add(bannedB.CompositeKey);

        // Get the foods with bans applied
        List<Food> newFoods = Algorithm.Build(typeof(AlgSFGA)).Foods.ToList();
        int numFoodsAfterBan = newFoods.Count;

        // First foods list contains the banned foods
        Assert.True(FoodsListContainsFoods(foods, bannedA, bannedB));

        // Second foods list doesn't contain the banned foods
        Assert.False(FoodsListContainsFoods(newFoods, bannedA, bannedB));

        // Also numFoods from 2nd = numFoods from 1st - 2
        Assert.True(numFoodsAfterBan == numFoodsBeforeBan - 2);
    }


    private bool FoodsListContainsFoods(List<Food> foods, Food a, Food b)
    {
        bool containsA = false;
        bool containsB = false;
        foreach (Food food in foods)
        {
            if (containsA && containsB) break;

            if (food.CompositeKey == a.CompositeKey)
                containsA = true;
            if (food.CompositeKey == b.CompositeKey)
                containsB = true;
        }
        return containsA && containsB;
    }


    [Test]
    public void BannedFoodsErroneousTest()
    {
        Assert.False(true);
    }
}