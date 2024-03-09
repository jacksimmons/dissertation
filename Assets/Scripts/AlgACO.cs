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
// to find the optimal Day (collection of portions). The fittest Days are given
// more pheromone, as they are more optimal.

// The Top n days are then added to the population for display in the GUI.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Random = System.Random;


// Limitation: Can only have one 100g portion for each food type.


// Warning: You'll want to use no hard constraints for this algorithm, as then all
// edge fitnesses will likely be infinity, due to each individual portion forming
// a very sub-optimal day.
public class AlgACO : Algorithm
{
    // Initialisation
    partial class Ant
    {
        private static int s_numAnts;
        public readonly int id;

        private AlgACO m_colony;
        private Day m_path;
        public Day Path
        {
            get
            {
                return new(m_path);
            }
        }

        public float Fitness
        {
            get
            {
                return m_path.GetFitness();
            }
        }

        private readonly int m_startIndex;
        public int LastIndex
        {
            get
            {
                return Array.IndexOf(m_colony.m_vertices, m_path.portions[^1]);
            }
        }


        public Ant(AlgACO colony, int startIndex)
        {
            m_colony = colony;
            m_path = new();

            // 0, 1, 2, ...
            id = s_numAnts;
            s_numAnts++;
            m_startIndex = startIndex;

            // Add the start vertex
            m_path.AddPortion(m_colony.m_vertices[startIndex]);
        }


        public void AddPortion(Portion p)
        {
            m_path.AddPortion(p);
        }
    }

    private const float EPSILON = 1e-7f;
    private const int RECURSE_LIMIT = 1;
    private const int NUM_PORTIONS = 10;

    private float[,] m_fitnesses;
    private float[,] m_pheromone;
    private Portion[] m_vertices;

    private int m_startIndex;

    private Ant[] m_ants;

    public override float AverageFitness => 0;


    public override void Init()
    {
        if (NUM_PORTIONS > Foods.Count) throw new ArgumentOutOfRangeException(nameof(NUM_PORTIONS));

        // Pheromone and fitness initialisation
        m_fitnesses = new float[NUM_PORTIONS, NUM_PORTIONS];
        m_pheromone = new float[NUM_PORTIONS, NUM_PORTIONS];

        // Easier to iterate over portions than Foods
        m_vertices = new Portion[NUM_PORTIONS];

        m_ants = new Ant[Preferences.Instance.populationSize];


        for (int i = 0; i < NUM_PORTIONS; i++)
        {
            m_vertices[i] = new(Foods[i])
            {
                Mass = Rand.Next(Preferences.Instance.portionMinStartMass, Preferences.Instance.portionMaxStartMass)
            };
        }


        for (int i = 0; i < NUM_PORTIONS; i++)
        {
            for (int j = 0; j < NUM_PORTIONS; j++)
            {
                m_fitnesses[i, j] = CalculateEdgeFitness(i, j);
                m_pheromone[i, j] = (float)Rand.NextDouble();
            }
        }


        m_startIndex = GetBestMatrixEdge(m_fitnesses, true).Item1;
        InitialiseAnts();
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
        fitnessTester.AddPortion(m_vertices[i]);
        float originalFitness = fitnessTester.GetFitness();

        fitnessTester.AddPortion(m_vertices[j]);

        // Regardless of i, the new fitness being infinity makes this edge terrible.
        if (float.IsPositiveInfinity(fitnessTester.GetFitness())) return float.PositiveInfinity;

        // Check if fitness goes from infinity to a finite value
        if (float.IsPositiveInfinity(originalFitness))
        {
            if (!float.IsPositiveInfinity(fitnessTester.GetFitness()))
            {
                // A movement from Infinity to a finite value should be considered very highly.
                // Give this edge a fitness of 0.
                return 0;
            }
            return float.PositiveInfinity;
        }

        return MathF.Abs(fitnessTester.GetFitness() - originalFitness);
    }


    /// <summary>
    /// Perform an action on every element in the matrix.
    /// Matrix must have standard dimensions (NUM_PORTIONS X NUM_PORTIONS)
    /// </summary>
    private void ActOnMatrix(float[,] mat, Action<int, int, float> act)
    {
        for (int i = 0; i < NUM_PORTIONS; i++)
        {
            for (int j = 0; j < NUM_PORTIONS; j++)
            {
                act(i, j, mat[i, j]);
            }
        }
    }


    private Tuple<int, int> GetBestMatrixEdge(float[,] mat, bool minimise = false)
    {
        float best = minimise ? float.PositiveInfinity : 0;
        int bestI = -1;
        int bestJ = -1;

        ActOnMatrix(mat, (int i, int j, float x) =>
        {
            if (minimise)
            {
                if (x < best)
                {
                    best = x;
                    bestI = i;
                    bestJ = j;
                }
            }
            else
            {
                if (x > best)
                {
                    best = x;
                    bestI = i;
                    bestJ = j;
                }
            }
        });

        return new(bestI, bestJ);
    }


    private void InitialiseAnts()
    {
        for (int i = 0; i < Preferences.Instance.populationSize; i++)
        {
            Ant ant = new(this, m_startIndex);
            m_ants[i] = ant;
        }
    }


    protected override void NextIteration()
    {
        // Generate ant solutions
        for (int i = 0; i < Preferences.Instance.populationSize; i++)
        {
            Ant ant = m_ants[i];
            ThreadPool.QueueUserWorkItem(RunAntThread!, ant);
        }

        // Update pheromone
        UpdatePheromone();

        // Reset all ants
        InitialiseAnts();
    }


