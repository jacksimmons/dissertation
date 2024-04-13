// Commented 13/4
/// <summary>
/// This algorithm models flocking animals, such as birds.
/// 
/// In this implementation, the swarm is drawn to low-fitness Days of food.
/// </summary>
public partial class AlgPSO : Algorithm
{
    /// <summary>
    /// An array containing all the particles in the swarm.
    /// </summary>
    private Particle[] m_particles;


    /// <summary>
    /// The swarm's global best position, and its corresponding Day object.
    /// </summary>
    private ParticleVector m_gbest;
    private Day m_gbestDay;


    public override bool Init()
    {
        // If the base Init failed, return false for failure.
        if (!base.Init()) return false;

        if (Prefs.inertialWeight > 1)
        {
            Logger.Warn($"Invalid preference: inertialWeight ({Prefs.inertialWeight}) cannot be greater than 1.");
            return false;
        }

        // Initialise particles
        m_particles = new Particle[Prefs.populationSize];
        for (int i = 0; i < m_particles.Length; i++)
        {
            m_particles[i] = new Particle(this);
        }

        // Initialise other data structures
        m_gbest = new(Foods.Count);

        return true;
    }


    /// <summary>
    /// Each iteration represents a time step for the particles.
    /// This timestep can be modified in the preferences.
    /// </summary>
    protected override void NextIteration()
    {
        // Remove all previous pbest
        ClearPopulation();

        // Handle all particle next steps
        for (int i = 0; i < m_particles.Length; i++)
        {
            m_particles[i].IndividualStep();

            // Load pbest into the population for display if one exists
            Day pbest = m_particles[i].PBest;
            if (pbest != null)
            {
                AddToPopulation(pbest);
            }
        }
    }
}