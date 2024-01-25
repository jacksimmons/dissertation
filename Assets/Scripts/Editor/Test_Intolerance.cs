using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using UnityEngine;

/// <summary>
/// Tests for user intolerances - ensuring the necessary food groups are removed
/// from the collection.
/// </summary>
public class Test_Intolerance
{
    /// <summary>
    /// Gets the list of foods contained in the whole dataset (Proximates.csv).
    /// Then asserts that all foods obtained are valid for the given preferences.
    /// (DatasetReader should do this automatically)
    /// </summary>
    private void AssertAllFoodsValid(Preferences prefs)
    {
        string file = File.ReadAllText(Application.dataPath + "/Proximates.csv");
        List<Food> foods = new DatasetReader().ReadFoods(file, prefs);
        Debug.Log($"Number of foods permitted: {foods.Count}");

        foreach (Food food in foods)
        {
            Assert.IsTrue(prefs.IsFoodGroupAllowed(food.FoodGroup));
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
}