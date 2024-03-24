//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading;
//using System;
//using UnityEngine;


using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;

public readonly struct Coordinates
{
    public float X { get; }
    public float Y { get; }


    public Coordinates(float x, float y)
    {
        X = x;
        Y = y;
    }
}


public enum EExperimentType
{
    AveragePerformance,
}


public class ExperimentRunner : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField m_numItersInp;
    private int m_numIters = 0;

    [SerializeField]
    private TMP_InputField m_numAlgsInp;
    private int m_numAlgs = 0;

    [SerializeField]
    private TMP_Text m_typeCycleTxt;
    private EExperimentType m_type = EExperimentType.AveragePerformance;

    [SerializeField]
    private TMP_Text m_itersPassedTxt; // A pseudo-progress bar
    private int m_itersPassed = 0; // A counter variable which is incremented through Interlocked.Increment.


    private void Start()
    {
        m_typeCycleTxt.text = $"Experiment Type: {m_type}";
    }


    //
    // UI Listener Methods
    //
    public void OnNumItersInputChanged()
    {
        m_numIters = int.Parse(m_numItersInp.text);
    }


    public void OnNumAlgsInputChanged()
    {
        m_numAlgs = int.Parse(m_numAlgsInp.text);
    }


    public void OnTypeCycleBtnPressed()
    {
        m_type = ArrayTools.CircularNextElement((EExperimentType[])Enum.GetValues(typeof(EExperimentType)), (int)m_type, true);
        m_typeCycleTxt.text = $"Experiment Type: {m_type}";
    }


    //
    // Backend Methods
    //
    public void Run()
    {
        if (m_numIters <= 0) Logger.Error("Number of iterations must be > 0");
        if (m_numAlgs <= 0) Logger.Error("Number of algorithms must be > 0");

        m_itersPassed = 0;
        StartCoroutine(UpdateIterationCount());

        switch (m_type)
        {
            case EExperimentType.AveragePerformance:
                AlgorithmSetResult result = RunAlgorithmSet(m_numAlgs, m_numIters);
                // Plot the best fitness achieved from the set
                PlotTools.PlotGraph(result.BestAlgorithm.BestFitnessEachIteration, m_numIters);
                break;
            default:
                Logger.Error("Experiment type was invalid.");
                break;
        }

        m_itersPassed = -1;
    }


    /// <summary>
    /// If the iterations passed variable is not -1, refreshes it on display frequently, until
    /// it is set to -1.
    /// </summary>
    private IEnumerator UpdateIterationCount()
    {
        while (m_itersPassed != -1)
        {
            // Displayed as an average if there are > 1 algorithms.
            m_itersPassedTxt.text = $"Avg. Iterations: {m_itersPassed / (float)m_numAlgs}/{m_numIters}";
            yield return new WaitForSeconds(1);
        }
        m_itersPassedTxt.text = "All iterations complete.";
        yield return null;
    }


    /// <summary>
    /// Handles the running of a single algorithm, outputting its stats and execution time.
    /// </summary>
    /// <returns>Algorithm stats.</returns>
    public AlgorithmResult RunAlgorithm(AlgorithmRunner runner, int numIters)
    {
        // Run the iterations
        Coordinates[] bestFitnessEachIter = new Coordinates[numIters];

        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < numIters; i++)
        {
            // Iteration 0 (before start) to Iteration {numIters - 1}
            if (runner.Alg.BestDayExists)
                bestFitnessEachIter[runner.Alg.IterNum] = new(runner.Alg.IterNum, runner.Alg.BestFitness);

            runner.RunIterations(1);
            Interlocked.Increment(ref m_itersPassed);
        }
        sw.Stop();


        // Return value
        AlgorithmResult result = new(runner.Alg, sw.ElapsedMilliseconds / 1000f, bestFitnessEachIter);
        return result;
    }


    /// <summary>
    /// [Threaded]
    /// Handles running of a set of algorithms, outputting their collective stats and execution time.
    /// Increasing the numAlgs improves the reliability of the results (low precision but high accuracy).
    /// </summary>
    /// <returns>Collective algorithm stats.</returns>
    public AlgorithmSetResult RunAlgorithmSet(int numAlgs, int numIters)
    {
        ManualResetEvent completionEvent = new(false);
        int threadsLeft = numAlgs;

        // Need to instantiate AlgorithmRunner on main thread
        AlgorithmRunner[] runners = new AlgorithmRunner[numAlgs];
        for (int i = 0; i < numAlgs; i++)
        {
            runners[i] = new();
            if (runners[i].Alg.DatasetError != "")
            {
                Logger.Error($"Dataset Error: {runners[i].Alg.DatasetError}");
            }
        }

        AlgorithmResult[] results = new AlgorithmResult[numAlgs];
        for (int i = 0; i < numAlgs; i++)
        {
            void Run(object state)
            {
                object[] args = state as object[];
                int index = (int)args[0];
                results[index] = RunAlgorithm(runners[index], numIters);
                if (Interlocked.Decrement(ref threadsLeft) == 0) completionEvent.Set();
            }

            ThreadPool.QueueUserWorkItem(Run!, new object[] { i });
        }
        completionEvent.WaitOne();

        return new(results);
    }


    /// <summary>
    /// [Serially runs Threaded]
    /// Handles running an algorithm set for each parameter value in a range.
    /// Increasing the numSteps improves the reliability of the experiment (accuracy).
    /// 
    /// Sets the preference to `min` and increases the preference by a step until `max` is reached.
    /// </summary>
    /// <param name="pref">A reference to the preference.</param>
    /// <param name="min">The minimum value assigned initially.</param>
    /// <param name="max">The maximum value to be tested.</param>
    /// </summary>
    /// <returns>Data from the experiment.</returns>
    public ExperimentResult RunFloatExperiment(int numAlgs, int numIters, ref float pref, float min, float max, float step, string name)
    {
        pref = min;
        int numSteps = 1 + (int)((max - min) / step);

        AlgorithmSetResult[] results = new AlgorithmSetResult[numSteps];
        float[] steps = new float[numSteps];

        for (int i = 0; i < numSteps; i++)
        {
            Logger.Log($"Running step {i}: {pref}.");

            // Run algorithm set then increment the preference
            results[i] = RunAlgorithmSet(numAlgs, numIters);
            steps[i] = pref;
            pref += step;
        }

        return new(results, steps, name);
    }


    public ExperimentResult RunIntExperiment(int numAlgs, int numIters, ref int pref, float min, float max, float step, string name)
    {
        float floatPref = pref;
        ExperimentResult result = RunFloatExperiment(numAlgs, numIters, ref floatPref, min, max, step, name);
        pref = (int)floatPref;

        return result;
    }


    public ExperimentResult RunBoolExperiment(int numAlgs, int numIters, ref bool pref, string name)
    {
        AlgorithmSetResult[] results = new AlgorithmSetResult[2];
        float[] steps = new float[2];

        pref = false;
        results[0] = RunAlgorithmSet(numAlgs, numIters);
        steps[0] = 0;

        pref = true;
        results[1] = RunAlgorithmSet(numAlgs, numIters);
        steps[1] = 1;

        return new(results, steps, name);
    }
}


