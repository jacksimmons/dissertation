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

public class AlgorithmRunner : MonoBehaviour
{
    public Algorithm Algorithm { get; private set; }

    [SerializeField]
    private MenuStackHandler m_menu;
    [SerializeField]
    private TMP_Text m_iterTimeTakenText;
    [SerializeField]
    private TMP_Text m_iterNumText;
    [SerializeField]
    private PopulationView m_populationView;

    private int m_iterNum;


    public void Init()
    {
        Algorithm.EndAlgorithm();
        m_iterNum = 1;

        switch (Preferences.Instance.algType)
        {
            case AlgorithmType.GA:
                switch (Preferences.Instance.gaType)
                {
                    case GAType.SummedFitness:
                        Algorithm = new SummedFitnessGA();
                        break;
                    case GAType.ParetoDominance:
                        Algorithm = new ParetoDominanceGA();
                        break;
                }
                break;
            case AlgorithmType.ACO:
                Algorithm = new ACO();
                break;
        }

        if (Algorithm.DatasetError != "")
        {
            m_menu.ShowPopup("Error", Algorithm.DatasetError, Color.red);
            return;
        }

        Algorithm.PostConstructorSetup();

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
        m_iterNumText.text = $"Iteration: {m_iterNum}";
        m_iterTimeTakenText.text = $"Execution Time ({iters} iters): {time_ms}ms";
    }


    /// <summary>
    /// Runs multiple next iterations, and updates the UI only once.
    /// </summary>
    public void RunIterations(int numIters)
    {
        Stopwatch sw = Stopwatch.StartNew();
        m_iterNum += numIters;

        for (int i = 0; i < numIters; i++)
        {
            Algorithm.NextIteration();
        }

        sw.Stop();
        float ms = sw.ElapsedMilliseconds;
        UpdateAlgorithmUI(ms, numIters);
    }
}