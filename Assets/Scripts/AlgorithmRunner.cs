// Non-unity components of AlgorithmRunner.
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;


public class AlgorithmRunner
{
    public Algorithm Alg { get; private set; }


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

        return sw.ElapsedMilliseconds;
    }
}