using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;



// Commented 27/3
/// <summary>
/// A Unity script which handles the setup and execution of experiments.
/// An experiment consists of a sequence of one or more steps, run serially.
/// During each step, a number of algorithms are run in parallel (and their
/// best fitnesses each iteration are averaged in the result).
/// </summary>
public class ExperimentRunner : SetupBehaviour
{
    /// <summary>
    /// The input field controlling the number of iterations for each algorithm
    /// to run.
    /// </summary>
    [SerializeField]
    private TMP_InputField m_numItersInp;
    private int m_numIters = 0;

    /// <summary>
    /// The input field controlling the number of algorithms to run in parallel
    /// for each step.
    /// </summary>
    [SerializeField]
    private TMP_InputField m_numAlgsInp;
    private int m_numAlgs = 0;

    /// <summary>
    /// The label displaying which preference the user is experimenting on.
    /// </summary>
    [SerializeField]
    private TMP_Text m_preferenceCycleTxt;
    /// <summary>
    /// All of the preferences the user can experiment on, obtained through reflection.
    /// </summary>
    private FieldInfo[] m_preferenceFields;
    private int m_selectedPreferenceFieldIndex = -1;

    /// <summary>
    /// The input field controlling the minimum (inclusive) of the experimental range.
    /// The preference is initialised to this value on the first step.
    /// </summary>
    [SerializeField]
    private TMP_InputField m_minInp;
    private float m_min = -1;

    /// <summary>
    /// The input field controlling the maximum (inclusive) of the experimental range.
    /// The preference is set to this value on the final step. The final step occurs
    /// when the preference has been incremented such that it is >= this value.
    /// </summary>
    [SerializeField]
    private TMP_InputField m_maxInp;
    private float m_max = -1;

    /// <summary>
    /// The input field controlling the step of the experiment. This value is added to
    /// the preference after each step until the maximum value is reached or surpassed.
    /// </summary>
    [SerializeField]
    private TMP_InputField m_stepInp;
    private float m_step = -1;

    private readonly static Type[] s_typesThatCanBeExperimentedOn =
    {
        typeof(int),
        typeof(float),
        typeof(bool),
    };
    private const int MAX_LINES_ON_GRAPH = 15;


