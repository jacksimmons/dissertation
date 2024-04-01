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
using System.Linq;
using Random = UnityEngine.Random;


// Limitation: Can only have one 100g portion for each food type.
    
// Warning: You'll want to use no hard constraints for this algorithm, as then all
// edge fitnesses will likely be infinity, due to each individual portion forming
// a very sub-optimal day.


/// <summary>
/// Ant colony optimisation algorithm, which has tunable parameters. (See Preferences class)
/// 
/// This algorithm models ants in real-life, where the ants are placed in a grid of
/// equally-spaced food portions. (The assumption is made, that every portion is always
/// equidistant to every other portion, which would be hard to achieve in real-life.)
/// 
/// (*) The ants will find the shortest path from one food to another. The ants are given some time
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
public partial class AlgACO : Algorithm
{
    private readonly float[,] m_fitnesses = new float[Prefs.colonyPortions, Prefs.colonyPortions];
    private readonly float[,] m_pheromone = new float[Prefs.colonyPortions, Prefs.colonyPortions];
    private readonly Portion[] m_vertices = new Portion[Prefs.colonyPortions];
    private readonly Ant[] m_ants = new Ant[Preferences.Instance.populationSize];


    public override bool Init()
    {
        if (!base.Init()) return false;

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
            // Ants all start at the same portion
            Ant ant = new(this, 0, true);
            m_ants[i] = ant;
        }

        return true;
    }


    /// <summary>
    /// The fitness of an edge is the difference in fitness that occurs when adding the second food.
    /// 
    /// This means the edges are NOT bi-directional.
    /// </summary>
    /// <param name="i">The previous vertex.</param>
    /// <param name="j">The new vertex.</param>
    /// <returns>The difference in fitness as a result of adding vertex `j`.</returns>
    protected float CalculateEdgeFitness(int i, int j)
    {
        Ant fitnessTester = new(this, i, false);
        float fitnessBefore = fitnessTester.Fitness;

        fitnessTester.AddIndex(j);
        float fitnessAfter = fitnessTester.Fitness;

        // Return a finite diff, if it is finite & not NaN. Otherwise return +Infinity.
        float diff = MathF.Abs(fitnessAfter - fitnessBefore);
        if (float.IsFinite(diff) && !float.IsNaN(diff)) return diff;
        return float.PositiveInfinity;
    }


    /// <summary>
    /// Perform an action on every element in the matrix.
    /// Matrix must have standard dimensions (Prefs.colonyPortions X Prefs.colonyPortions)
    /// </summary>
    protected static void ActOnMatrix(float[,] mat, Action<int, int, float> act)
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
            UpdateSearchSpace(m_fitnesses, m_pheromone, m_vertices);
        }

        // Reset all ants
        ResetAnts();

        // Run all ants
        for (int i = 0; i < Prefs.populationSize; i++)
        {
            m_ants[i].RunAnt();
        }


        // Generate ant solutions
        // https://stackoverflow.com/questions/6529659/wait-for-queueuserworkitem-to-complete

        //if (Prefs.acoUseThreads)
        //{
        //    ManualResetEvent completionEvent = new(false);
        //    int threadsLeft = Preferences.Instance.populationSize;
        //    for (int i = 0; i < Preferences.Instance.populationSize; i++)
        //    {
        //        Ant ant = m_ants[i];

        //        /// <summary>
        //        /// Handles an ant thread. Converts the object state into the parameters
        //        /// required for running the ant.
        //        /// </summary>
        //        /// <param name="state">The state provided to the thread (parameters).</param>
        //        void RunAntThread(object state)
        //        {
        //            Ant ant = state as Ant;
        //            ant.RunAnt();

        //            if (Interlocked.Decrement(ref threadsLeft) == 0) completionEvent.Set();
        //        }

        //        ThreadPool.QueueUserWorkItem(RunAntThread!, ant);
        //    }

        //    completionEvent.WaitOne();
        //}
        //else
        //{
        //    for (int i = 0; i < Preferences.Instance.populationSize; i++)
        //    {
        //        m_ants[i].RunAnt();
        //    }
        //}

        // Update pheromone
        UpdatePheromone();
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


    /// <summary>
    /// Remove the worst food in the experiment, and replace it with a new random
    /// one from the dataset.
    /// 
    /// The "worst" is calculated by selecting the vertex with the lowest total
    /// pheromone incoming from all other vertices.
    /// 
    /// Generally improves algorithm performance.
    /// </summary>
    private void UpdateSearchSpace(float[,] fitnesses, float[,] pheromone, Portion[] vertices)
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
