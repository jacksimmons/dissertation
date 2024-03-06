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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Random = System.Random;


// Limitation: Can only have one 100g portion for each food type.
public class AlgACO : Algorithm
{
    private const float EPSILON = 1e-7f;
    private const int RECURSE_LIMIT = 1;

    private float[,] m_fitnesses;
    private float[,] m_pheromone;
    private Portion[] m_vertices;


    public AlgACO() : base(false)
    {
        // Pheromone and fitness initialisation
        m_fitnesses = new float[m_foods.Count, m_foods.Count];
        m_pheromone = new float[m_foods.Count, m_foods.Count];
        
        // Easier to iterate over Portions than Foods
        m_vertices = new Portion[m_foods.Count];

        for (int i = 0; i < Preferences.Instance.populationSize; i++)
        {
            Day day = new();
            InitialiseTour(day);
            AddToPopulation(day);
        }
    }


    public override void PostConstructorSetup()
    {
        base.PostConstructorSetup();

        for (int i = 0; i < m_foods.Count; i++)
        {
            for (int j = 0; j < m_foods.Count; j++)
            {
                m_fitnesses[i, j] = CalculateEdgeFitness(i, j);
                m_pheromone[i, j] = (float)Rand.NextDouble();
            }

            m_vertices[i] = new(m_foods[i]);
        }
    }


    /// <summary>
    /// The fitness of an edge is the difference in fitness
    /// that occurs when adding the second food.
    /// 
    /// This means the edges are NOT bi-directional.
    /// </summary>
    /// <param name="i">The previous vertex.</param>
    /// <param name="j">The new vertex.</param>
    /// <returns>The difference in fitness as a result
    /// of adding vertex `j`.</returns>
    private float CalculateEdgeFitness(int i, int j)
    {
        Day fitnessTester = new();
        fitnessTester.AddPortion(new(m_foods[i]));
        float originalFitness = fitnessTester.Fitness;

        fitnessTester.AddPortion(new(m_foods[j]));

        // Regardless of i, the new fitness being infinity makes this edge terrible.
        if (float.IsPositiveInfinity(fitnessTester.Fitness)) return float.PositiveInfinity;

        // Check if fitness goes from infinity to a finite value
        if (float.IsPositiveInfinity(originalFitness))
        {
            if (!float.IsPositiveInfinity(fitnessTester.Fitness))
            {
                // A movement from Infinity to a finite value should be considered very highly.
                // Give this edge a fitness of 0.
                return 0;
            }
            return float.PositiveInfinity;
        }

        return MathF.Abs(fitnessTester.Fitness - originalFitness);
    }


    private void InitialiseTour(Day day)
    {
        // Random start index for each ant
        day.AddPortion(new(m_foods[Rand.Next(0, m_foods.Count)]));
    }


    protected override void RunIteration()
    {
        // Generate ant solutions
        for (int i = 0; i < Preferences.Instance.populationSize; i++)
        {
            Day day = Population[i];
            ThreadPool.QueueUserWorkItem(RunAntThread, day);
        }

        // Update pheromone
        UpdatePheromone();

        // Reset all ants
        for (int i = 0; i < Preferences.Instance.populationSize; i++)
        {
            InitialiseTour(Population[i]);
        }
    }


    /// <summary>
    /// Handles an ant thread. Converts the object state into the parameters
    /// required for running the ant.
    /// </summary>
    /// <param name="state">The state provided to the thread (parameters).</param>
    private void RunAntThread(object state)
    {
        Day day = state as Day;
        RunAnt(day);
    }


