using System;
using System.Linq;
using System.Numerics;

partial class AlgPSO
{
    protected class ParticleVector
    {
        private readonly float[] m_vector;


        /// <summary>
        /// Indexer property.
        /// </summary>
        public float this[int i]
        {
            get
            {
                return m_vector[i];
            }
        }


        /// <summary>
        /// Constructor with the length of the internal structure.
        /// </summary>
        public ParticleVector(int length)
        {
            m_vector = new float[length];
        }


        /// <summary>
        /// Constructor with a predetermined internal structure.
        /// </summary>
        public ParticleVector(float[] vector)
        {
            m_vector = vector;
        }


        /// <summary>
        /// Add a vector, with a scalar coefficient. Equivalent to C = A + kB, where C is returned.
        /// Operation guarantees that all values in C will be >= 0, even if the result would normally not.
        /// </summary>
        /// <param name="a">The first vector, which is added to.</param>
        /// <param name="b">The second vector, which is multiplied by the scalar then added to a.</param>
        /// <param name="multiplier">The scalar coefficient of the vector b.</param>
        public static ParticleVector MultAndAdd(ParticleVector a, ParticleVector b, float multiplier = 1)
        {
            if (a.m_vector.Length != b.m_vector.Length)
            {
                Logger.Warn("PSO: Invalid vector length in addition.");
                return null;
            }

            ParticleVector c = new(a.m_vector.Length);
            for (int i = 0; i < c.m_vector.Length; i++)
            {
                // Perform the calculation, ensuring no vector values go below 0.
                c.m_vector[i] = a.m_vector[i] + multiplier * b.m_vector[i];
            }

            return c;
        }


        public static ParticleVector Add(ParticleVector a, ParticleVector b)
        {
            return MultAndAdd(a, b, 1);
        }


        public static ParticleVector Subtract(ParticleVector a, ParticleVector b)
        {
            return MultAndAdd(a, b, -1);
        }


        /// <summary>
        /// Ensure all elements in the vector are >= 0.
        /// </summary>
        public void Normalise()
        {
            for (int i = 0; i < m_vector.Length; i++)
            {
                if (m_vector[i] < 0) m_vector[i] = 0;
            }
        }
    }


    /// <summary>
    /// A particle which moves around the search space, where each dimension
    /// is a food from the dataset. This leads to quite large velocity arrays.
    /// 
    /// 
    /// </summary>
    protected class Particle
    {
        private AlgPSO m_swarm;


        /// <summary>
        /// The position of the particle. In physical terms, for each index this
        /// represents the mass of the food at this index.
        /// 
        /// ! Casted down to int
        /// </summary>
        private ParticleVector m_position;


        /// <summary>
        /// An N-dimensional velocity, where N is the number of foods.
        /// Defaults to the 0-vector.
        /// </summary>
        private ParticleVector m_velocity;


        /// <summary>
        /// The particle's best position.
        /// </summary>
        private ParticleVector m_pbest;
        private Day m_pbestDay;
        public Day PBest => m_pbestDay == null ? null : new(m_pbestDay);


        public Particle(AlgPSO swarm)
        {
            m_swarm = swarm;

            // Randomly initialise the position
            float[] pos = new float[m_swarm.Foods.Count];
            for (int i = 0; i < m_swarm.Foods.Count; i++)
            {
                pos[i] = MathTools.Rand.Next(Prefs.minPortionMass, Prefs.maxPortionMass);
            }

            m_position = new ParticleVector(pos);
            m_velocity = new(m_swarm.Foods.Count);

            // Initialise the pbest as the starting position
            m_pbest = new(pos);
        }


        public void IndividualStep()
        {
            UpdatePosition();
            UpdateVelocity();
        }


        /// <summary>
        /// Sets the new position based on the velocity, and then calculates its fitness
        /// to determine if it is the gbest or pbest.
        /// </summary>
        private void UpdatePosition()
        {
            m_position = ParticleVector.MultAndAdd(m_position, m_velocity);

            // Only normalise the position so the solutions make physical sense.
            // Any incredibly negative velocity elements will just set the
            // corresponding position element to 0.
            m_position.Normalise();

            Day day = GetPositionDay();
            // Check if the position is the pbest. If so, then it is worth checking if
            // it is also the gbest.
            // position > pbest => position > gbest.
            if (CheckIfPBest(day))
            {
                CheckIfGBest(day, m_position);
            }
        }


        private Day GetPositionDay()
        {
            Day day = new(m_swarm);
            for (int i = 0; i < m_swarm.Foods.Count; i++)
            {
                int mass = (int)MathF.Ceiling(m_position[i]);
                if (mass == 0) continue; // Don't add any empty portions

                day.AddPortion(new(m_swarm.Foods[i], mass));
            }
            return day;
        }


        /// <summary>
        /// Checks if the provided position day is the
        /// best ever found by the particle. If it is, updates pbest.
        /// </summary>
        private bool CheckIfPBest(Day day)
        {
            if (m_pbestDay == null || day < m_pbestDay)
            {
                m_pbest = m_position;
                m_pbestDay = day;
                return true;
            }
            return false;
        }


        /// <summary>
        /// Checks if the provided position (and position day) is the
        /// best ever found by the swarm. If it is, updates gbest.
        /// </summary>
        private void CheckIfGBest(Day day, ParticleVector position)
        {
            if (m_swarm.m_gbestDay == null || day < m_swarm.m_gbestDay)
            {
                m_swarm.m_gbestDay = day;
                m_swarm.m_gbest = position;
            }
        }


        /// <summary>
        /// Sets the new velocity based on pbest and the swarm's gbest, as well as the
        /// Preferences acceleration coefficients, and some randomness.
        /// </summary>
        private void UpdateVelocity()
        {
            ParticleVector pbestMinusPos = ParticleVector.Subtract(m_pbest, m_position);
            ParticleVector gbestMinusPos = ParticleVector.Subtract(m_swarm.m_gbest, m_position);

            m_velocity = ParticleVector.Add(m_velocity, pbestMinusPos);
            m_velocity = ParticleVector.Add(m_velocity, gbestMinusPos);
        }
    }
}