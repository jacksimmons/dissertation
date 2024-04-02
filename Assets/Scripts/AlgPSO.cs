using System.Numerics;

/// <summary>
/// This algorithm models flocking animals, such as birds,
/// which undergo a "Cornfield Vector", drawing them toward
/// nice-smelling foods.
/// 
/// In this implementation, the birds are drawn to low-fitness
/// days (e.g. a bird feeder with an entire day's worth of food).
/// </summary>
public partial class AlgPSO : Algorithm
{
    private Particle[] m_particles;


    /// <summary>
    /// The swarm's global best position.
    /// </summary>
    private ParticleVector m_gbest;
    private Day m_gbestDay;



    public override bool Init()
    {
        if (!base.Init()) return false;

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