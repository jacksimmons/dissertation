// Non-unity components of AlgorithmRunner.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;


public class AlgorithmRunner
{
    public Algorithm Alg { get; private set; }

    private List<Coordinates> m_plot;
    public ReadOnlyCollection<Coordinates> Plot { get; }


    // https://stackoverflow.com/questions/12306/can-i-serialize-a-c-sharp-type-object
    public AlgorithmRunner()
    {
        // Instantiate Alg using Algorithm.Build
        // Alg is the algorithm casted to an Algorithm object, but it can represent any
        // class which satisfies ": Algorithm".
        string algType = Preferences.Instance.algorithmType;
        Alg = Algorithm.Build(Type.GetType(algType)!);


        // Perform any work that can't be done in the constructor.
        Alg.Init();


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
            m_plot.Add(new(Alg.IterNum, Alg.BestFitness));
        }

        sw.Stop();

        return sw.ElapsedMilliseconds;
    }
}