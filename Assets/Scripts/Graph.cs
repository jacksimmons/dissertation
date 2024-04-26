// Commented 24/4
using System;
using System.Linq;


/// <summary>
/// A struct representing a collection of mean lines.
/// </summary>
public readonly struct Graph
{
    /// <summary>
    /// Values that are supported on the Y-Axis.
    /// </summary>
    public enum YAxis { BestDayFitness, BestDayMass };

    public int NumLines => MeanLines.Length;
    public int NumIters => MeanLines[0].NumIters;

    public MeanLine[] MeanLines { get; }


    public Graph(int numMeanLines)
    {
        MeanLines = new MeanLine[numMeanLines];
    }


    /// <summary>
    /// Converts MeanLines to a list of lines in plottable format.
    /// First index = the line it corresponds to.
    /// Second index = the iteration it corresponds to.
    /// Value = the y value.
    /// </summary>
    /// <returns>Plottable form of the graph.</returns>
    public float[][] ToPlottable()
    {
        return MeanLines.Select(x => x.Means).ToArray();
    }
}


/// <summary>
/// A struct representing a mean line - a collection of lines that represent the makings of a mean line.
/// The mean line is constructed from the mean of these individual lines at each iteration, with error
/// bars for standard deviation.
/// </summary>
public struct MeanLine
{
    public int NumAlgs { get; }
    public readonly int NumIters => Means.Length;
    public string LineLabel { get; set; }

    /// <summary>
    /// i corresponds to x = i - 1, Iterations[i] corresponds to y = Iterations[i].
    /// </summary>
    public float[] Means { get; }
    public float[] StandardDeviations { get; }


    /// <summary>
    /// Internal constructor for code reuse.
    /// <param name="means">Lol</param>
    /// </summary>
    private MeanLine(float[] means, int numAlgs)
    {
        NumAlgs = numAlgs;
        LineLabel = $"(Final: {means[^1]})";
        Means = means;
        StandardDeviations = new float[Means.Length];
    }


    /// <summary>
    /// Construct a mean line from an array of precalculated means, and a 2D array of dimensions [lines][iterations]
    /// </summary>
    /// <param name="means"></param>
    /// <param name="lines"></param>
    public MeanLine(float[] means, float[][] lines) : this(means, lines == null ? 1 : lines.Length)
    {
        // Calculate standard deviation for each iteration in Means, if lines was provided. If lines == null,
        // don't add error bars. Ensure lines == null for experiments, as error bars on these are too cluttered.
        if (lines == null) return;
        for (int i = 0; i < Means.Length; i++)
        {
            float[] pts = new float[NumAlgs];
            for (int j = 0; j < NumAlgs; j++)
            {
                pts[j] = lines[j][i];
            }

            // Calculate population standard deviation (stdDev = sqrt(sum(pow(point - mean), 2)) / numLines)
            float mean = means[i];
            float stdDev = MathF.Sqrt(pts.Sum(y => MathF.Pow(y - mean, 2)) / NumAlgs);

            StandardDeviations[i] = stdDev;
        }
    }


    /// <summary>
    /// Construct a mean line from arrays of precalculated means and standard deviations.
    /// </summary>
    public MeanLine(float[] means, float[] stdDev, int numAlgs) : this(means, numAlgs)
    {
        StandardDeviations = stdDev;
    }


    /// <summary>
    /// Generate a meanline from a baseline object.
    /// </summary>
    public static MeanLine FromBaseline(Baseline b)
    {
        return new(b.means, b.stdDevs, Baseline.ALGS);
    }
}


/// <summary>
/// Represents a baseline, these can only be created by modifying the code.
/// This was used to create the baselines, and is used to deserialise the serialised baselines used
/// in experiments.
/// </summary>
[Serializable]
public struct Baseline
{
    public const int ALGS = 10;
    public const int ITERS = 1000;
    public float[] means;
    public float[] stdDevs;


    public Baseline(float[] means, float[] stdDevs)
    {
        this.means = means;
        this.stdDevs = stdDevs;
    }
}