public readonly struct AlgorithmResult : IVerbose
{
    public readonly Day BestDay { get; }
    public readonly float BestFitness { get; }
    public readonly float ExecutionSecs { get; }
    public readonly Coordinates[] BestFitnessEachIteration { get; }


    public AlgorithmResult(Algorithm alg, float executionSecs, Coordinates[] bestFitnessEachIteration)
    {
        BestDay = alg.BestDay;
        BestFitness = alg.BestFitness;
        ExecutionSecs = executionSecs;
        BestFitnessEachIteration = bestFitnessEachIteration;
    }


    public string Verbose()
    {
        return $"Best day:\n----------\nFitness: {BestFitness}\n{BestDay.Verbose()}\n----------\nExecution time: {ExecutionSecs}s";
    }
}


public readonly struct AlgorithmSetResult : IVerbose
{
    public readonly AlgorithmResult BestAlgorithm { get; }
    public readonly float AverageBestFitness { get; }

    public readonly float TotalExecutionSecs { get; }
    public readonly float AverageExecutionSecs { get; }


    public AlgorithmSetResult(AlgorithmResult[] set)
    {
        if (set.Length < 1)
            Logger.Error($"Could not construct AlgorithmSetResult from {set.Length} AlgorithmResults.");

        // Init sums to 0
        AverageBestFitness = 0;
        TotalExecutionSecs = 0;

        // Assign a default best
        BestAlgorithm = set[0];
        for (int i = 0; i < set.Length; i++)
        {
            // Check if the result is the best so far
            if (set[i].BestFitness < BestAlgorithm.BestFitness)
            {
                BestAlgorithm = set[i];
            }

            // Perform sum part of averages
            AverageBestFitness += set[i].BestFitness;
            TotalExecutionSecs += set[i].ExecutionSecs;
        }

        // Finish the average calculations
        AverageBestFitness /= set.Length;
        AverageExecutionSecs = TotalExecutionSecs / set.Length;
    }


    public string Verbose()
    {
        return $"Best algorithm:\n-=-=-=-=-=-\n{BestAlgorithm.Verbose()}\n=-=-=-=-=-\nAverage Best Fitness: {AverageBestFitness}\n"
                + $"Average Execution Time: {AverageExecutionSecs}s";
    }
}


