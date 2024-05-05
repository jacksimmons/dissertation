// Commented 18/4
// A collection of tools (extension classes).
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = System.Random;


public interface IVerbose
{
    /// <summary>
    /// Gets a verbose description of this object as a string.
    /// </summary>
    public string Verbose();
}



public enum ELogSeverity
{
    Log,
    Warning
}


/// <summary>
/// An extension class for logging methods.
/// </summary>
public static class Logger
{
    private static MenuStackHandler Menu
    {
        get
        {
            GameObject menuHandler = GameObject.FindWithTag("MenuStackHandler");
            if (menuHandler)
            {
                return menuHandler.GetComponent<MenuStackHandler>();
            }
            throw new WarnException();
        }
    }


    /// <summary>
    /// Handles logging to console, warning user and throwing errors in critical situations.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="severity">Severity of the log, affecting the output method
    /// or message. Default is Log.</param>
    public static void Log(object message, ELogSeverity severity = ELogSeverity.Log)
    {
        switch (severity)
        {
            case ELogSeverity.Log:
                Debug.Log(message);
                break;
            case ELogSeverity.Warning:
                Menu.ShowPopup("Warning", $"{message}", Color.red);
                break;
        }
    }


    public static void Warn(object message) => Log(message, ELogSeverity.Warning);
}


/// <summary>
/// An extension class for arrays.
/// </summary>
public static class ArrayTools
{
    /// <summary>
    /// A greedy insert which moves all elements after, one place closer to the end.
    /// Deletes the final element entirely.
    /// </summary>
    public static void ArrayInsert<T>(T[] array, T toAdd, int index)
    {
        // Insert the element
        T next = array[index];
        array[index] = toAdd;

        // Move all elements (up to the second last) toward the end, by one place
        for (int i = index + 1; i < array.Length - 1; i++)
        {
            T was = array[i];
            array[i] = next;
            next = was;
        }

        // Overwrite the last element with the second last element
        array[^1] = next;
    }


	/// <summary>
	/// Gets and returns the next element of an arbitrary array in a circular array fashion.
	/// </summary>
    public static T CircularNextElement<T>(T[] arr, int current, bool right)
    {
        return arr[CircularNextIndex(current, arr.Length, right)];
    }


    /// <summary>
    /// Gets the next index of an arbitrary array in a circular array fashion.
    /// If the provided index is the last element in the array, it returns 0 (if going right).
    /// Likewise, if provided index is 0, it returns {length - 1} (if going left).
    /// </summary>
    /// <param name="current">The current index to go left/right from.</param>
    /// <param name="length">The length of the array in question.</param>
    /// <param name="right">If `true`, goes right (clockwise) around the array, otherwise goes left (anti-cw).</param>
    /// <returns>The next index.</returns>
    public static int CircularNextIndex(int current, int length, bool right)
    {
        if (right)
        {
            // Going right
            if (current + 1 >= length) return 0;
            return current + 1;
        }

        // Going left
        if (current - 1 < 0) return length - 1;
        return current - 1;
    }
}


/// <summary>
/// An extension class for Unity UI operations.
/// </summary>
public static class UITools
{
    /// <summary>
    /// When the left/right button is pressed in the preferences menu (to navigate
    /// between preference categories).
    /// Disables the previous panel, and enables the new one.
    /// </summary>
    /// <param name="right">`true` if the right button was pressed, `false` if the
    /// left button was pressed.</param>
    /// <param name="panels">An array of all the panels to operate on.</param>
    /// <param name="activePanelIndex">The current panel that is active. Is updated
    /// accordingly.</param>
    /// <returns>The index of the new panel activated.</returns>
    public static void OnNavBtnPressed(bool right, UnityEngine.GameObject[] panels, ref int activePanelIndex)
    {
        panels[activePanelIndex].SetActive(false);

        activePanelIndex =
            ArrayTools.CircularNextIndex(activePanelIndex, panels.Length, right);

        panels[activePanelIndex].SetActive(true);
    }


    /// <summary>
    /// Safely destroys all child objects of a transform (without modified collection errors).
    /// </summary>
    public static void DestroyAllChildren(Transform transform)
    {
        // Getting all children into a new array
        Transform[] children = new Transform[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            children[i] = transform.GetChild(i);
        }

        // Destroying all children in the new array
        for (int j = 0; j < children.Length; j++)
        {
            UnityEngine.Object.Destroy(children[j].gameObject);
        }
    }
}


