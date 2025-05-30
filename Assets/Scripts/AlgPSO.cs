// Commented 13/4
using System.Collections.ObjectModel;

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
    public ReadOnlyCollection<Particle> Particles { get; set; }


    /// <summary>
    /// The swarm's global best position, and its corresponding Day object.
    /// </summary>
    private ParticleVector m_gbest;
    private Day m_gbestDay;


    public override string Init()
    {
        // ----- Preference error checking -----
        string errorText = base.Init();
        if (errorText != "") return errorText; // Parent initialisation, and ensure no errors have occurred.
		// ----- END -----

        // Initialise particles
        m_particles = new Particle[Prefs.populationSize];
        Particles = new(m_particles);
        for (int i = 0; i < m_particles.Length; i++)
        {
            m_particles[i] = new Particle(this);
        }

        // Initialise other data structures
        m_gbest = new(Foods.Count);

        return "";
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