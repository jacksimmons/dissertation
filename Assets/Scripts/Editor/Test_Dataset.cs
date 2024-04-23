using NUnit.Framework;
using System.Collections.Generic;


/// <summary>
/// Tests for reading the csv dataset.
/// </summary>
public class Test_Dataset
{
    public static DatasetReader Reader => new(new(), "Editor/TestProximates", "Editor/TestInorganics", "Editor/TestVitamins");

    /// <summary>
    /// Manual overwrite of the number of rows in the dataset, so the program
    /// knows when to stop reading rows specifically for the test dataset.
    /// This will therefore also ignore the Test_Preferences rows.
    /// </summary>
    private const int TEST_DATASET_TOTAL_ROWS = 9;


    // Reference used:
    // https://learn.microsoft.com/en-us/troubleshoot/developer/visualstudio/csharp/language-compilers/read-write-text-file

    /// <summary>
    /// Tests all parameters with default values.
    /// </summary>
    [Test]
    public void NormalTest()
    {
        DatasetReader dr = Reader;
        dr.ProcessFoods(TEST_DATASET_TOTAL_ROWS);
        Food food = dr.Output[0];

        Assert.AreEqual(food.Name, "Test1", $"Test name was incorrect.");

        float[] nutrients = new float[]
        // Proximates
        { 10, 20, 30, 340, 25, 9, 1,
        // Inorganics
        100, 20, 30,
        // Vitamins
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11};

        for (int i = 0; i < Constraint.Count - 1; i++) // Cost constraint is not obtained from the dataset
        {
            Assert.AreEqual(nutrients[i], food.Nutrients[i], $"{Constraint.Values[i]} was incorrect.");
        }
    }


    /// <summary>
    /// A test applied to the real dataset.
    /// </summary>
    [Test]
    public void AppliedTest()
    {
        DatasetReader dr = Reader;
        dr.ProcessFoods();

        // These unrestrictive preferences should yield at least 100 foods
        Assert.IsTrue(dr.Output.Count > 100);
    }


    /// <summary>
    /// Performs a test for a given value on a given Food object.
    /// </summary>
    private void EqualFloatTest(Food food, float value)
    {
        for (int i = 0; i < Constraint.Count; i++)
        {
            Assert.AreEqual(value, food.Nutrients[i], $"{Constraint.Values[i]} was incorrect.");
        }
    }


    /// <summary>
    /// Tests all float parameters with edge-case values, likely to cause issues.
    /// </summary>
    [Test]
    public void BoundaryTest()
    {
        DatasetReader dr = Reader;
        dr.ProcessFoods(TEST_DATASET_TOTAL_ROWS);

        EqualFloatTest(dr.Output[1], 0);
        EqualFloatTest(dr.Output[2], 0);
    }


    /// <summary>
    /// Tests all float parameters with erroneous values, which should not be accepted.
    /// </summary>
    [Test]
    public void ErroneousTest()
    {
        DatasetReader dr = Reader;
        dr.ProcessFoods(TEST_DATASET_TOTAL_ROWS);

        // Assert that the three erroneous foods were not added.
        Assert.IsTrue(dr.Output.Count == 3);
    }
}
