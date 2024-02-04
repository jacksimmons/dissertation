using System;
using TMPro;
using UnityEngine;

public class AlgorithmRunner : MonoBehaviour
{
    public Algorithm Algorithm { get; private set; }

    [SerializeField]
    private TMP_Text m_populationText;
    [SerializeField]
    private TMP_Text m_iterNumText;
    [SerializeField]
    private PopulationView m_populationView;
    [SerializeField]
    private TMP_Text m_iterTimeTakenText;


    public void Init()
    {
        Algorithm = new GeneticAlgorithm();
        UpdateAlgorithmUI();
    }


    private void UpdateAlgorithmUI(float time_ms = 0)
    {
        m_populationView.UpdatePopView();
        m_iterNumText.text = $"Iteration: {Algorithm.NumIterations}";
        m_iterTimeTakenText.text = $"Average Iteration Time: {time_ms}";
    }


    /// <summary>
    /// Runs multiple next iterations, and updates the UI only once.
    /// </summary>
    public void RunIterations(int numIters)
    {
        m_populationView.ClearPortionUI();

        DateTime before = DateTime.Now;
        for (int i = 0; i < numIters; i++)
        {
            Algorithm.NextIteration();
        }
        DateTime after = DateTime.Now;
        float ms = after.Subtract(before).Milliseconds / numIters;
        UpdateAlgorithmUI(ms);
    }
}