using TMPro;
using UnityEngine;

public class AlgorithmRunner : MonoBehaviour
{
    private Algorithm m_algorithm;

    [SerializeField]
    private TMP_Text m_populationText;
    [SerializeField]
    private TMP_Text m_iterNumText;
    [SerializeField]
    private PopulationView m_populationView;


    public void Init()
    {
        m_algorithm = new GeneticAlgorithm();
        UpdateAlgorithmUI();
    }


    private void UpdateAlgorithmUI()
    {
        m_populationView.UpdatePopView(m_algorithm);
        m_iterNumText.text = $"Iteration: {m_algorithm.NumIterations}";
    }


    /// <summary>
    /// Runs a single iteration of the algorithm, and updates the UI accordingly.
    /// </summary>
    public void NextIteration()
    {
        m_populationView.ClearPortionUI();
        m_algorithm.NextIteration();
        UpdateAlgorithmUI();
    }


    /// <summary>
    /// Runs multiple next iterations, and updates the UI only once.
    /// </summary>
    public void MultipleNextIterations(int numIters)
    {
        m_populationView.ClearPortionUI();
        for (int i = 0; i < numIters; i++)
        {
            m_algorithm.NextIteration();
        }
        UpdateAlgorithmUI();
    }
}