    private void Start()
    {
        // Don't want preferences to get saved in this menu.
        m_saveOnInputChange = false;

        // Get all of the public, non-static fields of the Preferences class.
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        List<FieldInfo> fields = Type.GetType("Preferences").GetFields(flags).ToList();


        // Only accept fields which have a type that can be experimented on (no arrays, for example).
        List<FieldInfo> validFields = new();
        foreach (FieldInfo field in fields)
        {
            if (s_typesThatCanBeExperimentedOn.Contains(field.FieldType))
            {
                validFields.Add(field);
            }
        }


        // Assign accepted preferences fields
        m_preferenceFields = validFields.ToArray();

        OnPreferenceCycleBtnPressed(true); // Go from -1 to first preference field

        // Add listeners to input fields
        m_numItersInp.onEndEdit.AddListener((string value) => OnIntInputChanged(ref m_numIters, value));
        m_numAlgsInp.onEndEdit.AddListener((string value) => OnIntInputChanged(ref m_numAlgs, value));
        m_minInp.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref m_min, value));
        m_maxInp.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref m_max, value));
        m_stepInp.onEndEdit.AddListener((string value) => OnFloatInputChanged(ref m_step, value));
    }


    //
    // UI Listener Methods
    //
    public void OnPreferenceCycleBtnPressed(bool right)
    {
        m_selectedPreferenceFieldIndex = ArrayTools.CircularNextIndex(m_selectedPreferenceFieldIndex, m_preferenceFields.Length, right);
        m_preferenceCycleTxt.text = $"{m_preferenceFields[m_selectedPreferenceFieldIndex].Name}";
    }


    //
    // Backend Methods
    //
    /// <summary>
    /// Runs a number of algorithms in parallel, graphs the individual best fitness each iteration
    /// as well as the average (of all algorithms) best fitness each iteration.
    /// </summary>
    public void RunNoStep()
    {
        if (!CheckParams()) return;

        PlotTools.PlotLines(RunAlgorithmSet(m_numAlgs, m_numIters));
    }


    /// <summary>
    /// Runs an experiment, consisting of a number of steps, each of which calculates the average
    /// best fitness for each iteration (average over all of its algorithms).
    /// Each step is plotted on a different line on the graph.
    /// </summary>
    public void Run()
    {
        if (!CheckParams()) return;

        FieldInfo field = m_preferenceFields[m_selectedPreferenceFieldIndex];

        // Call the experiment method based on the type of preference selected
        ExperimentResult result;
        if (field.FieldType == typeof(int) || field.FieldType == typeof(float))
        {
            if (!CheckNumericalParams()) return;

            result = RunNumericalExperiment(m_numAlgs, m_numIters, field, m_min, m_max, m_step, field.Name);
        }
        else
        {
            result = RunBoolExperiment(m_numAlgs, m_numIters, field, field.Name);
        }

        // Reset the value of the preference, so that the experiment doesn't alter regular algorithm
        // performance.
        Saving.LoadPreferences();

        PlotTools.PlotExperiment(result, field.Name);
    }


    /// <summary>
    /// Returns whether input parameters that apply to all experiments were valid or not.
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
    /// Returns whether input parameters that apply to only numerical experiments were valid or not.
    /// </summary>
    private bool CheckNumericalParams()
    {
        if (m_step <= 0)
        {
            Logger.Warn("Step must be greater than zero.");
            return false;
        }
        if (m_min < 0)
        {
            Logger.Warn("Min must be greater than zero.");
            return false;
        }
        if (m_max < 0)
        {
            Logger.Warn("Max must be greater than zero.");
            return false;
        }

        int numSteps = 1 + (int)((m_max - m_min) / m_step);
        if (numSteps > MAX_LINES_ON_GRAPH)
        {
            Logger.Warn($"Needs a larger step increment. The step provided would lead to > 15 different lines on the same graph.");
            return false;
        }

        return true;
    }


    /// <summary>
    /// Handles the running of a single algorithm, outputting its stats.
    /// </summary>
    /// <returns>Plottable single algorithm stats.</returns>
    public AlgorithmResult RunAlgorithm(AlgorithmRunner runner, int numIters)
    {
        runner.RunIterations(numIters);


        // Return value
        AlgorithmResult result = new(runner.Alg, runner.Plot.ToArray());
        return result;
    }


    /// <summary>
    /// [Threaded]
    /// Handles running of a set of algorithms, outputting their collective stats.
    /// Increasing the numAlgs improves the reliability of the results (low precision but high accuracy).
    /// </summary>
    /// <returns>Plottable stats for the set of algorithms.</returns>
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
    /// <returns>Plottable stats from the experiment.</returns>
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
        int numSteps = 1 + Mathf.CeilToInt((max - min) / step);

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
                pref.SetValue(Preferences.Instance, (int)Mathf.Min(intVal + Mathf.RoundToInt(step), max));
            }
            else
            {
                float floatVal = (float)pref.GetValue(Preferences.Instance);

                // Run algorithm set then increment the preference
                results[i] = RunAlgorithmSet(numAlgs, numIters);
                steps[i] = floatVal;
                pref.SetValue(Preferences.Instance, Mathf.Min(floatVal + step, max));
            }
        }

        return new(results, steps, name);
    }


    /// <summary>
    /// [Serially runs Threaded]
    /// Handles running an algorithm set for boolean `true` and `false`.
    /// </summary>
    /// <param name="pref">A reference to the preference field type.</param>
    /// <returns>Plottable stats from the experiment.</returns>
    public ExperimentResult RunBoolExperiment(int numAlgs, int numIters, FieldInfo pref, string name)
    {
        AlgorithmSetResult[] results = new AlgorithmSetResult[2];
        object[] steps = new object[2];

        // Run a false experiment
        pref.SetValue(Preferences.Instance, false);
        results[0] = RunAlgorithmSet(numAlgs, numIters);
        steps[0] = false;

        // Run a true experiment
        pref.SetValue(Preferences.Instance, true);
        results[1] = RunAlgorithmSet(numAlgs, numIters);
        steps[1] = true;

        return new(results, steps, name);
    }
}