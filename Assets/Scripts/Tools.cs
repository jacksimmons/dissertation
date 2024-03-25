/// A collection of tools (extension classes).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Random = System.Random;
using Debug = UnityEngine.Debug;
using System.Net.NetworkInformation;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UIElements;
using Unity.VisualScripting;


public interface IVerbose
{
    /// <summary>
    /// Gets a verbose description of this object.
    /// </summary>
    /// <returns>The string data.</returns>
    public string Verbose();
}



public enum ELogSeverity
{
    Log,
    Warning,
    Error
}


/// <summary>
/// An extension class for logging methods.
/// </summary>
public static class Logger
{
    private static MenuStackHandler Menu => GameObject.FindWithTag("MenuStackHandler").GetComponent<MenuStackHandler>();


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
            case ELogSeverity.Error:
                throw new Exception($"{message}");
        }
    }


    public static void Warn(object message) => Log(message, ELogSeverity.Warning);
    public static void Error(object message) => Log(message, ELogSeverity.Error);
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


    public static T CircularNextElement<T>(T[] arr, int current, bool right)
    {
        return arr[CircularNextIndex(current, arr.Length, right)];
    }


    /// <summary>
    /// Gets the next index of an arbitrary array in a circular queue fashion.
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
    // For grams stored as a float, gives precision of up to 1mg.
    public const float EPSILON = 1e-3f;
    private static readonly Random m_rand = new();


    /// <summary>
    /// Returns true if the absolute difference between two floats is smaller than
    /// the defined constant EPSILON. Returns false if not.
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
    /// Returns the first index of a probability array to be surpassed by a random 0-EPSILON value when summing over all
    /// elements in the array.
    /// </summary>
    public static int GetFirstSurpassedProbability(float[] probabilities)
    {
        int length = probabilities.Length;
        if (length < 1)
            Logger.Error("Invalid probability array had a length of < 1");

        // Clamp the probability to the range [EPSILON, 1 - EPSILON]
        float probability = MathF.Max(MathF.Min((float)m_rand.NextDouble(), 1 - EPSILON), EPSILON);

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
    /// [Citation needed] 1g of protein, fat or carbohydrate has 4, 9 or 4 kcal caloric value respectively.
    /// </summary>
    /// <param name="calories">The calories to update.</param>
    public static void MacrosToCalories(ref float calories, float proteinG, float fatG, float carbsG)
    {
        calories = proteinG * 4 + fatG * 8 + carbsG * 4;
    }


    /// <summary>
    /// If macros existed before, multiplies them by a ratio to ensure the macro calorie value equals the preferences calorie value.
    /// Otherwise, generates macros according to a sensible ratio of p/f/c: 20/35/45
    /// [Citation needed]
    /// </summary>
    /// <param name="calories">The calories to generate sensible macros for.</param>
    public static void CaloriesToMacros(float calories, ref float proteinG, ref float fatG, ref float carbsG)
    {
        // Calories calculated from the macros (not necessarily correct, goal is to correct it).
        float ratioCalories = 4 * proteinG + 9 * fatG + 4 * carbsG;

        // Div by zero case - assign recommended macro ratios from the given kcal.
        if (MathTools.Approx(ratioCalories, 0))
        {
            float proteinCalories = 0.2f * calories;
            float fatCalories = 0.35f * calories;
            float carbCalories = 0.45f * calories;

            // Convert calories to grams by dividing by each macro's respective ratio
            proteinG = proteinCalories / 4;
            fatG = fatCalories / 9;
            carbsG = carbCalories / 4;

            return;
        }

        // Default case - multiply macros so they satisfy the ratio kcal = 9fat + 4protein + 4carbs
        float multiplier = calories / ratioCalories;

        proteinG *= multiplier;
        fatG *= multiplier;
        carbsG *= multiplier;
    }
}


/// <summary>
/// An extension class for plotting graphs using gnuplot.
/// </summary>
public static class PlotTools
{
    public static void PlotLine(Coordinates[] line, int numIters)
    {
        PlotMultipleLines(new Coordinates[][] { line }, numIters);
    }


    public static void PlotMultipleLines(Coordinates[][] lines, int numIters)
    {
        // Make an Nx1 array by converting each Coordinates object into a Coordinates[] of len 1
        Coordinates[,] graph = new Coordinates[numIters, lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            for (int j = 0; j < numIters; j++)
            {
                graph[j, i] = lines[i][j];
            }
        }

        PlotGraph(graph, numIters, lines.Length);
    }