    /// <summary>
    /// Gets an ant (empty Day) to traverse its whole path,
    /// based on a pheromone/desirability probability calculation.
    /// </summary>
    /// <param name="ant">The ant.</param>
    private void RunAnt(Day ant, int recursion = 0)
    {
        // The ant always has at least one vertex, as it is initialised with one
        int lastVertex = m_foods.IndexOf(ant.Portions[^1].Food);

        float[] probabilities = GetAllVertexProbabilities(lastVertex, ant);

        // Clamp the probability to the range [EPSILON, 1 - EPSILON]
        float probability = MathF.Max(MathF.Min((float)Rand.NextDouble(), 1 - EPSILON), EPSILON);

        // Calculate which vertex was selected, through
        // a sum-of-probabilities check.
        float sum = 0;
        int nextVertex = -1;
        for (int i = 0; i < m_vertices.Length; i++)
        {
            sum += probabilities[i];
            if (sum > probability)
            {
                nextVertex = i;
                break;
            }
        }

        // If the next vertex was selected, then add the
        // new portion and continue.
        // Only do this up to a recursion limit, because
        // this function could enter millions of iterations
        // otherwise.
        if (nextVertex != -1)
        {
            ant.AddPortion(m_vertices[nextVertex]);

            if (recursion + 1 < RECURSE_LIMIT)
                RunAnt(ant, recursion + 1);
        }
        // If the next vertex wasn't selected, all remaining
        // untraversed edges have Infinity fitness.
        // For now, ignore these. There are plenty of foods
        // in the dataset that it is extremely unlikely that
        // any of these Infinity edges are necessary.
        
        // So end recursion.
    }


    /// <summary>
    /// Calculates the probability that an ant will select
    /// any of the possible next vertices, when moving from
    /// `prev`. A probability of -1 indicates that the ant
    /// cannot go there.
    /// 
    /// (tau[i,j]^(alpha) * eta[i,j]^(beta)) /
    /// (sum_h(tau[i,h]^(alpha) * eta[i,h]^(beta))
    /// </summary>
    /// <param name="prev">Last selected portion index.</param>
    /// <param name="ant">The ant (Day) selecting.</param>
    /// <returns>An array containing the probability that
    /// each vertex is selected, by index.</returns>
    private float[] GetAllVertexProbabilities(int prev, Day ant)
    {
        float[] probs = new float[m_vertices.Length];

        float[] J = new float[m_vertices.Length];
        for (int h = 0; h < m_vertices.Length; h++)
        {
            // Quick-exit default assignment
            J[h] = 0;

            if (h == prev) continue;
            if (ant.Portions.Contains(m_vertices[h])) continue;

            float p = m_pheromone[prev, h];
            float f = m_fitnesses[prev, h];

            // Probability of selection is 0 for an Infinity edge.
            if (float.IsPositiveInfinity(f)) continue;

            J[h] = MathF.Pow(p, Preferences.Instance.acoAlpha)
                 * MathF.Pow(f, Preferences.Instance.acoBeta);
        }

        float denom = J.Sum();
        for (int j = 0; j < m_vertices.Length; j++)
        {
            // J[j] == 0 implies the edge is invalid (visited, a self-edge, or Infinity fitness)
            if (Static.Approximately( J[j], 0 ))
            {
                probs[j] = 0;
                continue;
            }
            probs[j] = J[j] / denom;
        }
        return probs;
    }


    private void UpdatePheromone()
    {
        for (int i = 0; i < m_vertices.Length; i++)
        {
            for (int j = 0; j < m_vertices.Length; j++)
            {
                m_pheromone[i, j] *= (1 - Preferences.Instance.pheroEvapRate);
            }
        }

        // Add pheromone to edges used by ants in their paths
        for (int i = 0; i < Population.Count; i++)
        {
            if (Population[i].Portions.Count <= 1) continue;

            float increment = Preferences.Instance.pheroImportance / TourFitness(Population[i]);
            for (int j = 0; j < Population[i].Portions.Count; j++)
            {
                m_pheromone[i, j] += increment;
            }

            m_pheromone[Population[i].Portions.Count - 1, 0] += increment;
        }
    }


    // Edge i from a to b costs (Fitness A && B - Fitness A)
    /// <summary>
    /// Calculates the fitness of a Day from edge costs (as a tour).
    /// </summary>
    /// <param name="day">The day (tour).</param>
    /// <returns>The fitness.</returns>
    private float TourFitness(Day day)
    {
        if (day.Portions.Count <= 1) return float.PositiveInfinity;

        float fitness = 0;
        for (int i = 0; i < day.Portions.Count - 1; i++)
        {
            fitness += EdgeFitness(day.Portions[i], day.Portions[i + 1]);
        }
        fitness += EdgeFitness(day.Portions[^1], day.Portions[0]);

        return fitness;
    }


    /// <summary>
    /// Calculates the fitness of an edge, from two portions.
    /// </summary>
    /// <returns>The fitness.</returns>
    private float EdgeFitness(Portion a, Portion b)
    {
        return m_fitnesses[m_foods.IndexOf(a.Food), m_foods.IndexOf(b.Food)];
    }
}
