using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Handles the iterations of an algorithm, as well as construction/destruction.
/// </summary>
public class AlgorithmRunner : MonoBehaviour
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

    private AlgorithmRunnerCore m_core;


    public void Init()
    {
        m_core = new();

        if (Algorithm.DatasetError != "")
        {
            m_menu.ShowPopup("Error", Algorithm.DatasetError, Color.red);
            return;
        }

        UpdateAlgorithmUI(0, 0);
    }


    private void UpdateAlgorithmUI(float time_ms, int iters)
    {
        List<Day> challengers = new(Algorithm.Population);

        //
        // For every day in the population, check if we have any that are better
        // than the existing champions. If so, add them once and remove any champions
        // that they beat.
        //
        //foreach (Day challenger in challengers)
        //{
        //    List<Day> oldChamps = new(m_champions);
        //    bool added = false;

        //    if (oldChamps.Count == 0)
        //    {
        //        m_champions.Add(challenger);
        //        continue;
        //    }

        //    foreach (Day champion in oldChamps)
        //    {
        //        switch (Day.Compare(challenger, champion))
        //        {
        //            case ParetoComparison.StrictlyDominates:
        //            case ParetoComparison.Dominates:
        //                m_champions.Remove(champion);
        //                if (!added)
        //                {
        //                    m_champions.Add(challenger);
        //                    added = true;
        //                }
        //                break;
        //            case ParetoComparison.MutuallyNonDominating:
        //                if (!added)
        //                {
        //                    m_champions.Add(challenger);
        //                    added = true;
        //                }
        //                break;
        //        }
        //    }
        //}

        //float domFitness = m_champions.Count > 0 ? m_champions[0].GetFitness() : 0;
        //m_domFitnessText.text = $"Dominant Fitness ({m_champions.Count}): {domFitness}";
        
        m_populationView.UpdatePopView();
        m_iterNumText.text = $"Iteration: {m_core.IterNum}";
        m_iterTimeTakenText.text = $"Execution Time ({iters} iters): {time_ms}ms";
    }


    /// <summary>
    /// Runs multiple next iterations, and updates the UI only once.
    /// </summary>
    public void RunIterations(int numIters)
    {
        float ms = m_core.RunIterations(numIters);
        UpdateAlgorithmUI(ms, numIters);
    }
}