/// <summary>
/// The stats of a single algorithm.
/// </summary>
public readonly struct AlgorithmResult : IVerbose
{
    public readonly Day BestDay { get; }
    public readonly float BestFitness { get; }
    public readonly float[] BestFitnessEachIteration { get; }


    public AlgorithmResult(Algorithm alg, float[] bestFitnessEachIteration)
    {
        BestDay = alg.BestDay;
        BestFitness = alg.BestFitness;
        BestFitnessEachIteration = bestFitnessEachIteration;
    }


    public string Verbose()
    {
        return $"Best day:\n----------\nFitness: {BestFitness}\n{BestDay.Verbose()}\n";
    }
}


/// <summary>
/// The stats of a set of algorithms.
/// </summary>
public readonly struct AlgorithmSetResult : IVerbose
{
    public readonly AlgorithmResult[] Results { get; }
    public readonly int BestIndex { get; }
    public readonly AlgorithmResult BestResult => Results[BestIndex];

    public readonly float[] AverageBestFitnessEachIteration { get; }


    public AlgorithmSetResult(AlgorithmResult[] results)
    {
        if (results.Length < 1)
        {
            Logger.Warn($"Could not construct AlgorithmSetResult from {results.Length} AlgorithmResults.");
        }

        int numIters = results[0].BestFitnessEachIteration.Length;
        int numAlgs = results.Length;

        Results = results;
        AverageBestFitnessEachIteration = new float[numIters];

        // Assign a default best
        BestIndex = 0;
        for (int i = 0; i < results.Length; i++)
        {
            // Check if the result is the best so far
            if (results[i].BestFitness < results[BestIndex].BestFitness)
            {
                BestIndex = i;
            }

            for (int j = 0; j < numIters; j++)
            {
                AverageBestFitnessEachIteration[j] += results[i].BestFitnessEachIteration[j] / numAlgs;
            }
        }
    }


    public string Verbose()
    {
        return $"Best algorithm:\n-=-=-=-=-=-\n{BestResult.Verbose()}\n=-=-=-=-=-\n";
    }
}


/// <summary>
/// The stats of a full experiment, which consists of a set of steps, each of which has its own
/// algorithm set result.
/// </summary>
public readonly struct ExperimentResult : IVerbose
{
    public readonly AlgorithmSetResult[] Sets { get; }
    public readonly object[] Steps { get; }

    public readonly AlgorithmSetResult BestSet { get; }
    public readonly object BestStep { get; }

    // The average of the (average best fitness each iteration)s from each set.
    public readonly float[] Avg2BestFitnessEachIteration { get; }
    public readonly string Name { get; }


    public ExperimentResult(AlgorithmSetResult[] sets, object[] steps, string name)
    {
        if (sets.Length < 1)
            Logger.Error($"Could not construct ExperimentResult from {sets.Length} AlgorithmSetResults.");

        Sets = sets;
        Steps = steps;

        int numIters = sets[0].AverageBestFitnessEachIteration.Length;
        int numSets = sets.Length;

        Avg2BestFitnessEachIteration = new float[numIters];

        // Set default best
        BestSet = sets[0];
        BestStep = steps[0];

        for (int i = 0; i < sets.Length; i++)
        {
            // Check if the result is the best so far
            if (sets[i].BestResult.BestFitness < BestSet.BestResult.BestFitness)
            {
                BestSet = sets[i];
                BestStep = steps[i];
            }

            for (int j = 0; j < numIters; j++)
            {
                Avg2BestFitnessEachIteration[j] += sets[i].AverageBestFitnessEachIteration[j] / numSets;
            }

            // Check if the average result is the best average so far
            //if (sets[i].AverageBestFitness < BestSetOnAverage.AverageBestFitness)
            //{
            //    BestSetOnAverage = sets[i];
            //    BestStepOnAverage = steps[i];
            //}

            // Add points to graph
            //AverageBestFitnessEachStep[i] = new(steps[i], sets[i].AverageBestFitness);
        }
        Name = name;
    }


    public string Verbose()
    {
        return $"Best overall set:   \n==========\nStep: {BestStep}\nSet:\n{BestSet.Verbose()}    \n==========\n\n";
        /*+ $"Best set on average:\n==========\nStep: {BestStepOnAverage}\nSet:\n{BestSetOnAverage.Verbose()}\n==========\n*/
    }
}