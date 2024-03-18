using System;
using System.Collections.Generic;


/// <summary>
/// Runs crossover from the point that the parents' genes have been
/// combined into one list.
/// </summary>
public class CrossoverRunner
{
    private const int N = 1;

    /// <summary>
    /// The parents to crossover genes from.
    /// </summary>
    private readonly Tuple<Day, Day> m_parents;

    /// <summary>
    /// The children to crossover between.
    /// </summary>
    private readonly Tuple<Day, Day> m_children;

    /// <summary>
    /// The sum of all portion masses before this one.
    /// </summary>
    private int m_massSum;

    /// <summary>
    /// Sorted list of cutoff masses, in ascending order.
    /// </summary>
    private readonly List<int> m_cutoffMasses;
    private readonly List<Portion> m_parentPortions;

    /// <summary>
    /// true => Portions are to be added to the left child.
    /// false => Portions are to be added to the right child.
    /// Flips after the next cutoff mass is surpassed.
    /// </summary>
    private bool m_addToLeftChild;


    public CrossoverRunner(Tuple<Day, Day> parents, Algorithm alg)
    {
        m_parents = parents;
        m_children = new(new(alg), new(alg));

        m_massSum = 0;

        m_cutoffMasses = GetCutoffMasses(N);

        m_parentPortions = new();
        m_parentPortions.AddRange(parents.Item1.portions);
        m_parentPortions.AddRange(parents.Item2.portions);

        m_addToLeftChild = true;
    }


    /// <summary>
    /// Adds a portion to a children tuple based on the addToLeftChild boolean.
    /// </summary>
    private void AddPortionToChild(Portion portion)
    {
        if (m_addToLeftChild)
        {
            m_children.Item1.AddPortion(portion);
        }
        else
        {
            m_children.Item2.AddPortion(portion);
        }
    }


    /// <summary>
    /// Generates cutoff mass values, from a random range of 0 to 1.
    /// </summary>
    /// <returns>The masses where crossover applies to the genetic material.</returns>
    private List<int> GetCutoffMasses(int num)
    {
        List<int> cutoffMasses = new(num);

        // Total mass stored in both days
        int massGrandTotal = m_parents.Item1.Mass + m_parents.Item2.Mass;

        for (int i = 0; i < num; i++)
        {
            // A random split between the two parents
            float crossoverPoint = (float)Algorithm.Rand.NextDouble();

            float floatCutoffMass = massGrandTotal * crossoverPoint;
            int cutoffMass = (int)floatCutoffMass;

            // Remove LB edge case, where left gets all portions and right gets none
            if (cutoffMass == 0)
                cutoffMass++;
            // Remove UB edge case (need to confirm if this is possible)
            else if (cutoffMass == massGrandTotal)
                cutoffMass--;

            cutoffMasses.Add(cutoffMass);
        }

        cutoffMasses.Sort();
        return cutoffMasses;
    }


    public Tuple<Day, Day> NPointCrossover()
    {
        //
        // Loop to crossover portions between left and right children.
        //
        // Pseudocode:

        // For each portion:

        // 0. If not all crossovers have been completed...
        // Check if the smallest cutoff mass (next crossover to occur) is surpassed by adding portion [i].
        // (Reminder: The cutoff masses are sorted in increasing mass; we are adding portions in increasing
        // mass, hence only need to check the first cutoff mass.)

        // -- If so:
        // -- Add the portion's mass to the mass sum.
        // -- Split this portion at the exact crossover point (into two portions), and give one of the sub-
        // -- -portions to each of the children. The left subportion goes to the child currently being added to.
        // -- Change the child currently being added to.
        // -- Remove the smallest cutoff mass
        // -- Add the left sub portion's mass to the mass sum.

        // -- Otherwise:
        // -- No crossovers were surpassed => add the portion to the child currently being added to
        // -- Add the portion's mass to the mass sum

        // Goto next portion.

        foreach (Portion portion in m_parentPortions)
        {
            int mass = portion.Mass;

            if (m_cutoffMasses.Count > 0 && m_massSum + mass > m_cutoffMasses[0])
            {
                HandleAllCrossoversWithinPortion(portion);
            }
            else
            {
                if (m_addToLeftChild) { m_children.Item1.AddPortion(portion); }
                else { m_children.Item2.AddPortion(portion); }

                m_massSum += mass;
            }
        }


        if (m_children.Item2.Mass == 0)
        {
            Logger.Error("Crossover was not split correctly.");
        }


        return m_children;
    }


