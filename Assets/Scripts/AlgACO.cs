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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using Random = System.Random;


// Limitation: Can only have one 100g portion for each food type.


// Warning: You'll want to use no hard constraints for this algorithm, as then all
// edge fitnesses will likely be infinity, due to each individual portion forming
// a very sub-optimal day.

/// <summary>
/// An ACO algorithm for meal planning.
/// 
/// This algorithm models ants in real-life, where the ants are placed in a grid of
/// equally-spaced food portions. (The assumption is made, that every portion is always
/// equidistant to every other portion, which would be hard to achieve in real-life.)
/// 
/// The ants will each begin at different portions to maximise search space exploration.
/// If there are more ants than portions, some will share start points.
/// 
/// (*) The ants will find the shortest path around all of the foods. The ants are given some time
/// to traverse these foods and lay down enough pheromone to decide the clear worst food.
/// Then the worst food is removed and replaced with a new one. (Avoid stagnation)
/// 
/// Replacing foods is crucial to traverse the whole search space - the search space (1000s of foods,
/// with any real mass) is too big to tackle all at once.
/// 
/// (*) The distance between two foods is determined by the nutritional value of the combined
/// foods. Given that the first food was eaten, how beneficial would it be to eat the second?
/// E.g. Having eaten 400g of nuts, it would not be beneficial to have more nuts, or fat/protein! 
/// </summary>
public class AlgACO : Algorithm
{
    // Initialisation
    partial class Ant
    {
        private static int s_numAnts;
        public readonly int id;
        private readonly AlgACO m_colony;
        
        private readonly int m_startIndex;

        private Day m_path;
        public int PathLength => m_path.portions.Count;
        public float Fitness
        {
            get
            {
                if (m_partOfPopulation) return m_colony.m_population.GetFitness(m_path);
                return m_path.GetFitness();
            }
        }

        public int LastIndex { get; private set; }
        private HashSet<int> m_pathIndices;

        private readonly bool m_partOfPopulation;


        public Ant(AlgACO colony, int startIndex, bool partOfPopulation)
        {
            m_colony = colony;

            m_partOfPopulation = partOfPopulation;
            m_path = new(m_colony);
            if (partOfPopulation)
            {
                // 0, 1, 2, ...
                id = s_numAnts;
                s_numAnts++;
                m_colony.m_population.Add(m_path);
            }
            else
            {
                id = -1;
            }

            m_startIndex = startIndex;

            // Initialise the path
            ResetPath();
        }


        /// <summary>
        /// Reset the ant's path.
        /// </summary>
        public void ResetPath()
        {
            if (m_partOfPopulation)
                m_colony.m_population.Remove(m_path);

            m_path = new(m_colony);
            m_pathIndices = new(Prefs.colonyPortions);

            if (m_partOfPopulation)
                m_colony.m_population.Add(m_path);

            AddIndex(m_startIndex);
        }


        /// <summary>
        /// Adds a portion by index in the colony's vertices array.
        /// </summary>
        public void AddIndex(int index)
        {
            m_path.AddPortion(m_colony.m_vertices[index]);
            m_pathIndices.Add(index);
            LastIndex = index;
        }


        public bool PathContains(int portionIndex)
        {
            return m_pathIndices.Contains(portionIndex);
        }


        public Day ClonePath()
        {
            return new(m_path);
        }


        public void DepositPheromone()
        {
            if (m_path.portions.Count <= 1) return;

            float increment = Preferences.Instance.pheroImportance / m_colony.m_population.GetFitness(m_path);
            for (int j = 1; j < m_path.portions.Count; j++)
            {
                m_colony.m_pheromone[j - 1, j] += increment;
            }

            m_colony.m_pheromone[m_path.portions.Count - 1, 0] += increment;
        }


