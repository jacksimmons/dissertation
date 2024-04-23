using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using static Preferences;


/// <summary>
/// Tests for user preferences.
/// </summary>
public class Test_Preferences
{
    /// <summary>
    /// Gets the list of foods contained in the whole dataset (Nutrient.csv).
    /// Then asserts that all foods obtained are valid for the given preferences.
    /// (DatasetReader should do this automatically)
    /// 
    /// For an accepted food to be valid, it is ensured to be:
    /// - enabled
    /// - such that all foods that miss a compulsory constraint are rejected.
    /// </summary>
    private void AssertDatasetOutputAllowed(Preferences prefs)
    {
        DatasetReader dr = new(prefs);
        dr.ProcessFoods();

        foreach (Food food in dr.Output)
        {
            Assert.IsTrue(prefs.IsFoodAllowed(food.FoodGroup, food.Name, food.CompositeKey));
        }
    }


    /// <summary>
    /// Tests functionality of the energy goal preferences.
    /// 
    /// Doesn't test erroneous values, as they are caught by the UI (UI testing).
    /// </summary>
    [Test]
    public void EnergyGoalTest()
    {
        // Energy surplus test
        EnergySurplusOrDefecitTest(true);
        // Energy defecit test
        EnergySurplusOrDefecitTest(false);
    }


    /// <summary>
    /// Ensures that Normal and Boundary cases for energy recommendations work.
    /// </summary>
    /// <param name="surplusElseDefecit">`true` to test surplus, `false` to test defecit.</param>
    private void EnergySurplusOrDefecitTest(bool surplusElseDefecit)
    {
        void TestEnergyGoals(float exerciseKcal, float changeKcal)
        {
            // Setup preferences
            Preferences prefs = new();
            prefs.gainElseLoseWeight = surplusElseDefecit;
            prefs.dailyExerciseKcal = exerciseKcal;
            prefs.dailyLoseOrGainKcal = changeKcal;

            // Setup energy constraint (and others)
            prefs.CalculateDefaultConstraints();

            // Amount of calories to maintain mass; BMR + exercise
            float maintenance = prefs.BMR + exerciseKcal;

            // Sign is positive if they want to gain/maintain weight, negative if they want to lose weight.
            float sign = surplusElseDefecit ? 1 : -1;

            // Expected: Algorithm suggests BMR + exercise + (sign) * changeKcal.
            float goal = prefs.constraints[(int)EConstraintType.Kcal].Goal;
            Assert.AreEqual(goal, maintenance + sign * changeKcal, MathTools.EPSILON);

            Logger.Log($"Match: {maintenance + sign * changeKcal} and {goal}");
        }

        // Normal
        TestEnergyGoals(100, 200);

        // Boundary
        TestEnergyGoals(0, 1);
        TestEnergyGoals(1, 0);
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

        AssertDatasetOutputAllowed(prefs);
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

        AssertDatasetOutputAllowed(prefs);
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

        AssertDatasetOutputAllowed(prefs);
    }


    /// <summary>
    /// Tests functionality of the enabling/disabling of foods.
    /// </summary>
    [Test]
    public void BannedFoodsNormalTest()
    {
        // Load sensible constraints to test on
        Preferences prefs = new();

        prefs.customFoodSettings = new();

        // Get list of foods before banning
        List<Food> foods = Algorithm.Build(typeof(AlgGA)).Foods.ToList();
        int numFoodsBeforeBan = foods.Count;

        // Ban 2nd and 3rd foods
        Food bannedA = foods[2];
        Food bannedB = foods[3];
        prefs.customFoodSettings.Add(new() { Key = bannedA.CompositeKey, Banned = true });
        prefs.customFoodSettings.Add(new() { Key = bannedB.CompositeKey, Banned = true });

        // Get the foods with bans applied
        List<Food> newFoods = Algorithm.Build(typeof(AlgGA)).Foods.ToList();
        int numFoodsAfterBan = newFoods.Count;

        // First foods list contains the banned foods
        Assert.True(DoesFoodsListContain(foods, bannedA, bannedB));

        // Second foods list doesn't contain the banned foods
        Assert.False(DoesFoodsListContain(newFoods, bannedA, bannedB));

        // Also numFoods from 2nd = numFoods from 1st - 2
        Assert.True(numFoodsAfterBan == numFoodsBeforeBan - 2);
    }


