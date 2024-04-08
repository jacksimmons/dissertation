using System.Linq;
using TMPro;
using UnityEngine;


/// <summary>
/// Handles the iterations of an algorithm, as well as construction/destruction.
/// </summary>
public class AlgorithmBehaviour : MonoBehaviour
{
    public Algorithm Algorithm
    {
        get { return m_core.Alg; }
    }

    [SerializeField]
    private MenuStackHandler m_menu;
    [SerializeField]
    private TMP_Text m_iterTimeTakenText;
    [SerializeField]
    private TMP_Text m_iterNumText;
    [SerializeField]
    private PopulationView m_populationView;

    private AlgorithmRunner m_core;


    public void Init()
    {
        m_core = new();
        ResetAlgorithmUI();
    }


    private void ResetAlgorithmUI()
    {
        UpdateAlgorithmUI(0, 0);
    }


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
        float ms = m_core.RunIterations(numIters);
        UpdateAlgorithmUI(ms, numIters);
    }


    /// <summary>
    /// Plots a graph of Iteration against Best Fitness for the given iteration.
    /// </summary>
    public void PlotGraph()
    {
        PlotTools.PlotLine(new(Algorithm, m_core.Plot.ToArray()));
    }
}