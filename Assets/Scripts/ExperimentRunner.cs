//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading;
//using System;
//using UnityEngine;


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;



public class ExperimentRunner : SetupBehaviour
{
    [SerializeField]
    private TMP_InputField m_numItersInp;
    private int m_numIters = 0;

    [SerializeField]
    private TMP_InputField m_numAlgsInp;
    private int m_numAlgs = 0;

    [SerializeField]
    private TMP_Text m_preferenceCycleTxt;
    private FieldInfo[] m_preferenceFields;
    private int m_selectedPreferenceFieldIndex = -1;

    [SerializeField]
    private TMP_InputField m_minInp;
    private float m_min = -1;

    [SerializeField]
    private TMP_InputField m_maxInp;
    private float m_max = -1;

    [SerializeField]
    private TMP_InputField m_stepInp;
    private float m_step = -1;

    private readonly Type[] SUPPORTED_TYPES =
    {
        typeof(int),
        typeof(float),
        typeof(bool),
    };


    private void Start()
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        List<FieldInfo> fields = Type.GetType("Preferences").GetFields(flags).ToList();


        // Only accept types that can be experimented on (no arrays, for example)
        List<FieldInfo> validFields = new();
        foreach (FieldInfo field in fields)
        {
            if (SUPPORTED_TYPES.Contains(field.FieldType))
            {
                validFields.Add(field);
            }
        }

        m_preferenceFields = validFields.ToArray();
        OnPreferenceCycleBtnPressed(true); // Go from -1 to first element

        m_minInp.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref m_min, value));
        m_maxInp.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref m_max, value));
        m_stepInp.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref m_step, value));
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


    public void OnPreferenceCycleBtnPressed(bool right)
    {
        m_selectedPreferenceFieldIndex = ArrayTools.CircularNextIndex(m_selectedPreferenceFieldIndex, m_preferenceFields.Length, right);
        m_preferenceCycleTxt.text = $"{m_preferenceFields[m_selectedPreferenceFieldIndex].Name}";
    }


    public void OnMinInputChanged(string value)
    {
    }


    public void OnMaxInputChanged(float max)
    {
        if (max < m_min)
        {
            Logger.Warn("Max cannot be less than Min!");
            return;
        }

        m_max = max;
    }


    //
    // Backend Methods
    //
    public void RunNoStep()
    {
        if (!CheckParams()) return;

        PlotTools.PlotLines(RunAlgorithmSet(m_numAlgs, m_numIters));
    }


    public void Run()
    {
        if (!CheckParams()) return;

        FieldInfo field = m_preferenceFields[m_selectedPreferenceFieldIndex];

        ExperimentResult result;
        if (field.FieldType == typeof(int) || field.FieldType == typeof(float))
        {
            if (m_step <= 0)
            {
                Logger.Warn("Step must be greater than zero.");
                return;
            }
            if (m_min < 0)
            {
                Logger.Warn("Min must be greater than zero.");
                return;
            }
            if (m_max < 0)
            {
                Logger.Warn("Max must be greater than zero.");
                return;
            }

            result = RunNumericalExperiment(m_numAlgs, m_numIters, field, 10, 100, 10, field.Name);
        }
        else
        {
            result = RunBoolExperiment(m_numAlgs, m_numIters, field, field.Name);
        }

        // (*) Preserve the value of the field after the experiment
        Saving.LoadPreferences();

        PlotTools.PlotExperiment(result, field.Name);
    }


    /// <summary>
    /// Returns whether the params were valid or not.
    /// </summary>
    private bool CheckParams()
    {
        if (m_numIters <= 0)
        {
            Logger.Warn("Number of iterations must be greater than zero.");
            return false;
        }
        if (m_numAlgs <= 0)
        {
            Logger.Warn("Number of algorithms must be greater than zero.");
            return false;
        }

        return true;
    }


    /// <summary>
    /// Handles the running of a single algorithm, outputting its stats and execution time.
    /// </summary>
    /// <returns>Algorithm stats.</returns>
    public AlgorithmResult RunAlgorithm(AlgorithmRunner runner, int maxIter)
    {
        for (int i = 1; i <= maxIter; i++)
        {
            runner.RunIterations(1);
        }


        // Return value
        AlgorithmResult result = new(runner.Alg, runner.Plot.ToArray());
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
    /// <param name="pref">A reference to the preference field type.</param>
    /// <param name="min">The minimum value assigned initially.</param>
    /// <param name="max">The maximum value to be tested.</param>
    /// </summary>
    /// <returns>Data from the experiment.</returns>
    private ExperimentResult RunNumericalExperiment(int numAlgs, int numIters, FieldInfo pref, float min, float max, float step, string name)
    {
        if (pref.FieldType == typeof(int))
        {
            pref.SetValue(Preferences.Instance, Mathf.RoundToInt(min));
        }
        else
        {
            pref.SetValue(Preferences.Instance, min);
        }
        int numSteps = 1 + (int)((max - min) / step);

        AlgorithmSetResult[] results = new AlgorithmSetResult[numSteps];
        object[] steps = new object[numSteps];

        for (int i = 0; i < numSteps; i++)
        {
            if (pref.FieldType == typeof(int))
            {
                int intVal = (int)pref.GetValue(Preferences.Instance);

                // Run algorithm set then increment the preference
                results[i] = RunAlgorithmSet(numAlgs, numIters);
                steps[i] = intVal;
                pref.SetValue(Preferences.Instance, intVal + Mathf.RoundToInt(step));
            }
            else
            {
                float floatVal = (float)pref.GetValue(Preferences.Instance);

                // Run algorithm set then increment the preference
                results[i] = RunAlgorithmSet(numAlgs, numIters);
                steps[i] = floatVal;
                pref.SetValue(Preferences.Instance, floatVal + step);
            }
        }

        return new(results, steps, name);
    }


    public ExperimentResult RunBoolExperiment(int numAlgs, int numIters, FieldInfo pref, string name)
    {
        AlgorithmSetResult[] results = new AlgorithmSetResult[2];
        object[] steps = new object[2];

        pref.SetValue(Preferences.Instance, false);
        results[0] = RunAlgorithmSet(numAlgs, numIters);
        steps[0] = false;

        pref.SetValue(Preferences.Instance, true);
        results[1] = RunAlgorithmSet(numAlgs, numIters);
        steps[1] = true;

        return new(results, steps, name);
    }
}


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