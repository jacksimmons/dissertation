using System.Collections.Generic;
using System.Collections.ObjectModel;
using static Day;


/// <summary>
/// A class representing a sorted list of MNDSet objects.
/// </summary>
public class ParetoHierarchy
{
    private List<MNDSet> m_sets;


    public ParetoHierarchy()
    {
        m_sets = new();
    }


    /// <summary>
    /// Returns the set number of the provided ParetoFitness,
    /// or -1 if it doesn't have one.
    /// </summary>
    public int GetSet(ParetoFitness fitness)
    {
        for (int i = 0; i < m_sets.Count; i++)
        {
            if (m_sets[i].Contains(fitness))
            {
                return i;
            }
        }

        return -1;
    }


    /// <summary>
    /// Adds the provided fitness to the hierarchy.
    /// If a set is found where the fitness is mutually non-dominating,
    /// it is added to it.
    /// If a set is found where the fitness dominates any solution,
    /// </summary>
    public void Add(ParetoFitness fitness)
    {
        MNDSet newSet;
        for (int i = 0; i < m_sets.Count; i++)
        {
            switch (m_sets[i].Compare(fitness))
            {
                case -1:
                    // This fitness dominates the current set; need to
                    // prepend a new set
                    newSet = new();
                    m_sets[i] = newSet;
                    newSet.Add(fitness);
                    return;
                case 0:
                    // This fitness belongs in the set - it is mutually
                    // non-dominating to the whole set
                    m_sets[i].Add(fitness);
                    return;
            }
        }

        // This fitness is dominated by all other elements, or it is the first element
        // - append a new set
        newSet = new();
        m_sets.Add(newSet);
        newSet.Add(fitness);
    }


    /// <summary>
    /// Attempts to remove the provided fitness from a set.
    /// </summary>
    /// <param name="fitness">The fitness to try and remove.</param>
    /// <returns>`true` if the fitness was found and removed,
    /// `false` if not.</returns>
    public bool Remove(ParetoFitness fitness)
    {
        foreach (MNDSet set in m_sets)
        {
            if (set.Contains(fitness))
            {
                set.Remove(fitness);
                return true;
            }
        }

        return false;
    }
}


/// <summary>
/// Class representing a mutually non-dominated set of ParetoFitness
/// objects.
/// </summary>
public class MNDSet
{
    private List<ParetoFitness> m_fitnesses;
    public ReadOnlyCollection<ParetoFitness> Fitnesses;


    public MNDSet()
    {
        m_fitnesses = new();
        Fitnesses = new(m_fitnesses);
    }


    /// <summary>
    /// Compares the given pareto fitness to the rest of the set. Returns
    /// the worst comparison for the provided day that occurs in the set.
    /// </summary>
    public int Compare(ParetoFitness fitness)
    {
        int comparison = -1;
        foreach (ParetoFitness other in m_fitnesses)
        {
            if (other == null) continue;

            int otherComp = fitness.CompareTo(other);
            if (otherComp > comparison)
            {
                comparison = otherComp;
            }
        }
        return comparison;
    }


    public void Add(ParetoFitness fitness)
    {
        m_fitnesses.Add(fitness);
    }


    public void Remove(ParetoFitness fitness)
    {
        m_fitnesses.Remove(fitness);
    }


    public bool Contains(ParetoFitness fitness)
    {
        return m_fitnesses.Contains(fitness);
    }
}