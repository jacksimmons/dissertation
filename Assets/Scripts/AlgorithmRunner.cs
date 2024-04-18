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
    private List<Day> m_bestDayEachIteration;
    public ReadOnlyCollection<Day> BestDayEachIteration { get; }


    // https://stackoverflow.com/questions/12306/can-i-serialize-a-c-sharp-type-object
    public AlgorithmRunner()
    {
        // Instantiate Alg using Algorithm.Build
        // Alg is the algorithm casted to an Algorithm object, but it can represent any
        // class which satisfies ": Algorithm".
        string algType = Preferences.Instance.algorithmType;
        Alg = Algorithm.Build(Type.GetType(algType)!);


        // Initialise plot variables
        m_bestDayEachIteration = new();
        BestDayEachIteration = new(m_bestDayEachIteration);
    }


    /// <summary>
    /// Runs a given number of iterations, and adds each iteration to the graph.
    /// </summary>
    /// <returns> A tuple, of: 
    /// string: An empty string, if there are no errors. An error message, otherwise.
    /// int: Elapsed milliseconds.</returns>
    public Tuple<string, long> RunIterations(int numIters)
    {
        // Initialise the algorithm; if it fails, return the error message.
        if (BestDayEachIteration.Count == 0)
        {
            string errorText = Alg.Init();
            if (errorText != "")
            {
                return new(errorText, 0);
            }
        }

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < numIters; i++)
        {
            Alg.RunIteration();
            m_bestDayEachIteration.Add(Alg.BestDay); // ith plot addition
        }

        sw.Stop();

        return new("", sw.ElapsedMilliseconds);
    }
}