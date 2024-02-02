// Notes:

// The overall problem is to find the path which gets closest to the optimal length
// for a given constraint. Where length = cost of all nodes + cost of all edges

// Each food is a node.
// Each food connects to every other food via edges, which are fitness evaluations.
// - The edge going from A to B is the fitness of food B.
// - And vice versa: B->A is the fitness of A.

// Therefore, a lot of memory can be saved by not repeating these edge costs.
// The cost of any edge N->A will always cost the same amount.
// The same applies to pheromone - the edges are non-directional so a float[]
// type will suffice. (Matrix is not needed unlike normal)

// Thus all of the weight data can be stored in a float[] structure, where the index is
// the food index, and the value is the fitness of that food.

// Typical ACO:
// Ants move around a graph, dropping pheromone where they go.
// The shorter paths are given more pheromone, as they are more optimal.
// The ants hopefully converge on a global minimum through further iterations.

// This problem:
// Each node is a random Portion. The ants move around the Portion space, trying
// to find the optimal Meal (collection of Portions). The fittest Meals are given
// more pheromone, as they are more optimal.

// End of phase [EOP]:
// The fittest Meals are then collected into a Day.
// The number of Meals in the Day is pre-determined, and has already been factored
// into the optimal requirements of each Meal (r_meal = r_day / mealsInDay)
// (Maybe) Add some Quantity to the portions in the fittest Meal instead of pheromone

// Continuing:
// The ACO algorithm is just run again until the next EOP.
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class ACOAlgorithm : Algorithm
{
    private int m_numPortions;
    private List<Portion> m_portions;
    private float[] m_fitnesses;
    private float[] m_pheromone;


    protected override void RunIteration()
    {
        // Random portions (Copy from EA)

        // Pheromone and fitness initialisation
        m_fitnesses = new float[m_numPortions];
        m_pheromone = new float[m_numPortions];

        for (int i = 0; i < m_numPortions; i++)
        {
            m_pheromone[i] = Random.Range(0f, 1f);
        }
    }
}
