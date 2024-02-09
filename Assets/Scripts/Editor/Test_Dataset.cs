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
    /// Gets the list of foods contained in the testing dataset (Test*.csv).
    /// </summary>
    /// <returns>A list of Food objects, which are the testing data.</returns>
    private List<Food> GetTestFoods()
    {
        Dictionary<DatasetFile, string> files = new()
        {
            { DatasetFile.Proximates, File.ReadAllText(Application.dataPath + "/Scripts/Editor/TestProximates.csv") },
            { DatasetFile.Inorganics, File.ReadAllText(Application.dataPath + "/Scripts/Editor/TestInorganics.csv") }
        };
        List<Food> foods = new DatasetReader(Preferences.Instance, "Scripts/Editor/TestProximates.csv", "/Scripts/Editor/TestInorganics.csv",
            "/Scripts/Editor/TestVitamins.csv").ReadFoods();
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
        Assert.AreEqual(10, food.Nutrients[Nutrient.Protein], $"Protein was incorrect.");
        Assert.AreEqual(20, food.Nutrients[Nutrient.Fat], $"Fat was incorrect.");
        Assert.AreEqual(30, food.Nutrients[Nutrient.Carbs], $"Carbs was incorrect.");
        Assert.AreEqual(340, food.Nutrients[Nutrient.Kcal], $"Kcal was incorrect.");
        Assert.AreEqual(25, food.Nutrients[Nutrient.Sugar], $"Sugar was incorrect.");
        Assert.AreEqual(9, food.Nutrients[Nutrient.SatFat], $"SatFat was incorrect.");
        Assert.AreEqual(1, food.Nutrients[Nutrient.TransFat], $"TransFat was incorrect.");

        Assert.AreEqual(100, food.Nutrients[Nutrient.Calcium], $"Calcium was incorrect.");
        Assert.AreEqual(30, food.Nutrients[Nutrient.Iron], $"Iron was incorrect.");
        Assert.AreEqual(20, food.Nutrients[Nutrient.Iodine], $"Iodine was incorrect.");

    }


    /// <summary>
    /// Performs a test for a given value on a given Food object.
    /// </summary>
    private void EqualFloatTest(Food food, float value)
    {
        foreach (Nutrient nutrient in Nutrients.Values)
        {
            Assert.AreEqual(value, food.Nutrients[nutrient], $"{nutrient} was incorrect.");
        }
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


    [Test]
    public void ReadDatasetThrowsNoError()
    {
        List<Food> foods = new DatasetReader(Preferences.Instance).ReadFoods();
        Assert.IsTrue(foods.Count > 0);
    }
}
