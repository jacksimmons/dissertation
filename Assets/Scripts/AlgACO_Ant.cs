// Commented 7/4
using System;
using System.Collections.Generic;
using System.Linq;


partial class AlgACO
{
    /// <summary>
    /// A nested class of AlgACO, which navigates around a matrix of portion vertices,
    /// trying to find the fittest route through each.
    /// </summary>
    protected class Ant
    {
        /// <summary>
        /// The algorithm this ant belongs to.
        /// </summary>
        private readonly AlgACO m_colony;

        /// <summary>
        /// If true, this ant overwrites its path into the day population.
        /// Can be set to false for testing ants.
        /// </summary>
        private readonly bool m_partOfPopulation;

        /// <summary>
        /// The index of the vertex (in the colony's portions) this ant starts at.
        /// </summary>
        private readonly int m_startIndex;

        /// <summary>
        /// The current path this ant is travelling along.
        /// </summary>
        public Day Path;
        public int PathLength => Path.portions.Count;
        public float Fitness
        {
            get
            {
                if (m_partOfPopulation) return Path.TotalFitness.Value;
                return Path.TotalFitness.Value;
            }
        }

        public int LastIndex { get; private set; }

        /// <summary>
        /// A quick lookup for all indices the path contains.
        /// </summary>
        private HashSet<int> m_pathIndices;


        public Ant(AlgACO colony, int startIndex, bool partOfPopulation)
        {
            // Init
            m_colony = colony;
            m_partOfPopulation = partOfPopulation;
            Path = new(m_colony);
            m_startIndex = startIndex;

            // Insert path into the population, if necessary
            if (partOfPopulation)
            {
                m_colony.AddToPopulation(Path);
            }

            // Initialise the path (after inserting into population)
            ResetPath();
        }


        /// <summary>
        /// Reset the ant's path. The path must be inserted into the population
        /// for this to work.
        /// </summary>
        public void ResetPath()
        {
            if (m_partOfPopulation)
            // Ensure to remove the path from the population before resetting it
                m_colony.RemoveFromPopulation(Path);

            // Instantiate a new path, to reset it
            Path = new(m_colony);
            
            if (m_partOfPopulation)
            // Add the reset path back into the population
                m_colony.AddToPopulation(Path);

            // Reset the lookup for indices in the path
            m_pathIndices = new(Prefs.colonyPortions);

            AddIndex(m_startIndex);
        }


        /// <summary>
        /// Adds a portion by index in the colony's vertices array.
        /// </summary>
        public void AddIndex(int index)
        {
            Path.AddPortion(m_colony.m_vertices[index]);
            m_pathIndices.Add(index);
            LastIndex = index;
        }


        /// <summary>
        /// Accessible method to check whether the provided index has been traversed by the current path.
        /// </summary>
        public bool PathContains(int portionIndex)
        {
            return m_pathIndices.Contains(portionIndex);
        }


        /// <summary>
        /// Add pheromone to the matrix based on the fitness of the current path.
        /// </summary>
        public void DepositPheromone()
        {
            // A path with one vertex has no edges
            if (Path.portions.Count <= 1) return;

            // A value representing path fitness
            float increment = Preferences.Instance.pheroImportance / Path.TotalFitness.Value;

            // Increase pheromone for every edge on the path, based on the path's fitness
            for (int j = 1; j < Path.portions.Count; j++)
            {
                m_colony.m_pheromone[j - 1, j] += increment;
            }
        }


        /// <summary>
        /// Gets an ant (empty Day) to traverse its whole path,
        /// based on a pheromone/desirability probability calculation.
        /// </summary>
        public void RunAnt()
        {
            float[] probabilities = GetAllVertexProbabilities(LastIndex);
            int nextVertex = MathTools.GetFirstSurpassedProbability(probabilities, m_colony.Rand);

            // If the next vertex was selected, then add the
            // new portion and continue.
            // Only do this up to a recursion limit, because
            // this function could enter millions of iterations
            // otherwise.
            if (nextVertex != -1)
            {
                AddIndex(nextVertex);
                // Located the goal portion
                if (nextVertex == Preferences.Instance.colonyPortions - 1)
                    return;
                RunAnt();
            }
            else if (m_pathIndices.Count < Preferences.Instance.colonyPortions)
            {
                // Infinity edge encountered.
                Logger.Log("Infinity edge encountered in Ant.");
            }
            
            // Otherwise, all vertices were added to the path, so exit.
        }


        /// <summary>
        /// Applies for movement from `prev`. A multiplier of -1 indicates the ant
        /// cannot go there.
        /// 
        /// Reference: ECM3412 Lecture 8 2023
        /// (tau[i,j]^(alpha) * eta[i,j]^(beta)) /
        /// (sum_h(tau[i,h]^(alpha) * eta[i,h]^(beta))
        /// 
        /// Equiv. to:
        /// i = prev, known value
        /// probObjs[j] / sum_h(probObjs[h])
        /// 
        /// </summary>
        /// <param name="prev">Last selected portion index.</param>
        /// <returns>An array containing the "probability" value for each vertex
        /// by index.</returns>
        public float[] GetAllVertexProbabilities(int prev)
        {
            float[] probObjs = new float[Prefs.colonyPortions];

            // Iterate over every h in the portions still to be visited
            for (int h = 0; h < Prefs.colonyPortions; h++)
            {
                // If the portion is not yet to be visited, leave it as 0.
                if (h == prev) continue;
                if (PathContains(h)) continue;

                // Get fitness and pheromone values
                float f = m_colony.m_fitnesses[prev, h];
                if (float.IsPositiveInfinity(f)) continue;
                float p = m_colony.m_pheromone[prev, h];

                // Calculate the value based on alpha and beta
                probObjs[h] = MathF.Pow(p, Preferences.Instance.acoAlpha)
                            * MathF.Pow(f, Preferences.Instance.acoBeta);
            }

            float denom = probObjs.Sum();

            float[] probs = new float[Prefs.colonyPortions];

            // If denom is 0, can quick exit with all values set by default to 0.
            if (denom == 0) return probs;

            // denom is not 0 in this loop.
            for (int j = 0; j < Prefs.colonyPortions; j++)
            {
                probs[j] = probObjs[j] / denom;
            }

            return probs;
        }
    }
}