/// <summary>
/// An extension class for math operations required in this project.
/// </summary>
public static class MathTools
{
    // Atwater general factor theorem (Kent, 2006).
    public const int CALORIES_PER_GRAM_PROTEIN = 4;
    public const int CALORIES_PER_GRAM_FAT = 9;
    public const int CALORIES_PER_GRAM_CARBS = 4;


    /// <summary>
    /// Precision of approximation-based functions in this class.
    /// </summary>
    public const float EPSILON = 1e-3f;


    /// <summary>
    /// Returns true if the absolute difference between two floats is smaller than
    /// the defined precision. Returns false if not.
    /// </summary>
    public static bool Approx(float x, float y)
    {
        if (MathF.Abs(x - y) < EPSILON) return true;
        return false;
    }


    /// <summary>
    /// Calculates whether x is strictly less than y, ensuring that x and y are not
    /// even approximately equal.
    /// </summary>
    public static bool ApproxLessThan(float x, float y)
    {
        return x < y && !Approx(x, y);
    }


    /// <summary>
    /// if `value` < `min` => `min`
    /// if `value` > `max` => `max`
    /// else => `value`
    /// </summary>
    public static float MinMax(float min, float value, float max)
    {
        return MathF.Min(MathF.Max(min, value), max);
    }


    /// <summary>
	/// Calculates a random probability (P).
	/// Then sums over all elements in a probability array (which must sum to 1), and returns
	/// the index to cause the sum to become greater than P.
    /// </summary>
    public static int GetFirstSurpassedProbability(float[] probabilities, Random rand)
    {
        int length = probabilities.Length;
        if (length < 1)
            Logger.Warn("Invalid probability array had a length of < 1");

        // Clamp the probability to the range [EPSILON, 1 - EPSILON]
        float probability = MinMax(EPSILON, (float)rand.NextDouble(), 1-EPSILON);

        // Calculate which vertex was selected, through
        // a sum-of-probabilities check.
        float sum = 0;
        int index = -1;
        for (int i = 0; i < length; i++)
        {
            sum += probabilities[i];
            if (sum > probability)
            {
                index = i;
                break;
            }
        }
        return index;
    }


    /// <summary>
    /// Sets calories to the sum of the caloric values of provided macros.
    /// 1g of protein, fat or carbohydrate has 4, 9 or 4 kcal caloric value respectively.
    /// </summary>
    /// <param name="calories">The calories to update.</param>
    public static void MacrosToCalories(ref float calories, float proteinG, float fatG, float carbsG)
    {
        calories = proteinG * CALORIES_PER_GRAM_PROTEIN
                 + fatG * CALORIES_PER_GRAM_FAT
                 + carbsG * CALORIES_PER_GRAM_CARBS;
    }


    /// <summary>
    /// Multiplies macronutrients by a ratio to ensure the calories due to macros equals the calories due to energy.
    /// If protein, fat and carbs are uninitialised, the calories are split evenly between all 3.
    /// </summary>
    /// <param name="calories">The calories due to energy.</param>
    public static void CaloriesToMacros(float calories, ref float proteinG, ref float fatG, ref float carbsG)
    {
        // Calories calculated from the macros (not necessarily correct, goal is to correct it). (Kent, 2006).
        float macroCalories = proteinG * CALORIES_PER_GRAM_PROTEIN
                            + fatG * CALORIES_PER_GRAM_FAT
                            + carbsG * CALORIES_PER_GRAM_CARBS;

        // Multiply macros so they satisfy the ratio.
        if (!Approx(macroCalories, 0))
        {
            float multiplier = calories / macroCalories;
            proteinG *= multiplier;
            fatG *= multiplier;
            carbsG *= multiplier;
        }
        // Only occurs when macronutrients are uninitialised, e.g. if no preferences have yet been defined
        else
        {
            float caloriesEach = calories / 3;
            proteinG = caloriesEach / CALORIES_PER_GRAM_PROTEIN;
            fatG = caloriesEach / CALORIES_PER_GRAM_FAT;
            carbsG = caloriesEach / CALORIES_PER_GRAM_CARBS;
        }
    }
}


