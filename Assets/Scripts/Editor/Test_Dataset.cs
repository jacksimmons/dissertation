using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


/// <summary>
/// Tests for reading the csv dataset.
/// </summary>
public class Test_Dataset
{
    /// <summary>
    /// Gets the list of foods contained in the testing dataset (TestProximates.csv).
    /// </summary>
    /// <returns>A list of Food objects, which are the testing data.</returns>
    private List<Food> GetTestFoods()
    {
        string file = File.ReadAllText(Application.dataPath + "/Scripts/Editor/TestProximates.csv");
        List<Food> foods = new DatasetReader().ReadFoods(file, Preferences.Instance);
        return foods;
    }


    // Reference used:
    // https://learn.microsoft.com/en-us/troubleshoot/developer/visualstudio/csharp/language-compilers/read-write-text-file

    /// <summary>
    /// Tests all parameters with default values.
    /// </summary>
    [Test]
    public void NormalTest()
    {
        List<Food> foods = GetTestFoods();
        Food food = foods[0];

        Assert.AreEqual(food.Name, "Test1", $"Test name was incorrect.");
        Assert.AreEqual(food.Description, "Desc1", $"Desc name was incorrect.");
        Assert.AreEqual(food.FoodGroup, "Group1", $"Group name was incorrect.");
        Assert.AreEqual(food.Reference, "Ref1", $"Ref name was incorrect.");
        Assert.AreEqual(food.Proximates[Proximate.Protein], 10, $"Protein was incorrect.");
        Assert.AreEqual(food.Proximates[Proximate.Fat], 20, $"Fat was incorrect.");
        Assert.AreEqual(food.Proximates[Proximate.Carbs], 30, $"Carbs was incorrect.");
        Assert.AreEqual(food.Proximates[Proximate.Kcal], 340, $"Kcal was incorrect.");
        Assert.AreEqual(food.Proximates[Proximate.Sugar], 25, $"Sugar was incorrect.");
        Assert.AreEqual(food.Proximates[Proximate.SatFat], 9, $"SatFat was incorrect.");
        Assert.AreEqual(food.Proximates[Proximate.TransFat], 1, $"TransFat was incorrect.");
    }


    /// <summary>
    /// Performs a test for a given value on a given Food object.
    /// </summary>
    private void EqualFloatTest(Food food, float value)
    {
        Assert.AreEqual(food.Proximates[Proximate.Protein], value, $"Protein was incorrect.");
        Assert.AreEqual(food.Proximates[Proximate.Fat], value, $"Fat was incorrect.");
        Assert.AreEqual(food.Proximates[Proximate.Carbs], value, $"Carbs was incorrect.");
        Assert.AreEqual(food.Proximates[Proximate.Kcal], value, $"Kcal was incorrect.");
        Assert.AreEqual(food.Proximates[Proximate.Sugar], value, $"Sugar was incorrect.");
        Assert.AreEqual(food.Proximates[Proximate.SatFat], value, $"SatFat was incorrect.");
        Assert.AreEqual(food.Proximates[Proximate.TransFat], value, $"TransFat was incorrect.");
    }


    /// <summary>
    /// Tests all float parameters with edge-case values, likely to cause issues.
    /// </summary>
    [Test]
    public void BoundaryTest()
    {
        List<Food> foods = GetTestFoods();

        EqualFloatTest(foods[1], 0);
        EqualFloatTest(foods[2], 0);
    }


    /// <summary>
    /// Tests all float parameters with erroneous values, which should not be accepted.
    /// </summary>
    [Test]
    public void ErroneousTest()
    {
        List<Food> foods = GetTestFoods();

        // Assert that the three erroneous foods were not added.
        Assert.IsTrue(foods.Count == 3);
    }
}
