using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class AlgorithmRunner : MonoBehaviour
{
    public Algorithm Algorithm { get; private set; }

    [SerializeField]
    private TMP_Text m_domFitnessText;
    [SerializeField]
    private TMP_Text m_avgFitnessText;
    [SerializeField]
    private TMP_Text m_iterTimeTakenText;
    [SerializeField]
    private TMP_Text m_iterNumText;
    [SerializeField]
    private PopulationView m_populationView;

    private List<Day> m_champions;


    public void Init()
    {
        m_champions = new();
        Algorithm = new GeneticAlgorithm();
        Algorithm.CalculateInitialFitnesses();
        UpdateAlgorithmUI(0, 0);
    }


    private void UpdateAlgorithmUI(float time_ms, int iters)
    {
        float fitnessSum = 0;
        foreach (var kvp in Algorithm.Population)
        {
            fitnessSum += kvp.Value;
        }
        float fitnessAvg = fitnessSum / Algorithm.Population.Count;

        List<Day> challengers = Algorithm.Population.Keys.ToList();

        //
        // For every day in the population, check if we have any that are better
        // than the existing champions. If so, add them once and remove any champions
        // that they beat.
        //
        foreach (Day challenger in challengers)
        {
            List<Day> oldChamps = new(m_champions);
            bool added = false;

            if (oldChamps.Count == 0)
            {
                m_champions.Add(challenger);
                continue;
            }

            foreach (Day champion in oldChamps)
            {
                switch (Day.Compare(challenger, champion))
                {
                    case ParetoComparison.StrictlyDominates:
                    case ParetoComparison.Dominates:
                        m_champions.Remove(champion);
                        if (!added)
                        {
                            m_champions.Add(challenger);
                            added = true;
                        }
                        break;
                    case ParetoComparison.MutuallyNonDominating:
                        if (!added)
                        {
                            m_champions.Add(challenger);
                            added = true;
                        }
                        break;
                }
            }
        }

        float domFitness = m_champions.Count > 0 ? m_champions[0].GetFitness() : 0;
        m_domFitnessText.text = $"Dominant Fitness ({m_champions.Count}): {domFitness}";
        m_avgFitnessText.text = $"Average Fitness: {fitnessAvg}";
        m_populationView.UpdatePopView();
        m_iterNumText.text = $"Iteration: {Algorithm.NumIterations}";
        m_iterTimeTakenText.text = $"Execution Time ({iters} iters): {time_ms}ms";
    }


    /// <summary>
    /// Runs multiple next iterations, and updates the UI only once.
    /// </summary>
    public void RunIterations(int numIters)
    {
        m_populationView.ClearPortionUI();

        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < numIters; i++)
        {
            Algorithm.NextIteration();
        }
        sw.Stop();
        float ms = sw.ElapsedMilliseconds;
        UpdateAlgorithmUI(ms, numIters);
    }
}