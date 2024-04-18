// Commented 18/4
using System;


public partial class Day
{
    /// <summary>
    /// Compare two days in terms of their fitness.
    /// </summary>
    public int CompareTo(Day other)
    {
        return TotalFitness.CompareTo(other.TotalFitness);
    }


    /// <summary>
    /// Less-than operator performed on two Days.
    /// </summary>
    public static bool operator <(Day op1, Day op2) => op1.CompareTo(op2) < 0;


    /// <summary>
    /// Greater-than operator performed on two Days.
    /// </summary>
    public static bool operator >(Day op1, Day op2) => op1.CompareTo(op2) > 0;


    /// <summary>
    /// A class representing the fitness of an individual constraint.
    /// </summary>
    public class ConstraintFitness
    {
        /// <summary>
        /// The Day this object belongs to.
        /// </summary>
        private Day m_day;

        /// <summary>
        /// The nutrient this object represents.
        /// </summary>
        private EConstraintType m_nutrient;

        // Convert this object into a tangible float value.
        // When it is returned, a check is made to ensure it is up to date.
        // If so, returns the cached value m_value. Otherwise, recalculates the value with
        // the algorithm's corresponding constraint's GetFitness function.
        private float m_value;
        public float Value
        {
            get
            {
                if (UpToDate) return m_value;

                float amount = m_day.GetConstraintAmount(m_nutrient);
                m_value = m_day.m_algorithm.Constraints[(int)m_nutrient].GetFitness(amount);

                UpToDate = true;
                return m_value;
            }
        }

        /// <summary>
        /// If this is false, Value must be recalculated. Otherwise the cached value m_value
        /// can be used.
        /// </summary>
        public bool UpToDate { get; private set; } = false;


        public ConstraintFitness(Day day, EConstraintType nutrient)
        {
            m_day = day;
            m_nutrient = nutrient;
        }


        public void SetOutdated()
        {
            UpToDate = false;
        }
    }


    /// <summary>
    /// An abstract base class for representing the fitness of a whole day.
    /// </summary>
    public abstract class Fitness : IComparable, IVerbose
    {
        protected Day m_day;
        protected ConstraintFitness[] m_nutrientFitnesses;


        /// <summary>
        /// Whether all nutrient fitnesses are up to date. If so, don't need to sum over
        /// the nutrient fitnesses, and can use the cached m_value value.
        /// </summary>
        protected bool m_allUpToDate = false;


        /// <summary>
        /// A float representation of the fitness, whether it is the actual fitness sum,
        /// or the non-dominated rank of the solution (an integer).
        /// </summary>
        public abstract float Value { get; }


        public Fitness(Day day)
        {
            m_day = day;
            m_nutrientFitnesses = new ConstraintFitness[Constraint.Count];
            for (int i = 0; i < Constraint.Count; i++)
            {
                m_nutrientFitnesses[i] = new(day, (EConstraintType)i);
            }
        }


        /// <summary>
        /// Marks a nutrient as outdated, meaning its value must be recalculated.
        /// </summary>
        /// <param name="nutrient">The nutrient to set as outdated.</param>
        public void SetNutrientOutdated(EConstraintType nutrient)
        {
            m_allUpToDate = false;
            m_nutrientFitnesses[(int)nutrient].SetOutdated();
        }


        /// <summary>
        /// Compares this object to another object of the same type.
        /// </summary>
        /// <param name="obj">Object to compare to. Throws InvalidCastException if not the same type.</param>
        /// <returns>`-1` if it is less than obj, `0` if equal, `1` if greater.</returns>
        public abstract int CompareTo(object obj);


        public abstract string Verbose();
    }


    /// <summary>
    /// Class for fitnesses calculated through combining fitnesses of each objective into
    /// one fitness, by summing.
    /// </summary>
    public class SummedFitness : Fitness
    {
        /// <summary>
        /// The previous, cached, value of the day's summed fitness.
        /// </summary>
        private float m_value;
        public override float Value
        {
            get
            {
                if (m_allUpToDate) return m_value;

                // Sum over all nutrient fitnesses
                float sum = 0;
                for (int i = 0; i < m_nutrientFitnesses.Length; i++)
                {
                    sum += m_nutrientFitnesses[i].Value;
                }

                // Add fitness for all portions over the mass limit, corresponding to the amount over
                // the limit.
                if (Preferences.Instance.addFitnessForMass)
                {
                    foreach (Portion p in m_day.portions)
                    {
                        sum += MathF.Max(p.Mass - Preferences.Instance.maxPortionMass, 0);
                    }
                }

                m_value = sum;
                m_allUpToDate = true;
                return m_value;
            }
        }


        public SummedFitness(Day day) : base(day) { }


        public override int CompareTo(object obj)
        {
            SummedFitness other = (SummedFitness)obj;

            if (Value < other.Value) return -1;
            if (Value == other.Value) return 0;
            return 1;
        }


        public override string Verbose()
        {
            return $"{Value}";
        }
    }


    /// <summary>
    /// Class for fitnesses calculated by Pareto comparison to other members of the population.
    /// </summary>
    public class ParetoFitness : Fitness
    {
        public override float Value
        {
            get
            {
                // Only an AlgGA can create a ParetoFitness object, so safely cast to obtain the Pareto Hierarchy
                ParetoHierarchy hierarchy = ((AlgGA)m_day.m_algorithm).Hierarchy;

                int set = hierarchy.GetSet(this);

                // Algorithm handles set adding, meaning this Day is not part of the
                // population. Just add to the set, get the set and remove from the set.
                if (set == -1)
                {
                    hierarchy.Add(this);
                    set = hierarchy.GetSet(this);
                    hierarchy.Remove(this);
                }

                return set;
            }
        }


        public ParetoFitness(Day day) : base(day) { }


        public override int CompareTo(object obj)
        {
            ParetoFitness other = (ParetoFitness)obj;

            // Store how many constraints this is better/worse than `other` on.
            int betterCount = 0;
            int worseCount = 0;

            for (int i = 0; i < Constraint.Count; i++)
            {
                float fitnessA = m_nutrientFitnesses[i].Value;
                float fitnessB = other.m_nutrientFitnesses[i].Value;

                if (fitnessA < fitnessB)
                    betterCount++;
                else if (fitnessA > fitnessB)
                    worseCount++;
            }


            // If on any constraint, !(ourFitness <= fitness) then this does not dominate.
            if (worseCount > 0)
            {
                // Worse on 1+, and better on 1+ => MND
                if (betterCount > 0)
                    return 0;

                // Worse on all constraints => Strictly Dominated
                // Worse on 1+, and not better on any => Dominated
                return 1;
            }
            else
            {
                // Better on all constraints => Strictly Dominates
                // Not worse on any, and better on 1+ => Dominates
                if (betterCount > 0)
                    return -1;

                // Not worse on any, and not better on any => MND (They are equal)
                return 0;
            }
        }


        public override string Verbose()
        {
            return $"{Value}";
        }
    }
}