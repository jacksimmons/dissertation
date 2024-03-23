//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading;
//using System;
//using UnityEngine;


public struct Coordinates
{
    public float X { get; }
    public float Y { get; }


    public Coordinates(float x, float y)
    {
        X = x;
        Y = y;
    }
}


//public class Experiment
//{
//    private int m_numIters;
//    private int m_numAlgs;


//    public Experiment(int numIters, int numAlgs)
//    {
//        m_numIters = numIters;
//        m_numAlgs = numAlgs;

//        if (m_numIters <= 0) Logger.Error("Number of iterations must be > 0");
//        if (m_numAlgs <= 0) Logger.Error("Number of algorithms must be > 0");

//        Logger.Log($"Preferences:\n{Preferences.Instance.Verbose()}");
//    }


//    /// <summary>
//    /// Handles the running of a single algorithm, outputting its stats and execution time.
//    /// </summary>
//    /// <returns>Algorithm stats.</returns>
//    public AlgorithmResult RunAlgorithm(bool outputIterNum)
//    {
//        // Algorithm setup
//        AlgorithmRunner core = new();
//        if (core.Alg.DatasetError != "")
//        {
//            Logger.Log($"Dataset Error: {core.Alg.DatasetError}", Severity.Error);
//        }


//        // Run the iterations
//        Coordinates[] bestFitnessEachIter = new Coordinates[m_numIters];
//        int secondsPassed = 0;

//        Stopwatch sw = Stopwatch.StartNew();
//        do
//        {
//            core.RunIterations(1);

//            if (core.Alg.BestDayExists)
//                bestFitnessEachIter[core.Alg.IterNum] = new(core.Alg.IterNum, core.Alg.BestFitness);

//            // Output the current iteration number every second
//            if (sw.ElapsedMilliseconds > secondsPassed * 1000)
//            {
//                if (outputIterNum)
//                    Console.WriteLine($"Iterations: {core.Alg.IterNum}/{m_numIters}", Severity.Log);
//                secondsPassed++;
//            }
//        } while (core.Alg.IterNum < m_numIters - 1);
//        sw.Stop();


//        // Return value
//        AlgorithmResult result = new(core.Alg, sw.ElapsedMilliseconds / 1000f, bestFitnessEachIter);
//        return result;
//    }


//    /// <summary>
//    /// [Threaded]
//    /// Handles running of a set of algorithms, outputting their collective stats and execution time.
//    /// Increasing the numAlgs improves the reliability of the results (low precision but high accuracy).
//    /// </summary>
//    /// <returns>Collective algorithm stats.</returns>
//    public AlgorithmSetResult RunAlgorithmSet()
//    {
//        ManualResetEvent completionEvent = new(false);
//        int threadsLeft = m_numAlgs;

//        AlgorithmResult[] results = new AlgorithmResult[m_numAlgs];
//        for (int i = 0; i < m_numAlgs; i++)
//        {
//            void Run(object state)
//            {
//                object[] args = state as object[];
//                results[(int)args![0]] = RunAlgorithm(false);
//                if (Interlocked.Decrement(ref threadsLeft) == 0) completionEvent.Set();
//            }

//            ThreadPool.QueueUserWorkItem(Run!, new object[] { i });
//        }
//        completionEvent.WaitOne();

//        return new(results);
//    }


//    /// <summary>
//    /// [Serially runs Threaded]
//    /// Handles running an algorithm set for each parameter value in a range.
//    /// Increasing the numSteps improves the reliability of the experiment (accuracy).
//    /// 
//    /// Sets the preference to `min` and increases the preference by a step until `max` is reached.
//    /// </summary>
//    /// <param name="pref">A reference to the preference.</param>
//    /// <param name="min">The minimum value assigned initially.</param>
//    /// <param name="max">The maximum value to be tested.</param>
//    /// </summary>
//    /// <returns>Data from the experiment.</returns>
//    public ExperimentResult RunFloatExperiment(ref float pref, float min, float max, float step, string name)
//    {
//        pref = min;
//        int numSteps = 1 + (int)((max - min) / step);

//        AlgorithmSetResult[] results = new AlgorithmSetResult[numSteps];
//        double[] steps = new double[numSteps];

//        for (int i = 0; i < numSteps; i++)
//        {
//            Logger.Log($"Running step {i}: {pref}.");

//            // Run algorithm set then increment the preference
//            results[i] = RunAlgorithmSet();
//            steps[i] = pref;
//            pref += step;
//        }

//        return new(results, steps, name);
//    }


//    public ExperimentResult RunIntExperiment(ref int pref, float min, float max, float step, string name)
//    {
//        float floatPref = pref;
//        ExperimentResult result = RunFloatExperiment(ref floatPref, min, max, step, name);
//        pref = (int)floatPref;

//        return result;
//    }


//    public ExperimentResult RunBoolExperiment(ref bool pref, string name)
//    {
//        AlgorithmSetResult[] results = new AlgorithmSetResult[2];
//        double[] steps = new double[2];

//        pref = false;
//        results[0] = RunAlgorithmSet();
//        steps[0] = 0;

//        pref = true;
//        results[1] = RunAlgorithmSet();
//        steps[1] = 1;

//        return new(results, steps, name);
//    }


//    public static void PlotExperiment(ExperimentResult result)
//    {
//        Plot plot = InitPlot(result.AverageBestFitnessEachStep, result.Name, "Step", "Average Best Fitness");
//        plot.Axes.SetLimitsX(result.Min.X, result.Max.X);
//        plot.Axes.SetLimitsY(result.Min.Y, result.Max.Y);
//        plot.SavePng(Application.persistentDataPath + $"/Plots/{result.Name}_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png", 640, 480);
//    }


