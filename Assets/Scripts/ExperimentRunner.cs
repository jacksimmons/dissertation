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


public enum EExperimentType
{
    AveragePerformance,
    PopSize,
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


    private void Start()
    {
        m_typeCycleTxt.text = $"Experiment Type: {m_type}";
    }


    //
    // UI Listener Methods
    //
    public void OnNumItersInputChanged()
    {
        if (m_numItersInp.text == "") return;
        m_numIters = int.Parse(m_numItersInp.text);
    }


    public void OnNumAlgsInputChanged()
    {
        if (m_numAlgsInp.text == "") return;
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
        if (m_numIters <= 0)
        {
            Logger.Warn("Number of iterations must be greater than zero.");
            return;
        }
        if (m_numAlgs <= 0)
        {
            Logger.Warn("Number of algorithms must be greater than zero.");
            return;
        }

        switch (m_type)
        {
            case EExperimentType.AveragePerformance:
                PlotTools.PlotLines(RunAlgorithmSet(m_numAlgs, m_numIters));
                break;
            case EExperimentType.PopSize:
                PlotTools.PlotExperiment(RunIntExperiment(m_numAlgs, m_numIters, ref Preferences.Instance.populationSize, 10, 100, 10, "PopSize"));
                break;
            default:
                Logger.Error("Experiment type was invalid.");
                break;
        }
    }


    /// <summary>
    /// Handles the running of a single algorithm, outputting its stats and execution time.
    /// </summary>
    /// <returns>Algorithm stats.</returns>
    public AlgorithmResult RunAlgorithm(AlgorithmRunner runner, int maxIter)
    {
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 1; i <= maxIter; i++)
        {
            runner.RunIterations(1);
        }
        sw.Stop();


        // Return value
        AlgorithmResult result = new(runner.Alg, sw.ElapsedMilliseconds / 1000f, runner.Plot.ToArray());
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

        return new(results, steps.Select(x => (object)x).ToArray(), name);
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

        return new(results, steps.Select(x => (object)x).ToArray(), name);
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
    public readonly AlgorithmResult[] Results { get; }
    public readonly int BestIndex { get; }
    public readonly AlgorithmResult BestResult => Results[BestIndex];

    public readonly Coordinates[] AverageBestFitnessEachIteration { get; }

    public readonly float TotalExecutionSecs { get; }
    public readonly float AverageExecutionSecs { get; }


    public AlgorithmSetResult(AlgorithmResult[] results)
    {
        if (results.Length < 1)
        {
            Logger.Warn($"Could not construct AlgorithmSetResult from {results.Length} AlgorithmResults.");
        }

        int numIters = results[0].BestFitnessEachIteration.Length;
        int numAlgs = results.Length;

        Results = results;
        AverageBestFitnessEachIteration = new Coordinates[numIters];

        TotalExecutionSecs = 0;
        AverageExecutionSecs = 0;

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
                AverageBestFitnessEachIteration[j].Y += results[i].BestFitnessEachIteration[j].Y / numAlgs;
            }

            // Perform sum part of averages
            TotalExecutionSecs += results[i].ExecutionSecs;
            AverageExecutionSecs += results[i].ExecutionSecs / numAlgs;
        }
    }


    public string Verbose()
    {
        return $"Best algorithm:\n-=-=-=-=-=-\n{BestResult.Verbose()}\n=-=-=-=-=-\n"
                + $"Average Execution Time: {AverageExecutionSecs}s";
    }
}


public readonly struct ExperimentResult : IVerbose
{
    public readonly AlgorithmSetResult[] Sets { get; }
    public readonly object[] Steps { get; }

    public readonly AlgorithmSetResult BestSet { get; }
    public readonly object BestStep { get; }

    // The average of the (average best fitness each iteration)s from each set.
    public readonly Coordinates[] Avg2BestFitnessEachIteration { get; }
    public readonly string Name { get; }


    public ExperimentResult(AlgorithmSetResult[] sets, object[] steps, string name)
    {
        if (sets.Length < 1)
            Logger.Error($"Could not construct ExperimentResult from {sets.Length} AlgorithmSetResults.");

        Sets = sets;
        Steps = steps;

        int numIters = sets[0].AverageBestFitnessEachIteration.Length;
        int numSets = sets.Length;

        Avg2BestFitnessEachIteration = new Coordinates[numIters];

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
                Avg2BestFitnessEachIteration[j].Y += sets[i].AverageBestFitnessEachIteration[j].Y / numSets;
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