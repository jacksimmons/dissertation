using System;
using System.Linq;
using Random = UnityEngine.Random;


/// <summary>
/// Takes a dynamic search space exploration approach to the ACO.
/// Every stagnation iterations, replaces the worst portion and adds a new one.
/// </summary>
public class AlgACODynamic : AlgACO
{
    /// <summary>
    /// Remove the worst food in the experiment, and replace it with a new random
    /// one from the dataset.
    /// 
    /// The "worst" is calculated by selecting the vertex with the lowest total
    /// pheromone incoming from all other vertices.
    /// 
    /// Generally improves algorithm performance.
    /// </summary>
    protected override void UpdateSearchSpace(float[,] fitnesses, float[,] pheromone, Portion[] vertices)
    {
        float[] pheroSumIntoEachVert = new float[Prefs.colonyPortions];
        for (int i = 0; i < Prefs.colonyPortions; i++)
        {
            for (int j = 0; j < Prefs.colonyPortions; j++)
            {
                pheroSumIntoEachVert[j] += pheromone[i, j];
            }
        }

        int worstIndex = Array.IndexOf(pheroSumIntoEachVert, pheroSumIntoEachVert.Min());

        // Replace the worst index with a random portion
        vertices[worstIndex] = RandomPortion;

        // Calculate and assign new fitnesses
        for (int i = 0; i < Prefs.colonyPortions; i++)
        {
            fitnesses[i, worstIndex] = CalculateEdgeFitness(i, worstIndex);
            fitnesses[worstIndex, i] = CalculateEdgeFitness(worstIndex, i);

            pheromone[i, worstIndex] = (float)Rand.NextDouble();
            pheromone[worstIndex, i] = (float)Rand.NextDouble();
        }


        // Reset pheromone
        ActOnMatrix(pheromone, (int i, int j, float _) => pheromone[i, j] = (float)Rand.NextDouble());
    }
}