//    /// <summary>
//    /// Add the results to datapoints, to be output in a graph later.
//    /// </summary>
//    private static Plot InitPlot(Coordinates[] graph, string name, string labelX, string labelY)
//    {
//        // Create Plot object
//        Plot plot = new();

//        // Labels
//        plot.Title(name);
//        plot.XLabel(labelX);
//        plot.YLabel(labelY);

//        // Create scatter graph
//        var scatter = plot.Add.Scatter(graph);
//        scatter.MarkerStyle = MarkerStyle.None; // Remove dots, as they form a thick line when spaced closely

//        return plot;
//    }
//}


//public readonly struct AlgorithmResult : IVerbose
//{
//    public readonly Day BestDay { get; }
//    public readonly float BestFitness { get; }
//    public readonly float ExecutionSecs { get; }
//    public readonly Coordinates[] BestFitnessEachIteration { get; }


//    public AlgorithmResult(Algorithm alg, float executionSecs, Coordinates[] bestFitnessEachIteration)
//    {
//        BestDay = alg.BestDay;
//        BestFitness = alg.BestFitness;
//        ExecutionSecs = executionSecs;
//        BestFitnessEachIteration = bestFitnessEachIteration;
//    }


//    public string Verbose()
//    {
//        return $"Best day:\n----------\nFitness: {BestFitness}\n{BestDay.Verbose()}\n----------\nExecution time: {ExecutionSecs}s";
//    }
//}


//public readonly struct AlgorithmSetResult : IVerbose
//{
//    public readonly AlgorithmResult BestAlgorithm { get; }
//    public readonly float AverageBestFitness { get; }

//    public readonly float TotalExecutionSecs { get; }
//    public readonly float AverageExecutionSecs { get; }


//    public AlgorithmSetResult(AlgorithmResult[] set)
//    {
//        if (set.Length < 1)
//            Logger.Error($"Could not construct AlgorithmSetResult from {set.Length} AlgorithmResults.");

//        // Init sums to 0
//        AverageBestFitness = 0;
//        TotalExecutionSecs = 0;

//        // Assign a default best
//        BestAlgorithm = set[0];
//        for (int i = 0; i < set.Length; i++)
//        {
//            // Check if the result is the best so far
//            if (set[i].BestFitness < BestAlgorithm.BestFitness)
//            {
//                BestAlgorithm = set[i];
//            }

//            // Perform sum part of averages
//            AverageBestFitness += set[i].BestFitness;
//            TotalExecutionSecs += set[i].ExecutionSecs;
//        }

//        // Finish the average calculations
//        AverageBestFitness /= set.Length;
//        AverageExecutionSecs = TotalExecutionSecs / set.Length;
//    }


//    public string Verbose()
//    {
//        return $"Best algorithm:\n-=-=-=-=-=-\n{BestAlgorithm.Verbose()}\n=-=-=-=-=-\nAverage Best Fitness: {AverageBestFitness}\n"
//                + $"Average Execution Time: {AverageExecutionSecs}s";
//    }
//}


//public readonly struct ExperimentResult : IVerbose
//{
//    public readonly AlgorithmSetResult BestSetOverall { get; }
//    public readonly AlgorithmSetResult BestSetOnAverage { get; }
//    public readonly object BestStepOverall { get; }
//    public readonly object BestStepOnAverage { get; }
//    public readonly Coordinates[] AverageBestFitnessEachStep { get; }

//    // For graph output
//    public readonly Coordinates Min { get; }
//    public readonly Coordinates Max { get; }
//    public readonly string Name { get; }


//    public ExperimentResult(AlgorithmSetResult[] sets, double[] steps, string name)
//    {
//        if (sets.Length < 1)
//            Logger.Error($"Could not construct ExperimentResult from {sets.Length} AlgorithmSetResults.");

//        // Init graph
//        AverageBestFitnessEachStep = new Coordinates[sets.Length];

//        // Set default best
//        BestSetOverall = sets[0];
//        BestStepOverall = steps[0];
//        BestSetOnAverage = sets[0];
//        BestStepOnAverage = steps[0];
//        for (int i = 0; i < sets.Length; i++)
//        {
//            // Check if the result is the best so far
//            if (sets[i].BestAlgorithm.BestFitness < BestSetOverall.BestAlgorithm.BestFitness)
//            {
//                BestSetOverall = sets[i];
//                BestStepOverall = steps[i];
//            }

//            // Check if the average result is the best average so far
//            if (sets[i].AverageBestFitness < BestSetOnAverage.AverageBestFitness)
//            {
//                BestSetOnAverage = sets[i];
//                BestStepOnAverage = steps[i];
//            }

//            // Add points to graph
//            AverageBestFitnessEachStep[i] = new(steps[i], sets[i].AverageBestFitness);
//        }

//        // Set graph params
//        Min = new Coordinates(steps[0], 0);
//        Max = new Coordinates(steps[^1], AverageBestFitnessEachStep!.Max(f => f.Y));
//        Name = name;
//    }


//    public string Verbose()
//    {
//        return $"Best overall set:   \n==========\nStep: {BestStepOverall}\nSet:\n{BestSetOverall.Verbose()}    \n==========\n\n"
//                + $"Best set on average:\n==========\nStep: {BestStepOnAverage}\nSet:\n{BestSetOnAverage.Verbose()}\n==========\n";
//    }
//}