/// <summary>
/// An extension class for plotting graphs using gnuplot.
/// </summary>
public static class PlotTools
{
    /// <summary>
    /// Plots a single line on a graph. It will be displayed as the line, and then an average (which will
    /// be identical to it).
    /// </summary>
    /// <param name="alg">The output of an algorithm.</param>
    public static void PlotLine(AlgorithmResult alg, MeanLine baseline)
    {
        PlotLines(new(new AlgorithmResult[] { alg }), baseline);
    }


    /// <summary>
    /// Plots multiple lines as a single mean with error bars (standard deviation).
    /// </summary>
    /// <param name="algs">The output of several algorithms ran in parallel, with identical
    /// configurations.</param>
    public static void PlotLines(AlgorithmSetResult algs, MeanLine baseline)
    {
        int numIters = algs.Results[0].BestDayEachIteration.Length;
        int numAlgs = algs.Results.Length;

        // First index gives the AlgorithmResult, Second index gives the Iteration number.
        // Value at [i][j] is the best day for the ith result, at the jth iteration.
        Day[][] allAlgsBestDayEachIter = algs.Results.Select(r => r.BestDayEachIteration).ToArray();

        // Create a new graph, with an average line created from a 1-step experiment.
        float[][] lines = allAlgsBestDayEachIter.Select(r => r.Select(d => DayToYCoordinate(d)).ToArray()).ToArray();
        MeanLine ml = new(Get2DArrAverage(allAlgsBestDayEachIter), lines);

        // Create graph with just the mean line
        Graph graph = new(1);
        graph.MeanLines[0] = ml;

        graph.MeanLines[0].LineLabel = $"Mean for configuration " + graph.MeanLines[0].LineLabel;

        PlotGraph(graph, baseline);
    }


    /// <summary>
    /// This algorithm reduces a 2D array into an average over the two indices. For each first index, calculates an average
    /// over all of the second indices for that index. Reduces the array to 1D.
    /// </summary>
    private static float[] Get2DArrAverage(Day[][] arr2D)
    {
        int numAlgs = arr2D.Length;
        int numIters = arr2D[0].Length;
        float[] average = new float[numIters];

        for (int i = 0; i < numAlgs; i++)
        {
            for (int j = 0; j < numIters; j++)
            {
                average[j] += DayToYCoordinate(arr2D[i][j]);
            }
        }
        return average.Select(x => x / numAlgs).ToArray();
    }


    /// <summary>
    /// Depending on the yAxis preference, returns the corresponding y-value of the
    /// provided day.
    /// </summary>
    /// <param name="day">The day to convert into a y-coordinate.</param>
    /// <returns></returns>
    private static float DayToYCoordinate(Day day)
    {
        float fitness = day.TotalFitness.Value;

        // If using ParetoDominance, convert it to SummedFitness for graphical representation.
        if (Preferences.Instance.fitnessApproach == EFitnessApproach.ParetoDominance)
        {
            fitness = new Day.SummedFitness(day).Value;
        }

        return Preferences.Instance.yAxis switch
        {
            Graph.YAxis.BestDayMass => day.Mass,
            Graph.YAxis.BestDayFitness or _ => fitness,
        };
    }


    /// <summary>
    /// Plots a graph where each line corresponds to the average best fitness each iteration for a given AlgorithmSetResult.
    /// It also plots an "average" line which is the average of the averages.
    /// </summary>
    /// <param name="exp">The result to plot.</param>
    public static void PlotExperiment(ExperimentResult exp, string preference, MeanLine baseline)
    {
        int numIters = exp.Sets[0].Results[0].BestDayEachIteration.Length;
        int numSteps = exp.Sets.Length;

        // Convert ExperimentResult into a 3D array.
        Day[][][] allStepsAllAlgsBestDayEachIter = exp.Sets.Select(e => e.Results.Select(r => r.BestDayEachIteration).ToArray()).ToArray();

        // Reduce the above arr 3D -> 2D, by first taking the average for each result (over all its iterations)
        // This merges the 2nd and 3rd dimensions to be one dimension, of same length as the 2nd dimension was.
        float[][] allStepsAvgBestDayEachIter = allStepsAllAlgsBestDayEachIter.Select(x => Get2DArrAverage(x)).ToArray();

        // Further reduce the above arr to a graph
        Graph graph = new(exp.Steps.Length);
        for (int i = 0; i < exp.Steps.Length; i++)
        {
            float[][] lines = allStepsAllAlgsBestDayEachIter[i].Select(x => x.Select(y => DayToYCoordinate(y)).ToArray()).ToArray();

            // Sum iteration i for all results
            MeanLine line = new(allStepsAvgBestDayEachIter[i], lines);
            graph.MeanLines[i] = line;
        }

        for (int i = 0; i < graph.NumLines; i++)
        {
            graph.MeanLines[i].LineLabel = $"Mean for value {exp.Steps[i]} " + graph.MeanLines[i].LineLabel;
        }

        PlotGraph(graph, baseline, preference);
    }


