using System;
using System.Collections.Generic;
using System.Linq;

partial class AlgACO
{
    protected class Ant
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
}