        /// <summary>
        /// Gets an ant (empty Day) to traverse its whole path,
        /// based on a pheromone/desirability probability calculation.
        /// </summary>
        public void RunAnt(int recursion = 0)
        {
            float[] probabilities = GetAllVertexProbabilities(LastIndex);
            int nextVertex = MathTools.GetFirstSurpassedProbability(probabilities);

            // If the next vertex was selected, then add the
            // new portion and continue.
            // Only do this up to a recursion limit, because
            // this function could enter millions of iterations
            // otherwise.
            if (nextVertex != -1)
            {
                AddIndex(nextVertex);

                if (recursion + 1 < Prefs.colonyPortions)
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
        /// In this form of ACO, the "probability" of vertex selection translates into
        /// the mass of the portion selected.
        /// 
        /// ALL portions in the initial population are added to each ant, except the
        /// really bad ones will have an infinitessimal mass.
        /// 
        /// Applies for movement from `prev`. A multiplier of -1 indicates the ant
        /// cannot go there.
        /// 
        /// (tau[i,j]^(alpha) * eta[i,j]^(beta)) /
        /// (sum_h(tau[i,h]^(alpha) * eta[i,h]^(beta))
        /// 
        /// Not stored in a data structure (this would minimise calculations in case
        /// ants go from the same previous node) because this would have to be serially
        /// calculated, slowing the more-demanding threaded program down.
        /// </summary>
        /// <param name="prev">Last selected portion index.</param>
        /// <returns>An array containing the "probability" value for each vertex
        /// by index.</returns>
        public float[] GetAllVertexProbabilities(int prev)
        {
            float[] probs = new float[Prefs.colonyPortions];

            float[] J = new float[Prefs.colonyPortions];
            for (int h = 0; h < Prefs.colonyPortions; h++)
            {
                // Quick-exit default assignment
                J[h] = 0;

                if (h == prev) continue;
                if (PathContains(h)) continue;

                float f = m_colony.m_fitnesses[prev, h];
                // Probability of selection is 0 for an Infinity fitness edge.
                if (float.IsPositiveInfinity(f)) continue;

                float p = m_colony.m_pheromone[prev, h];


                J[h] = MathF.Pow(p, Preferences.Instance.acoAlpha)
                     * MathF.Pow(f, Preferences.Instance.acoBeta);
            }

            float denom = J.Sum();
            for (int j = 0; j < Prefs.colonyPortions; j++)
            {
                //J[j] == 0 implies the edge is invalid(visited, a self-edge, or Infinity fitness)
                if (MathTools.Approx(J[j], 0))
                {
                    probs[j] = 0;
                    continue;
                }
                probs[j] = J[j] / denom;
            }
            return probs;
        }
    }

    private readonly float[,] m_fitnesses = new float[Prefs.colonyPortions, Prefs.colonyPortions];
    private readonly float[,] m_pheromone = new float[Prefs.colonyPortions, Prefs.colonyPortions];
    private readonly Portion[] m_vertices = new Portion[Prefs.colonyPortions];
    private readonly Ant[] m_ants = new Ant[Preferences.Instance.populationSize];

    //private bool[] m_exploredFoods;


    public override void Init()
    {
        //m_exploredFoods = new bool[Foods.Count];


        // Create vertices
        for (int i = 0; i < Prefs.colonyPortions; i++)
        {
            Portion randP = RandomPortion;
            m_vertices[i] = randP;
        }


        // Calculate and assign edge fitnesses and pheromone
        ActOnMatrix(m_fitnesses, (int i, int j, float _) =>
        {
            m_fitnesses[i, j] = CalculateEdgeFitness(i, j);
            m_pheromone[i, j] = (float)Rand.NextDouble();
        });


        // Create ants
        for (int i = 0; i < Preferences.Instance.populationSize; i++)
        {
            // Ants are split roughly equally between the portions
            Ant ant = new(this, i % Prefs.colonyPortions, true);
            m_ants[i] = ant;
        }
    }


    /// <summary>
    /// The fitness of an edge is the difference in fitness that occurs when adding the second food.
    /// 
    /// This means the edges are NOT bi-directional.
    /// </summary>
    /// <param name="i">The previous vertex.</param>
    /// <param name="j">The new vertex.</param>
    /// <returns>The difference in fitness as a result of adding vertex `j`.</returns>
    private float CalculateEdgeFitness(int i, int j)
    {
        Ant fitnessTester = new(this, i, false);
        float fitnessBefore = fitnessTester.Fitness;

        fitnessTester.AddIndex(j);
        float fitnessAfter = fitnessTester.Fitness;

        return MathF.Abs(fitnessAfter - fitnessBefore);
    }


    /// <summary>
    /// Perform an action on every element in the matrix.
    /// Matrix must have standard dimensions (Prefs.colonyPortions X Prefs.colonyPortions)
    /// </summary>
    private static void ActOnMatrix(float[,] mat, Action<int, int, float> act)
    {
        for (int i = 0; i < Prefs.colonyPortions; i++)
        {
            for (int j = 0; j < Prefs.colonyPortions; j++)
            {
                act(i, j, mat[i, j]);
            }
        }
    }


    //private Tuple<int, int> GetBestMatrixEdge(float[,] mat, bool minimise = false)
    //{
    //    float best = minimise ? float.PositiveInfinity : 0;

    //    // Default edge; if all are infinity return 0 to 1
    //    int bestI = 0;
    //    int bestJ = 1;

    //    ActOnMatrix(mat, (int i, int j, float x) =>
    //    {
    //        if (minimise)
    //        {
    //            if (x < best)
    //            {
    //                best = x;
    //                bestI = i;
    //                bestJ = j;
    //            }
    //        }
    //        else
    //        {
    //            if (x > best)
    //            {
    //                best = x;
    //                bestI = i;
    //                bestJ = j;
    //            }
    //        }
    //    });

    //    return new(bestI, bestJ);
    //}


    protected override void NextIteration()
    {
        // Reset the simulation
        if (IterNum % Prefs.colonyStagnationIters == 0)
        {
            ReplaceWorstFood();
        }


        // Reset all ants
        ResetAnts();


        // Generate ant solutions
        // https://stackoverflow.com/questions/6529659/wait-for-queueuserworkitem-to-complete

        if (Prefs.acoUseThreads)
        {
            ManualResetEvent completionEvent = new(false);
            int threadsLeft = Preferences.Instance.populationSize;
            for (int i = 0; i < Preferences.Instance.populationSize; i++)
            {
                Ant ant = m_ants[i];

                /// <summary>
                /// Handles an ant thread. Converts the object state into the parameters
                /// required for running the ant.
                /// </summary>
                /// <param name="state">The state provided to the thread (parameters).</param>
                void RunAntThread(object state)
                {
                    Ant ant = state as Ant;
                    ant.RunAnt();

                    if (Interlocked.Decrement(ref threadsLeft) == 0) completionEvent.Set();
                }

                ThreadPool.QueueUserWorkItem(RunAntThread!, ant);
            }

            completionEvent.WaitOne();
        }
        else
        {
            for (int i = 0; i < Preferences.Instance.populationSize; i++)
            {
                m_ants[i].RunAnt();
            }
        }

        // Update pheromone
        UpdatePheromone();
    }


    /// <summary>
    /// Remove the worst food in the experiment, and replace it with a new random
    /// one from the dataset.
    /// 
    /// The "worst" is calculated by selecting the vertex with the lowest total
    /// pheromone incoming from all other vertices.
    /// 
    /// Generally improves algorithm performance.
    /// </summary>
    private void ReplaceWorstFood()
    {
        float[] pheroSumIntoEachVert = new float[Prefs.colonyPortions];
        for (int i = 0; i < Prefs.colonyPortions; i++)
        {
            for (int j = 0; j < Prefs.colonyPortions; j++)
            {
                pheroSumIntoEachVert[j] += m_pheromone[i, j];
            }
        }

        int worstIndex = Array.IndexOf(pheroSumIntoEachVert, pheroSumIntoEachVert.Min());

        // Replace the worst index with a random portion
        m_vertices[worstIndex] = RandomPortion;

        // Calculate and assign new fitnesses
        for (int i = 0; i < Prefs.colonyPortions; i++)
        {
            m_fitnesses[i, worstIndex] = CalculateEdgeFitness(i, worstIndex);
            m_fitnesses[worstIndex, i] = CalculateEdgeFitness(worstIndex, i);

            m_pheromone[i, worstIndex] = (float)Rand.NextDouble();
            m_pheromone[worstIndex, i] = (float)Rand.NextDouble();
        }


        // Reset pheromone
        ActOnMatrix(m_pheromone, (int i, int j, float _) => m_pheromone[i, j] = (float)Rand.NextDouble());
    }


    private void ResetAnts()
    {
        for (int i = 0; i < m_ants.Length; i++)
        {
            m_ants[i].ResetPath();
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
        for (int i = 0; i < Preferences.Instance.populationSize; i++)
        {
            m_ants[i].DepositPheromone();
        }

        if (BestDayExists && Preferences.Instance.elitist) DepositPheromone(BestDay, BestFitness);
    }


    private void DepositPheromone(Day path, float fitness)
    {
        if (path.portions.Count <= 1) return;

        float increment = Preferences.Instance.pheroImportance / fitness;
        for (int j = 1; j < path.portions.Count; j++)
        {
            m_pheromone[j - 1, j] += increment;
        }

        m_pheromone[path.portions.Count - 1, 0] += increment;
    }


    // Edge i from a to b costs (Fitness A && B - Fitness A)
    /// <summary>
    /// Calculates the fitness of a Day from edge costs (as a tour).
    /// </summary>
    /// <param name="day">The day (tour).</param>
    /// <returns>The fitness.</returns>
    //private float TourFitness(Ant ant)
    //{
    //    if (day.portions.Count <= 1) return float.PositiveInfinity;

    //    float fitness = 0;
    //    for (int i = 0; i < day.portions.Count - 1; i++)
    //    {
    //        fitness += EdgeFitness(day.portions[i], day.portions[i + 1]);
    //    }
    //    fitness += EdgeFitness(day.portions[^1], day.portions[0]);

    //    return fitness;
    //}
}