    /// <summary>
    /// Handles all method calls necessary to plot a graph to file.
    /// </summary>
    private static void PlotGraph(Graph graph, MeanLine baseline, string title = "")
    {
        string dataFilePath = Application.persistentDataPath + "/plot.dat";
        string gnuplotFilePath = Application.persistentDataPath + "/plot.gnuplot";
        string graphFilePath = $"{Application.persistentDataPath}/Plots/{title}{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png";

        // For Baseline generation
        //Saving.SaveToFile<Baseline>(new(graph.MeanLines[0].Means, graph.MeanLines[0].StandardDeviations), "baseline.json");
        //return;

        ConstructDataFile(graph, baseline, dataFilePath);
        ConstructGnuplotFile(graph, baseline, graphFilePath, gnuplotFilePath, dataFilePath, title);
        RunGnuplot(gnuplotFilePath);
    }


    /// <summary>
    /// Constructs the .dat file which the gnuplot file will extract a png graph from.
    /// </summary>
    private static void ConstructDataFile(Graph graph, MeanLine baseline, string dataFilePath)
    {
        // Create data file
        if (!File.Exists(dataFilePath))
            File.Create(dataFilePath).Close();

        // Only add best fitness changes to the file (and a point for the last iteration)
        string dataFileContent = "";

        // Iterate over iteration
        // ith iteration for all lines
        for (int i = 0; i < graph.NumIters; i++)
        {
            // Only plot every graph.NumIters / 10 or when iteration is 1 => 11 points
            if (!((i + 1) % (graph.NumIters / 10) == 0 || i + 1 == 1)) continue;

            // The next line of the .dat file.
            string dataFileLine = $"{i + 1}";

            for (int j = 0; j < graph.NumLines; j++)
            {
                MeanLine ml = graph.MeanLines[j];
                dataFileLine += float.IsPositiveInfinity(ml.Means[i]) ? " inf" : $" {ml.Means[i]}";
                dataFileLine += float.IsPositiveInfinity(ml.StandardDeviations[i]) ? " inf" : $" {ml.StandardDeviations[i]}";
            }

            // Add the baseline if an experiment
            if (i <= Baseline.ITERS)
            {
                dataFileLine += float.IsPositiveInfinity(baseline.Means[i]) ? " inf" : $" {baseline.Means[i]}";
                dataFileLine += float.IsPositiveInfinity(baseline.StandardDeviations[i]) ? " inf" : $" {baseline.StandardDeviations[i]}";
            }

            dataFileLine += '\n';
            dataFileContent += dataFileLine;
        }

        // Write .dat data file
        try
        {
            File.WriteAllText(dataFilePath, dataFileContent);
        }
        catch
        {
            Logger.Warn($"ConstructDataFile: Unable to write to file {dataFilePath}. Please ensure the program has" +
                " read/write permissions here and that the file isn't already open.");
        }
    }


