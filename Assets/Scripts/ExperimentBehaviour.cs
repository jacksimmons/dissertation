// Commented 27/3
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// A Unity script which handles the setup and execution of experiments.
/// An experiment consists of a sequence of one or more steps, run serially.
/// During each step, a number of algorithms are run in parallel (and their
/// best fitnesses each iteration are averaged in the result).
/// 
/// The output fitness is always in the SummedFitness format, because ParetoDominance
/// fitnesses are >> 2-dimensional and thus cannot be represented well on a 2D plot.
public sealed class ExperimentBehaviour : SetupBehaviour
{
    /// <summary>
    /// The .dat file for the baseline experiments (one per algorithm per enum value in PlotTools.YAxis).
    /// </summary>
    [SerializeField]
    private TextAsset[] m_baselineDat;

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

    /// <summary>
    /// The cycle button which controls the metric used as the y-axis.
    /// </summary>
    [SerializeField]
    private Button m_experimentYAxisCycleBtn;
    [SerializeField]
    private TMP_Text m_experimentYAxisTxt;


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

        m_experimentYAxisTxt.text = Preferences.Instance.yAxis.ToString();
        m_experimentYAxisCycleBtn.onClick.AddListener(() => OnExperimentYAxisCycleBtnPressed());
    }


    /// <summary>
    /// Returns the corresponding baseline to the current preferences as a MeanLine.
    /// </summary>
    public MeanLine GetBaseline()
    {
        // For Baseline generation
        //return new();

        int algTypeIndex = Array.IndexOf(Preferences.ALG_TYPES, Preferences.Instance.algorithmType);
        string json = m_baselineDat[algTypeIndex * 2 + (int)Preferences.Instance.yAxis].text;

        // Extract json into a Baseline => Convert baseline into meanline => return.
        MeanLine ml = MeanLine.FromBaseline((Baseline)JsonUtility.FromJson(json, typeof(Baseline)));
        return ml;
    }


    //
    // UI Listener Methods
    //
    public void OnPreferenceCycleBtnPressed(bool right)
    {
        m_selectedPreferenceFieldIndex = ArrayTools.CircularNextIndex(m_selectedPreferenceFieldIndex, m_preferenceFields.Length, right);
        m_preferenceCycleTxt.text = $"{m_preferenceFields[m_selectedPreferenceFieldIndex].Name}";
    }


    public void OnExperimentYAxisCycleBtnPressed()
    {
        OnCycleEnumWithLabel(ref Preferences.Instance.yAxis, true, m_experimentYAxisTxt);

        // Manually save preferences, because by default this script doesn't save to disk (m_saveOnInputChange = false)
        Saving.SavePreferences();
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
        Stopwatch sw = Stopwatch.StartNew();

        // Ensure parameters are correct
        if (!CheckParams()) return;

        var setResult = RunAlgorithmSet(m_numAlgs, m_numIters);
        // First safe point we can display an exception; so display and return.
        if (setResult.Item1 != "")
        {
            Logger.Warn(setResult.Item1);
            return;
        }

        PlotTools.PlotLines(setResult.Item2, GetBaseline());

        sw.Stop();
        Logger.Log($"Execution time: {sw.ElapsedMilliseconds}ms");
    }


    /// <summary>
    /// Runs an experiment, consisting of a number of steps, each of which calculates the average
    /// best fitness for each iteration (average over all of its algorithms).
    /// Each step is plotted on a different line on the graph.
    /// </summary>
    public void Run()
    {
        Stopwatch sw = Stopwatch.StartNew();

        // Ensure parameters are correct
        if (!CheckParams()) return;

        FieldInfo field = m_preferenceFields[m_selectedPreferenceFieldIndex];

        // Call the experiment method based on the type of preference selected
        Tuple<string, ExperimentResult> result;
        if (field.FieldType == typeof(int) || field.FieldType == typeof(float))
        {
            if (!CheckNumericalParams()) return;

            result = RunNumericalExperiment(m_numAlgs, m_numIters, field, m_min, m_max, m_step, field.Name);
        }
        else
        {
            result = RunBoolExperiment(m_numAlgs, m_numIters, field, field.Name);
        }

        // First safe point to display the backpropagated exception (if there is one).
        if (result.Item1 != "")
        {
            Logger.Warn(result.Item1);
            return;
        }

        // Plot the graph
        PlotTools.PlotExperiment(result.Item2, field.Name, GetBaseline());

        // Reload original preferences
        Saving.LoadPreferences();

        sw.Stop();
        Logger.Log($"Execution time: {sw.ElapsedMilliseconds}ms");
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
        if (m_min > m_max)
        {
            Logger.Warn("Max must be greater than Min.");
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
    /// <returns>Backpropagated error (or empty string), as well as plottable single algorithm stats.</returns>
    public Tuple<string, AlgorithmResult> RunAlgorithm(AlgorithmRunner runner, int numIters)
    {
        var result = runner.RunIterations(numIters);

        // Handle any backpropagated exceptions, and avoid constructing an AlgorithmResult based on the erroneous result.
        if (result.Item1 != "")
        {
            return new(result.Item1, null);
        }

        AlgorithmResult alg = new(runner.BestDayEachIteration.ToArray());
        return new("", alg);
    }


    /// <summary>
    /// [Threaded]
    /// Handles running of a set of algorithms, outputting their collective stats.
    /// Increasing the numAlgs improves the reliability of the results (low precision but high accuracy).
    /// </summary>
    /// <returns>Backpropagated error (or empty string), and plottable stats for the set of algorithms.</returns>
    public Tuple<string, AlgorithmSetResult> RunAlgorithmSet(int numAlgs, int numIters)
    {
        // This is non-empty when an error has backpropagated to this function.
        string errorText = "";

        ManualResetEvent completionEvent = new(false);
        int threadsLeft = numAlgs;

        // Need to instantiate AlgorithmRunner on main thread
        AlgorithmRunner[] runners = new AlgorithmRunner[numAlgs];
        for (int i = 0; i < numAlgs; i++)
        {
            runners[i] = new();
        }

        AlgorithmResult[] results = new AlgorithmResult[numAlgs];
        for (int i = 0; i < numAlgs; i++)
        {
            void Run(object state)
            {
                object[] args = state as object[];
                int index = (int)args[0];
                
                var result = RunAlgorithm(runners[index], numIters);
                results[index] = result.Item2;

                if (result.Item1 != "")
                {
                    errorText = result.Item1;
                }

                if (Interlocked.Decrement(ref threadsLeft) == 0) completionEvent.Set();
            }

            ThreadPool.QueueUserWorkItem(Run!, new object[] { i });
        }
        completionEvent.WaitOne();

        // If there was an error, avoid constructing an AlgorithmSetResult based on these results.
        if (errorText != "")
        {
            return new(errorText, null);
        }
        return new(errorText, new(results));
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
    /// <returns>Backpropagated error (or empty string), and plottable stats from the experiment.</returns>
    private Tuple<string, ExperimentResult> RunNumericalExperiment(int numAlgs, int numIters, FieldInfo pref, float min, float max, float step, string name)
    {
        // This is non-empty when an error has backpropagated to this function.
        string errorText = "";

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
                var result = RunAlgorithmSet(numAlgs, numIters);
                if (result.Item1 != "") errorText = result.Item1;
                results[i] = result.Item2;

                steps[i] = intVal;
                pref.SetValue(Preferences.Instance, (int)Mathf.Min(intVal + Mathf.RoundToInt(step), max));
            }
            else
            {
                float floatVal = (float)pref.GetValue(Preferences.Instance);

                // Run algorithm set then increment the preference
                var result = RunAlgorithmSet(numAlgs, numIters);
                if (result.Item1 != "") errorText = result.Item1;
                results[i] = result.Item2;

                steps[i] = floatVal;
                pref.SetValue(Preferences.Instance, Mathf.Min(floatVal + step, max));
            }
        }

        // Handle backpropagated errors, and don't construct the ExperimentResult if there are any.
        if (errorText != "")
        {
            return new(errorText, null);
        }
        return new(errorText, new(results, steps, name));
    }


    /// <summary>
    /// [Serially runs Threaded]
    /// Handles running an algorithm set for boolean `true` and `false`.
    /// </summary>
    /// <param name="pref">A reference to the preference field type.</param>
    /// <returns>Backpropagated error (or empty string), and plottable stats from the experiment.</returns>
    public Tuple<string, ExperimentResult> RunBoolExperiment(int numAlgs, int numIters, FieldInfo pref, string name)
    {
        // This is non-empty when an error has backpropagated to this function.
        string errorText = "";

        AlgorithmSetResult[] results = new AlgorithmSetResult[2];
        object[] steps = new object[2];

        // Run a false experiment
        pref.SetValue(Preferences.Instance, false);
        var result = RunAlgorithmSet(numAlgs, numIters);

        if (result.Item1 != "")
        {
            errorText = result.Item1;
        }
        results[0] = result.Item2;
        steps[0] = false;

        // Run a true experiment
        pref.SetValue(Preferences.Instance, true);
        result = RunAlgorithmSet(numAlgs, numIters);

        if (result.Item1 != "")
        {
            errorText = result.Item1;
        } 
        results[1] = result.Item2;
        steps[1] = true;

        return new(errorText, new(results, steps, name));
    }
}