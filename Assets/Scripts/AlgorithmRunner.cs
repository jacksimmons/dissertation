// Commented 8/4
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using UnityEngine;


/// <summary>
/// A class which handles the running and performance logging of any type of algorithm from start to end.
/// </summary>
public class AlgorithmRunner
{
    public Algorithm Alg { get; private set; }

    // A plot containing Iteration for x and Best Fitness for y. Goes from Iteration 0 to Iteration NumIters.
    private List<float> m_plot;
    public ReadOnlyCollection<float> Plot { get; }


    // https://stackoverflow.com/questions/12306/can-i-serialize-a-c-sharp-type-object
    public AlgorithmRunner()
    {
        // Instantiate Alg using Algorithm.Build
        // Alg is the algorithm casted to an Algorithm object, but it can represent any
        // class which satisfies ": Algorithm".
        string algType = Preferences.Instance.algorithmType;
        Alg = Algorithm.Build(Type.GetType(algType)!);


        // Handle algorithm initialisation. If it fails, go back to the previous menu. (Which must be AlgorithmSetup)
        if (!Alg.Init())
        {
            GameObject.FindWithTag("MenuStackHandler").GetComponent<MenuStackHandler>().OnBackPressed();
            return;
        }


        // Initialise plot variables
        m_plot = new();
        Plot = new(m_plot);
    }


    /// <summary>
    /// Runs a given number of iterations, and adds each iteration to the graph.
    /// </summary>
    /// <returns>The time taken for the iterations to pass.</returns>
    public float RunIterations(int numIters)
    {
        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < numIters; i++)
        {
            Alg.RunIteration();

            // Always plot the graph using a summed-fitness measure.
            Day.SummedFitness sf = new(Alg.BestDay);
            m_plot.Add(sf.Value); // ith plot addition
        }

        sw.Stop();

        return sw.ElapsedMilliseconds;
    }
}