    /// <summary>
    /// Constructs the gnuplot script file which will create the graph using gnuplot, and output it as png.
    /// </summary>
    private static void ConstructGnuplotFile(Graph graph, MeanLine baseline, string graphPath, string gnuplotScriptPath, string dataPath, string title)
    {
        // Create gnuplot script file
        if (!File.Exists(gnuplotScriptPath))
            File.Create(gnuplotScriptPath).Close();

        // Correct datapath string for gnuplot syntax
        dataPath = dataPath.Replace("\\", "/");

        // Iterate over every line in every iteration, to find the maximum y-value of the graph
        //float maxY = 0;
        //for (int i = 0; i < graph.NumIters; i++)
        //{
        //    for (int j = 0; j < graph.NumLines; j++)
        //    {
        //        float y = graph.MeanLines[j].Means[i];
        //        if (y > maxY)
        //        {
        //            maxY = y;
        //        }
        //    }
        //}

            // The contents of the file, which were inferred from gnuplot's documentation.
        string gnuplotFile
        // Set output type and location
        = "set terminal png size 640,480\n"
        + $"set output \"{graphPath.Replace("\\", "/")}\"\n"

        // Set axis ranges
        + $"set xrange [0: {graph.NumIters}]\n"
        // Graph becomes very cramped when considering a wide range of y values
        + $"set yrange [100: 10000]\n"

        // Set titles
        + $"set title \"Graph of {Preferences.Instance.yAxis} against Iteration Number\"\n"
        + "set xlabel \"Iteration\"\n"
        + $"set ylabel \"{Preferences.Instance.yAxis}\"\n"

        // Set nice colours for lines
        + "set style line 1 lc \"#ff0000\"\n"
        + "set style line 2 lc \"#ff8700\"\n"
        + "set style line 3 lc \"#ffd300\"\n"
        + "set style line 4 lc \"#deff0a\"\n"
        + "set style line 5 lc \"#a1ff0a\"\n"
        + "set style line 6 lc \"#0aff99\"\n"
        + "set style line 7 lc \"#0aefff\"\n"
        + "set style line 8 lc \"#147df5\"\n"
        + "set style line 9 lc \"#580aff\"\n"
        + "set style line 10 lc \"#be0aff\"\n"
        + "set style line 11 lc \"#cccccc\"\n"
        + "set style line 12 lc \"#999999\"\n"
        + "set style line 13 lc \"#666666\"\n"
        + "set style line 14 lc \"#333333\"\n"
        + "set style line 15 lc \"#964B00\"\n"
        + "set style line 16 lc \"#000000\"\n"
        + "plot ";

        // Add line plots
        for (int l = 0; l < graph.NumLines; l++)
        {
            // If less than or equal to 2 lines, add error bars. Otherwise add a line without error bars.
            string errorBars;
            if (graph.NumLines <= 2)
                errorBars = $":{3 + l * 2} with errorlines";
            else
                errorBars = $"with lines";

            // Regardless of if error bars were added, still uses the format 1:(2 + l*2):(3 + l*2), just excludes the third index if no error bars.s
            gnuplotFile += $"\"{dataPath}\" using 1:{2 + l * 2}{errorBars} ls {l + 1} title \"{graph.MeanLines[l].LineLabel}\",\\\n";
        }

        // Add the baseline to the graph
        gnuplotFile += $"\"{dataPath}\" using 1:{1 + graph.NumLines * 2 + 1}:{1 + graph.NumLines * 2 + 2} with errorlines ls 16 title \"Baseline {baseline.LineLabel}\",\\\n";

        // Write .gnuplot script file
        try
        {
            File.WriteAllText(gnuplotScriptPath, gnuplotFile);

            // Also need to create the Plots folder if it does not exist.
            string paths = Path.GetDirectoryName(graphPath);
            if (!Directory.Exists(paths))
            {
                Directory.CreateDirectory(paths);
            }
        }
        catch
        {
            Logger.Warn($"ConstructGnuplotFile: Unable to write to file {gnuplotScriptPath}.");
        }
    }


    /// <summary>
    /// Runs a gnuplot script as a hidden process.
    /// </summary>
    /// <param name="gnuplotScriptPath">The gnuplot script to run.</param>
    private static void RunGnuplot(string gnuplotScriptPath)
    {
        // Run gnuplot
        Process p = new();

        // This gives the folder /Assets in Editor, and /Builds/Build_X/Dissertation_Data folder in Player
        string gnuplotExecutablePath = Application.dataPath;
        
        // If standalone, navigate back from the Builds/Build_X/Dissertation_Data folder (3 backwards navigations)
        // Then, navigate to Assets to put it in the same folder as the Editor's data path.
        if (!Application.isEditor)
        {
            gnuplotExecutablePath += "\\..\\..\\..\\Assets";
        }

        // Now we are in the folder containing Assets
        gnuplotExecutablePath += "\\gnuplot\\bin\\gnuplot.exe";

        p.StartInfo = new(gnuplotExecutablePath, gnuplotScriptPath);
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; // Hide the window
        p.Start();
    }
}