    public static void PlotGraph(Coordinates[,] graph, int numIters, int numLines)
    {
        // Check dimensions provided
        if (graph.GetLength(0) != numIters || graph.GetLength(1) != numLines)
        {
            Logger.Warn($"Provided dimensions ({numIters}x{numLines}) don't match with actual dimensions ({graph.GetLength(0)}x{graph.GetLength(1)}).");
            return;
        }

        // Check for all infinite values
        for (int i = 0; i < numIters; i++)
        {
            for (int j = 0; j < numLines; j++)
            {
                if (float.IsFinite(graph[i, j].Y)) goto valid;
            }

            Logger.Warn("All values in one of the algorithms were non-finite. Try more iterations or having less restrictive constraints.");
        valid:
            continue;
        }

        string dataFilePath = Application.persistentDataPath + "/plot.dat";
        string gnuplotFilePath = Application.persistentDataPath + "/plot.gnuplot";
        string graphFilePath = $"{Application.persistentDataPath}/Plots/{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png";

        ConstructDataFile(graph, numIters, numLines, dataFilePath);
        ConstructGnuplotFile(graph, numIters, numLines, graphFilePath, gnuplotFilePath, dataFilePath);
        RunGnuplot(gnuplotFilePath);
    }


    /// <summary>
    /// Constructs the .dat file which the gnuplot file will extract a png graph from.
    /// </summary>
    private static void ConstructDataFile(Coordinates[,] graph, int numIters, int numLines, string dataFilePath)
    {
        // Create data file
        if (!File.Exists(dataFilePath))
            File.Create(dataFilePath).Close();

        // Only add best fitness changes to the file (and a point for the last iteration)
        string dataFileContent = "";

        float[] bestFitnessesSoFar = new float[numLines];
        for (int i = 0; i < numLines; i++)
        {
            bestFitnessesSoFar[i] = float.PositiveInfinity;
        }

        // Iterate over iteration
        // ith iteration for all lines
        for (int i = 0; i < numIters; i++)
        {
            string dataFileLine = $"{i + 1}";

            // Iterate over line num
            // jth line for the ith iteration
            int numLinesNotImproved = 0;
            for (int j = 0; j < numLines; j++)
            {
                float fitness = graph[i, j].Y;
                dataFileLine += float.IsPositiveInfinity(fitness) ? " inf" : $" {graph[i, j].Y}";

                if (fitness < bestFitnessesSoFar[j] || graph[i, j].X == numIters)
                {
                    bestFitnessesSoFar[j] = fitness;
                }
                else
                {
                    numLinesNotImproved++;
                }
            }

            dataFileLine += '\n';

            // Only write the line if at least one of the lines has improved on its best fitness
            if (numLinesNotImproved < numLines)
            {
                dataFileContent += dataFileLine;
            }
        }

        // Write .dat data file
        File.WriteAllText(dataFilePath, dataFileContent);
    }


    /// <summary>
    /// Constructs the gnuplot script file which will create the graph using gnuplot, and output it as png.
    /// </summary>
    private static void ConstructGnuplotFile(Coordinates[,] graph, int numIters, int numLines, string graphPath, string gnuplotScriptPath, string dataPath)
    {
        // Create gnuplot script file
        if (!File.Exists(gnuplotScriptPath))
            File.Create(gnuplotScriptPath).Close();

        // Find the maximum finite fitness of the graph (checked earlier that not all points are infinity)
        float maxFitness = 0;

        for (int i = 0; i < numIters; i++)
        {
            for (int j = 0; j < numLines; j++)
            {
                float fitness = graph[i, j].Y;
                if (fitness > maxFitness && float.IsFinite(fitness))
                {
                    maxFitness = fitness;
                }
            }
        }

        string gnuplotFile
        = "set terminal png enhanced\n"
        + $"set output \"{Path.GetRelativePath(Directory.GetCurrentDirectory(), graphPath).Replace("\\", "/")}\"\n"
        + $"set xrange [0: {numIters}]\n"
        + $"set yrange [0: {maxFitness}]\n"
        + "set title \"Graph of Best Fitness against Iteration Number\"\n"
        + "set xlabel \"Iteration\"\n"
        + "set ylabel \"Best Fitness\"\n"
        + "plot ";

        string dataRelativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), dataPath).Replace("\\", "/");
        for (int l = 0; l < numLines; l++)
        {
            gnuplotFile += $"\"{dataRelativePath}\" using 1:{l+2} with lines title \"Algorithm {l} (Final Best Fitness: {graph[numIters - 1, l].Y})\",\\\n";
        }

        // Write .gnuplot script file
        File.WriteAllText(gnuplotScriptPath, gnuplotFile);
    }


    /// <summary>
    /// Runs a gnuplot script as a hidden process.
    /// </summary>
    /// <param name="gnuplotScriptPath">The gnuplot script to run.</param>
    private static void RunGnuplot(string gnuplotScriptPath)
    {
        // Run gnuplot
        Process p = new();
        p.StartInfo = new(Application.dataPath + "\\gnuplot\\bin\\gnuplot.exe", gnuplotScriptPath);
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; // Hide the window
        p.Start();
    }
}