    protected override void UpdateBestDay()
    {
        foreach (Ant ant in m_ants)
        {
            float antFitness = ant.Fitness;
            if (antFitness < BestFitness || BestDay == null)
                SetBestDay(ant.Path, antFitness, IterNum);
        }
    }


    /// <summary>
    /// Handles an ant thread. Converts the object state into the parameters
    /// required for running the ant.
    /// </summary>
    /// <param name="state">The state provided to the thread (parameters).</param>
    private void RunAntThread(object state)
    {
        Ant ant = state as Ant;
        ant.RunAnt();
    }


    partial class Ant
    {
        /// <summary>
        /// Gets an ant (empty Day) to traverse its whole path,
        /// based on a pheromone/desirability probability calculation.
        /// </summary>
        /// <param name="ant">The ant.</param>
        public void RunAnt(int recursion = 0)
        {
            // The ant always has at least one vertex, as it is initialised with one
            int lastVertex = LastIndex;

            float[] probabilities = GetAllVertexProbabilities(lastVertex);

            // Clamp the probability to the range [EPSILON, 1 - EPSILON]
            float probability = MathF.Max(MathF.Min((float)Rand.NextDouble(), 1 - EPSILON), EPSILON);

            // Calculate which vertex was selected, through
            // a sum-of-probabilities check.
            float sum = 0;
            int nextVertex = -1;
            for (int i = 0; i < NUM_PORTIONS; i++)
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
                AddPortion(m_colony.m_vertices[nextVertex]);

                if (recursion + 1 < RECURSE_LIMIT)
                    RunAnt(recursion + 1);
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
        private float[] GetAllVertexProbabilities(int prev)
        {
            float[] probs = new float[NUM_PORTIONS];

            float[] J = new float[NUM_PORTIONS];
            for (int h = 0; h < NUM_PORTIONS; h++)
            {
                // Quick-exit default assignment
                J[h] = 0;

                if (h == prev) continue;
                if (m_path.portions.Contains(m_colony.m_vertices[h])) continue;

                float p = m_colony.m_pheromone[prev, h];
                float f = m_colony.m_fitnesses[prev, h];

                // Probability of selection is 0 for an Infinity edge.
                if (float.IsPositiveInfinity(f)) continue;

                J[h] = MathF.Pow(p, Preferences.Instance.acoAlpha)
                     * MathF.Pow(f, Preferences.Instance.acoBeta);
            }

            float denom = J.Sum();
            for (int j = 0; j < NUM_PORTIONS; j++)
            {
                //J[j] == 0 implies the edge is invalid(visited, a self-edge, or Infinity fitness)
                if (MathfTools.Approx(J[j], 0))
                {
                    probs[j] = 0;
                    continue;
                }
                probs[j] = J[j] / denom;
            }
            return probs;
        }
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
        //for (int i = 0; i < Population.Count; i++)
        //{
        //    if (Population[i].portions.Count <= 1) continue;

        //    float increment = Preferences.Instance.pheroImportance / TourFitness(Population[i]);
        //    for (int j = 0; j < Population[i].portions.Count; j++)
        //    {
        //        m_pheromone[i, j] += increment;
        //    }

        //    m_pheromone[Population[i].portions.Count - 1, 0] += increment;
        //}
    }


    // Edge i from a to b costs (Fitness A && B - Fitness A)
    /// <summary>
    /// Calculates the fitness of a Day from edge costs (as a tour).
    /// </summary>
    /// <param name="day">The day (tour).</param>
    /// <returns>The fitness.</returns>
    private float TourFitness(Day day)
    {
        if (day.portions.Count <= 1) return float.PositiveInfinity;

        float fitness = 0;
        for (int i = 0; i < day.portions.Count - 1; i++)
        {
            fitness += EdgeFitness(day.portions[i], day.portions[i + 1]);
        }
        fitness += EdgeFitness(day.portions[^1], day.portions[0]);

        return fitness;
    }


    /// <summary>
    /// Calculates the fitness of an edge, from two portions.
    /// </summary>
    /// <returns>The fitness.</returns>
    private float EdgeFitness(Portion a, Portion b)
    {
        return m_fitnesses[Foods.IndexOf(a.food), Foods.IndexOf(b.food)];
    }


    /// <summary>
    /// Evaluate the provided solution and return a string corresponding to its fitness, or
    /// another way of describing as a string how good a solution is.
    /// </summary>
    public override string EvaluateDay(Day day)
    {
        return $"Fitness: {day.GetFitness()}";
    }


    /// <summary>
    /// Generates and returns a string with the average stats of the program (e.g. average population stats
    /// or average ant stats, etc.)
    /// </summary>
    public override string GetAverageStatsLabel()
    {
        float sumFitness = 0;
        for (int i = 0; i < Preferences.Instance.populationSize; i++)
        {
            sumFitness += m_ants[i].Fitness;
        }
        return $"Fitness: {sumFitness / Preferences.Instance.populationSize}";
    }


    public override void Day_OnPortionAdded(Day day, Portion portion)
    {
    }


    public override void Day_OnPortionRemoved(Day day, Portion portion)
    {
    }


    public override void Day_OnPortionMassModified(Day day, Portion portion, int dmass)
    {
    }
}
