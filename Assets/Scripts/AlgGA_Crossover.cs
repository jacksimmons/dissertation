using System;
using System.Collections.Generic;


/// <summary>
/// Runs crossover from the point that the parents' genes have been
/// combined into one list.
/// </summary>
public partial class AlgGA
{
    /// <summary>
    /// Applies genetic crossover from parents to children.
    /// This crossover works by turning each Day into fine data, by using the
    /// mass of each portion as a unit of measurement.
    /// 
    /// So it first looks at the proportion of portions entirely included by
    /// the crossover point (in day 1 then day 2), and finds the first portion not fully
    /// included (this can be in day 1 or 2).
    /// 
    /// It then goes down to the mass-level, and splits the portion at the point where total
    /// mass of the Day so far equals (total mass of both days) * crossoverPoint.
    /// 
    /// The rest of the portion, and the other portions after that (including the entirety
    /// of day 2, if the crossover point is less than 0.5) goes into the other child.
    /// </summary>
    /// <param name="parents">Parents to crossover genes from.</param>
    /// <returns>Two children with only crossover applied.</returns>
    public Tuple<Day, Day> Crossover(Tuple<Day, Day> parents)
    {
        Tuple<Day, Day> children = new(new(this), new(this));

        /// The sum of all portion masses before this one.
        int massSum = 0;

        // Sorted list of cutoff masses, in ascending order.
        List<int> cutoffMasses = GetCutoffMasses(Preferences.Instance.crossoverPoints, parents);

        List<Portion> parentPortions = new();
        parentPortions.AddRange(parents.Item1.portions);
        parentPortions.AddRange(parents.Item2.portions);

        /// true => Portions are to be added to the left child.
        /// false => Portions are to be added to the right child.
        /// Flips after the next cutoff mass is surpassed.
        bool addToLeftChild = true;

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

        foreach (Portion portion in parentPortions)
        {
            int mass = portion.Mass;

            if (cutoffMasses.Count > 0 && massSum + mass > cutoffMasses[0])
            {
                HandleAllCrossoversWithinPortion(portion, children, cutoffMasses, ref addToLeftChild, ref massSum);
            }
            else
            {
                if (addToLeftChild) { children.Item1.AddPortion(portion); }
                else { children.Item2.AddPortion(portion); }

                massSum += mass;
            }
        }


        // PATCHED: A bug could occur where the second child was given none of the portions.
        //if (m_children.Item2.Mass == 0 && !(m_parentPortions.Count == 0))
        //{
        //    Logger.Error("Crossover was not split correctly.");
        //}


        return children;
    }


    /// <summary>
    /// Adds a portion to a children tuple based on the addToLeftChild boolean.
    /// </summary>
    private void AddPortionToChild(Portion portion, Tuple<Day, Day> children, bool addToLeftChild)
    {
        if (addToLeftChild)
        {
            children.Item1.AddPortion(portion);
        }
        else
        {
            children.Item2.AddPortion(portion);
        }
    }


    /// <summary>
    /// Generates cutoff mass values, from a random range of 0 to 1.
    /// </summary>
    /// <returns>The masses where crossover applies to the genetic material.</returns>
    private List<int> GetCutoffMasses(int num, Tuple<Day, Day> parents)
    {
        List<int> cutoffMasses = new(num);

        // Total mass stored in both days
        int massGrandTotal = parents.Item1.Mass + parents.Item2.Mass;

        for (int i = 0; i < num; i++)
        {
            // A random split between the two parents
            float crossoverPoint = (float) Rand.NextDouble();

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
    /// <param name="children">The children to split the portion between.</param>
    /// <param name="cutoffMasses">The amount of mass "through" the portion at which crossover occurs.</param>
    private void HandleAllCrossoversWithinPortion(Portion portion, Tuple<Day, Day> children, List<int> cutoffMasses, ref bool addToLeftChild, ref int massSum)
    {
        // If the sub-portion has not surpassed the next cutoff point (or there is no next cutoff point), add the portion to the child to be added to.
        if (cutoffMasses.Count <= 0 || massSum + portion.Mass < cutoffMasses[0])
        {
            AddPortionToChild(portion, children, addToLeftChild);
            return;
        }

        // The mass sum surpassed the cutoff point by addition of portion's mass, so portion
        // must be split.
        int exactCrossoverPt = cutoffMasses[0] - massSum;
        Portion rightSub = HandleSplitPortion(portion, children, exactCrossoverPt, ref addToLeftChild);

        // Add the left subportion's mass to the mass sum, as it was added to the current child being added to.
        massSum += exactCrossoverPt;

        // Remove the current cutoff mass; it has been handled.
        cutoffMasses.RemoveAt(0);

        // Recurse to find further portion splits for the right subportion (if there are any cutoff pts remaining)
        HandleAllCrossoversWithinPortion(rightSub, children, cutoffMasses, ref addToLeftChild, ref massSum);
    }


    /// <summary>
    /// Handles splitting of the middle (cutoff) portion in the portion list.
    /// Generally splits the portion at the cutoff mass provided, giving the "left" side to the child
    /// currently being added to, and the rest to the right child.
    /// Then flips the "addToLeftChild" boolean so that the other child becomes the one being added to.
    /// </summary>
    /// <param name="portion">The portion to be split.</param>
    /// <param name="children">The children to split a part of the portion between.</param>
    /// <param name="exactCrossoverPt">The remaining cutoff after last whole portion.</param>
    private Portion HandleSplitPortion(Portion portion, Tuple<Day, Day> children, int exactCrossoverPt, ref bool addToLeftChild)
    {
        Portion right;

        if (exactCrossoverPt != 0)
        {
            Tuple<Portion, Portion> split = SplitPortion(portion, exactCrossoverPt);

            if (addToLeftChild)
                children.Item1.AddPortion(split.Item1);
            else
                children.Item2.AddPortion(split.Item1);

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
        addToLeftChild = !addToLeftChild;
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
        Portion left = new(portion.FoodType, cutoffMass);
        Portion right = new(portion.FoodType, portion.Mass - cutoffMass);

        if (cutoffMass < 0 || portion.Mass - cutoffMass < 0)
            Logger.Error($"Invalid cutoff mass: Portion mass: {portion.Mass}, Cutoff mass: {cutoffMass}");

        return new(left, right);
    }
}