    /// <summary>
    /// Recursive function for performing all portion splits that occur within a single portion.
    /// 
    /// Recursive evaluation:
    /// Base case 1- the portion does not surpass the next cutoff point.
    /// Base case 2- there are no cutoff points remaining.
    /// Recursive case- It is unclear whether there will be a further cutoff point in the right subportion of the current call. Need to check;
    /// recurse.
    /// 
    /// In either base case, need to add the portion to the current child being added to, to reflect the fact that the mass sum gets that portion's mass
    /// added to it after this call.
    /// 
    /// </summary>
    /// <param name="portion">The portion to check if it needs to be split.</param>
    private void HandleAllCrossoversWithinPortion(Portion portion)
    {
        // If the sub-portion has not surpassed the next cutoff point (or there is no next cutoff point), add the portion to the child to be added to.
        if (m_cutoffMasses.Count <= 0 || m_massSum + portion.Mass < m_cutoffMasses[0])
        {
            AddPortionToChild(portion);
            return;
        }

        // The mass sum surpassed the cutoff point by addition of portion's mass, so portion
        // must be split.
        int exactCrossoverPt = m_cutoffMasses[0] - m_massSum;
        Portion rightSub = HandleSplitPortion(portion, exactCrossoverPt);
        
        // Add the left subportion's mass to the mass sum, as it was added to the current child being added to.
        m_massSum += exactCrossoverPt;

        // Remove the current cutoff mass; it has been handled.
        m_cutoffMasses.RemoveAt(0);

        // Recurse to find further portion splits for the right subportion (if there are any cutoff pts remaining)
        HandleAllCrossoversWithinPortion(rightSub);
    }


    /// <summary>
    /// Handles splitting of the middle (cutoff) portion in the portion list.
    /// Generally splits the portion at the cutoff mass provided, giving the "left" side to the child
    /// currently being added to, and the rest to the right child.
    /// Then flips the "addToLeftChild" boolean so that the other child becomes the one being added to.
    /// </summary>
    /// <param name="portion">The portion to be split.</param>
    /// <param name="exactCrossoverPt">The remaining cutoff after last whole portion.</param>
    private Portion HandleSplitPortion(Portion portion, int exactCrossoverPt)
    {
        Portion right;

        if (exactCrossoverPt != 0)
        {
            Tuple<Portion, Portion> split = SplitPortion(portion, exactCrossoverPt);

            if (m_addToLeftChild)
                m_children.Item1.AddPortion(split.Item1);
            else
                m_children.Item2.AddPortion(split.Item1);

            right = split.Item2;
        }

        // If the local cutoff point is 0, then the right subportion is 100% of the portion.
        // Note: The opposite case for the left child is already handled in the portion split for loop,
        // so unlike this case, it will not lead to empty portions.
        else
        {
            right = portion;
        }

        // Change the child to be added to, as a crossover just occurred.
        m_addToLeftChild = !m_addToLeftChild;
        return right;
    }


    /// <summary>
    /// Splits a portion into two, at a given cutoff point in grams.
    /// </summary>
    /// <param name="portion">The portion to split.</param>
    /// <param name="cutoffMass">The local cutoff point - amount of mass
    /// which goes into the left portion. The rest of the portion goes to
    /// the right portion.</param>
    /// <returns>Two new portions, split from the original.</returns>
    private static Tuple<Portion, Portion> SplitPortion(Portion portion, int cutoffMass)
    {
        Portion left = new(portion.food, cutoffMass);
        Portion right = new(portion.food, portion.Mass - cutoffMass);

        if (cutoffMass < 0 || portion.Mass - cutoffMass < 0)
        {
            Logger.Log($"Mass: {portion.Mass}, cutoff pt: {cutoffMass}", Severity.Error);
        }

        return new(left, right);
    }
}