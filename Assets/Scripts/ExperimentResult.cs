// Commented 18/4
/// <summary>
/// The stats of a single algorithm.
/// </summary>
public sealed class AlgorithmResult : IVerbose
{
    /// <summary>
    /// For each index (iteration), contains a Day which had the lowest fitness ever seen up
    /// to that iteration.
    /// </summary>
    public Day[] BestDayEachIteration { get; }

    public Day BestDay => BestDayEachIteration[^1];
    public float BestFitness => BestDay.TotalFitness.Value;


    public AlgorithmResult(Day[] bestDayEachIteration)
    {
        BestDayEachIteration = bestDayEachIteration;
    }


    public string Verbose()
    {
        return $"Best day:\n----------\nFitness: {BestFitness}\n{BestDay.Verbose()}\n";
    }
}


/// <summary>
/// The stats of a set of algorithms.
/// </summary>
public sealed class AlgorithmSetResult : IVerbose
{
    /// <summary>
    /// An array of AlgorithmResults, one for each algorithm that ran in parallel.
    /// </summary>
    public AlgorithmResult[] Results { get; }

    /// <summary>
    /// The index of the AlgorithmResult that found the lowest fitness Day.
    /// </summary>
    private readonly int m_bestIndex;
    public AlgorithmResult BestResult
    {
        get
        {
            if (Results.Length > 0)
            {
                return Results[m_bestIndex];
            }
            return null;
        }
    }


    public AlgorithmSetResult(AlgorithmResult[] results)
    {
        Results = results;

        // Calculate best result
        for (int i = 0; i < results.Length; i++)
        {
            // Check if the result is the best so far
            if (results[i].BestFitness < results[m_bestIndex].BestFitness)
            {
                m_bestIndex = i;
            }
        }
    }


    public string Verbose()
    {
        if (BestResult != null)
        {
            return $"Best algorithm:\n-=-=-=-=-=-\n{BestResult.Verbose()}\n=-=-=-=-=-\n";
        }
        return "Null AlgorithmSetResult.";
    }
}


/// <summary>
/// The stats of a full experiment, which consists of a set of steps, each of which has its own
/// algorithm set result.
/// </summary>
public sealed class ExperimentResult : IVerbose
{
    /// <summary>
    /// An array of AlgorithmSetResults, one for each step that ran. Each AlgorithmSetResult has
    /// an array of AlgorithmResults, one for each algorithm that ran in parallel.
    /// </summary>
    public AlgorithmSetResult[] Sets { get; }

    /// <summary>
    /// The values used at each step. One-to-one mapping with the above sets.
    /// </summary>
    public object[] Steps { get; }

    /// <summary>
    /// The set with the lowest ALF (ALF = average over (lowest fitnesses of all algorithms))
    /// </summary>
    public AlgorithmSetResult BestSet { get; }

    /// <summary>
    /// The best value in the Steps array, for some a, Sets[a] = BestSet => Steps[a] = BestStep.
    /// </summary>
    public object BestStep { get; }

    /// <summary>
    /// The name used as part of the filename. Briefly outlines the experiment done.
    /// </summary>
    public string Name { get; }


    public ExperimentResult(AlgorithmSetResult[] sets, object[] steps, string name)
    {
        if (sets.Length < 1)
            Logger.Warn($"Could not construct ExperimentResult from {sets.Length} AlgorithmSetResults.");

        Sets = sets;
        Steps = steps;
        Name = name;

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
        }
    }


    public string Verbose()
    {
        return $"Best overall set:   \n==========\nStep: {BestStep}\nSet:\n{BestSet.Verbose()}\n==========\n\n";
    }
}