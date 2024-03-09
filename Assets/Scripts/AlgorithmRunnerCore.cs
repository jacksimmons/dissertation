// Non-unity components of AlgorithmRunner.
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;


public class AlgorithmRunnerCore
{
    public Algorithm Alg { get; private set; }


#if !UNITY_64
    private List<Tuple<float, float, float>> m_datapoints = new();
    public ReadOnlyCollection<Tuple<float, float, float>> datapoints;
#endif


    // https://stackoverflow.com/questions/12306/can-i-serialize-a-c-sharp-type-object
    public AlgorithmRunnerCore()
    {
        // End any existing algorithms.
        Algorithm.EndAlgorithm();


        // Instantiate Alg using Algorithm.Build
        // Alg is the algorithm casted to an Algorithm object, but it can represent any
        // class which satisfies ": Algorithm".
        string algType = Preferences.Instance.algorithmType;
        Alg = Algorithm.Build(Type.GetType(algType));


        // Assign the singleton instance
        Algorithm.Instance = Alg;


        // Perform any work that can't be done in the constructor.
        Alg.Init();

#if !UNITY_64
        datapoints = new(m_datapoints);
#endif
    }


    /// <returns>The time taken for the iterations to pass.</returns>
    public float RunIterations(int numIters)
    {
        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < numIters; i++)
        {
            Alg.RunIteration();
        }

        sw.Stop();

#if !UNITY_64
        AddToGraph(Algorithm.Instance.BestFitness, Algorithm.Instance.AverageFitness);
#endif

        return sw.ElapsedMilliseconds;
    }


    /// <summary>
    /// Add the results to datapoints, to be output in a graph later.
    /// </summary>
#if !UNITY_64
    private void AddToGraph(float bestFitness, float avgFitness)
    {
        m_datapoints.Add(new(bestFitness, 0, Algorithm.Instance.IterNum));
    }
#endif
}