public readonly struct ExperimentResult : IVerbose
{
    public readonly AlgorithmSetResult BestSetOverall { get; }
    public readonly AlgorithmSetResult BestSetOnAverage { get; }
    public readonly object BestStepOverall { get; }
    public readonly object BestStepOnAverage { get; }
    public readonly Coordinates[] AverageBestFitnessEachStep { get; }

    // For graph output
    public readonly Coordinates Min { get; }
    public readonly Coordinates Max { get; }
    public readonly string Name { get; }


    public ExperimentResult(AlgorithmSetResult[] sets, float[] steps, string name)
    {
        if (sets.Length < 1)
            Logger.Error($"Could not construct ExperimentResult from {sets.Length} AlgorithmSetResults.");

        // Init graph
        AverageBestFitnessEachStep = new Coordinates[sets.Length];

        // Set default best
        BestSetOverall = sets[0];
        BestStepOverall = steps[0];
        BestSetOnAverage = sets[0];
        BestStepOnAverage = steps[0];
        for (int i = 0; i < sets.Length; i++)
        {
            // Check if the result is the best so far
            if (sets[i].BestAlgorithm.BestFitness < BestSetOverall.BestAlgorithm.BestFitness)
            {
                BestSetOverall = sets[i];
                BestStepOverall = steps[i];
            }

            // Check if the average result is the best average so far
            if (sets[i].AverageBestFitness < BestSetOnAverage.AverageBestFitness)
            {
                BestSetOnAverage = sets[i];
                BestStepOnAverage = steps[i];
            }

            // Add points to graph
            AverageBestFitnessEachStep[i] = new(steps[i], sets[i].AverageBestFitness);
        }

        // Set graph params
        Min = new Coordinates(steps[0], 0);
        Max = new Coordinates(steps[^1], AverageBestFitnessEachStep.Max(f => f.Y));
        Name = name;
    }


    public string Verbose()
    {
        return $"Best overall set:   \n==========\nStep: {BestStepOverall}\nSet:\n{BestSetOverall.Verbose()}    \n==========\n\n"
                + $"Best set on average:\n==========\nStep: {BestStepOnAverage}\nSet:\n{BestSetOnAverage.Verbose()}\n==========\n";
    }
}