using NUnit.Framework;

public abstract class Test_HasPopulation
{
    protected void FitnessTest(Algorithm alg)
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