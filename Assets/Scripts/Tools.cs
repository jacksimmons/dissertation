/// A collection of tools (extension classes).


using System;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_64
using UnityEngine;
using Random = System.Random;
#endif


public enum Severity
{
    Log,
    Warning,
    Error
}


/// <summary>
/// A generic logging class which supports Unity.Debug and System.Console output.
/// </summary>
public static class Logger
{
    /// <summary>
    /// Handles logging for both Unity and NoGUI projects.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="severity">Severity of the log, affecting the output stream
    /// or message. Default is Log.</param>
    public static void Log(object message, Severity severity = Severity.Log, [System.Runtime.CompilerServices.CallerMemberName] string memName = "")
    {
#if UNITY_64
        switch (severity)
        {
            case Severity.Log:
                Debug.Log(message);
                break;
            case Severity.Warning:
                Debug.LogWarning(message);
                break;
            case Severity.Error:
                throw new Exception($"{memName} ERROR: {message}");
        }
#else
        switch (severity)
        {
            case Severity.Log:
                Console.WriteLine($"{memName}: {message}");
                break;
            case Severity.Warning:
                Console.WriteLine($"{memName} WARN: {message}");
                break;
            case Severity.Error:
                throw new Exception($"{memName} ERROR: {message}");
        }
#endif
    }


    public static void Warn(object message) => Log(message, Severity.Warning);
    public static void Error(object message) => Log(message, Severity.Error);
}


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


#if UNITY_64
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
}
#endif


public static class MathfTools
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
}


#if !UNITY_64
public static class FileTools
{
    public static string GetProjectDirectory()
    {
        // E.g. Project/bin/Debug/net8.0/
        string cwd = Environment.CurrentDirectory;

        
        DirectoryInfo compileTarget = Directory.GetParent(cwd)!; // E.g. Project/bin/Debug/
        DirectoryInfo bin = compileTarget.Parent!; // E.g. Project/bin/
        
        // Now in Project folder
        return bin.Parent!.FullName + "\\";
    }
}
#endif