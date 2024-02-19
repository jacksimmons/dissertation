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

// This problem (each iteration):
// Each node is a random Food. The ants move around the Food space, trying
// to find the optimal Day (collection of Portions). The fittest Days are given
// more pheromone, as they are more optimal.

// The Top n days are then added to the population for display in the GUI.
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class ACO : Algorithm
{
    private List<Portion> m_portions;
    private float[] m_fitnesses;
    private float[] m_pheromone;

    const int NUM_ANTS = 100;


    public ACO() : base()
    {
        // Pheromone and fitness initialisation
        m_fitnesses = new float[m_foods.Count];
        m_pheromone = new float[m_foods.Count];

        for (int i = 0; i < m_foods.Count; i++)
        {
            m_pheromone[i] = Random.Range(0f, 1f);
        }
    }


    protected override void RunIteration()
    {
        for (int i = 0; i < NUM_ANTS; i++)
        {
        }
    }
}
