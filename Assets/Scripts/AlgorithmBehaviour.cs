// Commented 20/4
using System.Linq;
using TMPro;
using UnityEngine;


/// <summary>
/// Handles communication between Unity (frontend) and the backend.
/// </summary>
public sealed class AlgorithmBehaviour : MonoBehaviour
{
    public Algorithm Algorithm
    {
        get { return m_core.Alg; }
    }

    [SerializeField]
    private TMP_Text m_iterTimeTakenText;
    [SerializeField]
    private TMP_Text m_iterNumText;

    /// <summary>
    /// The Unity script handling the population view UI.
    /// </summary>
    [SerializeField]
    private PopulationView m_populationView;

    /// <summary>
    /// The object which handles execution of the algorithm.
    /// </summary>
    private AlgorithmRunner m_core;

    /// <summary>
    /// The experiment GameObject in the scene's script.
    /// </summary>
    [SerializeField]
    private ExperimentBehaviour m_experiment;


    /// <summary>
    /// Initialises the AlgorithmRunner and the UI.
    /// </summary>
    public void Init()
    {
        m_core = new();
        ResetAlgorithmUI();
    }


    /// <summary>
    /// Resets the UI elements corresponding to the algorithm, to their default values.
    /// </summary>
    private void ResetAlgorithmUI()
    {
        UpdateAlgorithmUI(0, 0);
    }


    /// <summary>
    /// Updates all UI elements corresponding to the algorithm - the population view,
    /// and the execution stats.
    /// </summary>
    /// <param name="time_ms">The time, in ms, taken to execute the iterations.</param>
    /// <param name="iters">The number of iterations that were just executed.</param>
    private void UpdateAlgorithmUI(float time_ms, int iters)
    {
        m_populationView.UpdatePopView();
        m_iterNumText.text = $"Iteration: {Algorithm.IterNum}";
        m_iterTimeTakenText.text = $"Execution Time ({iters} iters): {time_ms}ms";
    }


    /// <summary>
    /// Runs multiple next iterations, and updates the UI and graph file only once.
    /// </summary>
    public void RunIterations(int numIters)
    {
        var result = m_core.RunIterations(numIters);
        if (result.Item1 != "")
        {
            Logger.Warn(result.Item1);
            return;
        }
        else
        {
            UpdateAlgorithmUI(result.Item2, numIters);
        }
    }


    /// <summary>
    /// Plots a graph of Iteration against Best Fitness for the given iteration.
    /// </summary>
    public void PlotGraph()
    {
        PlotTools.PlotLine(new(m_core.BestDayEachIteration.ToArray()), m_experiment.GetBaseline());
    }
}