    /// <summary>
    /// Checks whether a list of Food objects contains both provided Food
    /// objects, by composite key.
    /// </summary>
    private bool DoesFoodsListContain(List<Food> foods, Food a, Food b)
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


    private const int FIRST_PROXIMATE_IN_FILE = (int)EConstraintType.Protein;
    private const int LAST_PROXIMATE_IN_FILE = (int)EConstraintType.TransFat;
    private const int FIRST_INORGANIC_IN_FILE = (int)EConstraintType.Calcium;
    private const int LAST_INORGANIC_IN_FILE = (int)EConstraintType.Iodine;
    private const int FIRST_VITAMIN_IN_FILE = (int)EConstraintType.VitA;
    private const int LAST_VITAMIN_IN_FILE = (int)EConstraintType.VitC;
    private const int TEST_PREFERENCES_TOTAL_ROWS = 18;
    /// <summary>
    /// Ensures the dataset accepts entries correctly based for non-compulsory constraints.
    /// </summary>
    [Test]
    public void MissingConstraintAcceptanceTest()
    {
        OneConstraintMissingTest(FIRST_PROXIMATE_IN_FILE, "TestM1");
        OneConstraintMissingTest(LAST_PROXIMATE_IN_FILE, "TestM2");
        OneConstraintMissingTest(FIRST_INORGANIC_IN_FILE, "TestM4");
        OneConstraintMissingTest(LAST_INORGANIC_IN_FILE, "TestM5");
        OneConstraintMissingTest(FIRST_VITAMIN_IN_FILE, "TestM7");
        OneConstraintMissingTest(LAST_VITAMIN_IN_FILE, "TestM8");

        AllConstraintsInFileMissingTest((int)EConstraintType.Protein, (int)EConstraintType.TransFat, "TestM3");
        AllConstraintsInFileMissingTest((int)EConstraintType.Calcium, (int)EConstraintType.Iron, "TestM6");
        AllConstraintsInFileMissingTest((int)EConstraintType.VitA, (int)EConstraintType.VitK1, "TestM9");
    }


    /// <summary>
    /// Assert that the non-compulsory nutrient settings permitted the selected food by name.
    /// </summary>
    /// <param name="constraint"></param>
    /// <param name="name"></param>
    private void ConstraintMissingTest(Preferences prefs, string name)
    {
        DatasetReader reader = new(prefs, "Editor/TestProximates", "Editor/TestInorganics", "Editor/TestVitamins");
        reader.ProcessFoods(TEST_PREFERENCES_TOTAL_ROWS);

        // Ensure the dataset contains the row with a missing value for the allowed-missing constraint.
        bool rowWithMissingConstraintFound = false;
        for (int i = 0; i < reader.Output.Count; i++)
        {
            if (reader.Output[i].Name == name)
            {
                rowWithMissingConstraintFound = true;
                break;
            }
        }

        Assert.That(rowWithMissingConstraintFound);
    }


    private void OneConstraintMissingTest(int constraint, string name)
    {
        Preferences prefs = new();
        prefs.acceptMissingNutrientValue[constraint] = true;
        ConstraintMissingTest(prefs, name);
    }


    /// <summary>
    /// Test if all Proximates, Inorganics or Vitamins being missing can be permitted, given that each of the constraints
    /// in that type are set to non-compulsory.
    /// </summary>
    /// <param name="constraint"></param>
    /// <param name="name"></param>
    private void AllConstraintsInFileMissingTest(int firstConstraintInEnum, int lastConstraintInEnum, string name)
    {
        Preferences prefs = new();

        // Accept all missing constraints
        for (int i = firstConstraintInEnum; i <= lastConstraintInEnum; i++)
        {
            prefs.acceptMissingNutrientValue[i] = true;
        }

        // Ensure the food with all missing constraints was still accepted
        ConstraintMissingTest(prefs, name);
    }
}