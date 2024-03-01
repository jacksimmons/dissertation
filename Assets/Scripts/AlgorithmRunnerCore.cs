// Non-unity components of AlgorithmRunner.
using System.Diagnostics;


public class AlgorithmRunnerCore
{
    public Algorithm Alg { get; private set; }
    public int IterNum { get; private set; }


    public AlgorithmRunnerCore()
    {
        Algorithm.EndAlgorithm();
        IterNum++;

        switch (Preferences.Instance.algType)
        {
            case AlgorithmType.GA:
                switch (Preferences.Instance.gaType)
                {
                    case GAType.SummedFitness:
                        Alg = new AlgSFGA();
                        break;
                    case GAType.ParetoDominance:
                        Alg = new AlgPDGA();
                        break;
                }
                break;
            case AlgorithmType.ACO:
                Alg = new AlgACO();
                break;
        }

        Algorithm.Instance = Alg;
        Alg.PostConstructorSetup();
    }


    /// <returns>The time taken for the iterations to pass.</returns>
    public float RunIterations(int numIters)
    {
        Stopwatch sw = Stopwatch.StartNew();
        IterNum += numIters;

        for (int i = 0; i < numIters; i++)
        {
            Alg.NextIteration();
        }

        sw.Stop();
        return sw.ElapsedMilliseconds;
    }
}