using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NoGUI
{
    /// <summary>
    /// Used to perform experiments on the algorithms.
    /// </summary>
    internal class Program
    {
        // AUTO_ITERTATIONS == true means the algorithm will keep iterating until it hits
        // MAX_ITERS. == false means you will input the number of iterations to perform, then
        // it will ask again after finishing.
        private const bool AUTO_ITERATIONS = true;
        // The number of iterations the program will perform if AUTO_ITERATIONS == true.
        private const int NUM_AUTO_ITERATIONS = 1_000_000;


        // Max fitness value plotted on the output graph.
        private const int MAX_FITNESS = 50_000;


        private static List<Tuple<float, float, float>> s_datapoints = new();


        public static void Main()
        {
            // Preferences setup
            Preferences prefs = new();
            prefs.MakeVegan();
            prefs.algorithmType = typeof(AlgACO).FullName!; // Set the type in typeof(*)
            prefs.populationSize = 10;
            Console.Write(prefs.Verbose());
            Preferences.Instance = prefs;


            // Algorithm setup (and error handling)
            AlgorithmRunnerCore core = new();
            if (core.Alg.DatasetError != "")
            {
                Logger.Log($"Dataset Error: {core.Alg.DatasetError}", Severity.Error);
            }


            if (!AUTO_ITERATIONS)
            {
                // Input loop
                int numIters = GetIterInput();
                while (numIters != -1) // While a valid integer was provided
                {
                    float ms = core.RunIterations(numIters);

                    Console.WriteLine($"\nIteration Num: {core.Alg.IterNum}");

                    // Print the average stats of the population
                    Console.WriteLine("Average stats:");
                    Console.Write(Algorithm.Instance.GetAverageStatsLabel() + "\n");

                    // Print the best ever stats (and store in graph)
                    if (core.Alg.BestDayExists)
                    {
                        string fitness = float.IsPositiveInfinity(core.Alg.BestFitness) ? "inf" : core.Alg.BestFitness.ToString();
                        Console.WriteLine($"Best stats: (From Iteration {core.Alg.BestIteration}) [Fitness: {fitness}]");
                        Console.Write(core.Alg.BestDay.Verbose() + "\n");
                        AddToGraph(Algorithm.Instance.BestFitness, Algorithm.Instance.AverageFitness);
                    }

                    Console.WriteLine($"Execution took {ms}ms.\n", Severity.Log);

                    numIters = GetIterInput();
                }
            }
            else
            {
                Console.WriteLine($"Running iterations, please wait...");
                Stopwatch sw = Stopwatch.StartNew();
                int secondsPassed = 0;
                while (core.Alg.IterNum < NUM_AUTO_ITERATIONS)
                {
                    core.RunIterations(1);

                    if (core.Alg.BestDayExists)
                        AddToGraph(Algorithm.Instance.BestFitness, Algorithm.Instance.AverageFitness);

                    if (sw.ElapsedMilliseconds > secondsPassed * 1000)
                    {
                        Console.WriteLine($"Iterations: {core.Alg.IterNum}/{NUM_AUTO_ITERATIONS}", Severity.Log);
                        secondsPassed++;
                    }
                }

                Console.WriteLine($"Execution took {sw.ElapsedMilliseconds / 1000f}s.");
            }


            // Output program best
            Console.WriteLine($"Done.\n\nProgram best stats: (From Iteration {core.Alg.BestIteration}) [Fitness: {core.Alg.BestFitness}]\nPortions:\n");
            foreach (Portion p in core.Alg.BestDay.portions)
            {
                Console.Write(p.Verbose() + "\n");
            }
            Console.Write($"\nDay stats:\n{core.Alg.BestDay.Verbose()}\n\nNow rendering graph...\n");


            // Output graph
            double[] xs = s_datapoints.Select(x => (double)x.Item3).ToArray();
            double[] ys = s_datapoints.Select(x => (double)x.Item1).ToArray();

            Plot plot = MakeLinearPlot(core.Alg.IterNum, MAX_FITNESS, xs, ys);
            plot.SavePng(FileTools.GetProjectDirectory() + "Graph.png", 1920, 1080);
        }


        private static int GetIterInput()
        {
            Console.Write("[int] Enter the number of iterations to perform:\n> ");
            string? line = Console.ReadLine();

            if (line != null && int.TryParse(line, out int value) && value >= 0)
            {
                return value;
            }
            return -1;
        }


        /// <summary>
        /// Add the results to datapoints, to be output in a graph later.
        /// </summary>
#if !UNITY_64
        private static void AddToGraph(float bestFitness, float avgFitness)
        {
            s_datapoints.Add(new(bestFitness, 0, Algorithm.Instance.IterNum));
        }


        private static Plot InitPlot(double[] xs, double[] ys)
        {
            // Create Plot object
            Plot plot = new();

            // Labels
            plot.Title(Preferences.Instance.algorithmType);
            plot.XLabel("Iteration");
            plot.YLabel("Fitness");

            // Create scatter graph
            var scatter = plot.Add.Scatter(xs, ys);
            scatter.MarkerStyle = MarkerStyle.None; // Remove dots, as they form a thick line when spaced closely

            return plot;
        }


        private static Plot MakeLinearPlot(int xMax, int yMax, double[] xs, double[] ys)
        {
            Plot plot = InitPlot(xs, ys);

            plot.Axes.SetLimitsX(0, xMax);
            plot.Axes.SetLimitsY(0, yMax);

            return plot;
        }


        /// <summary>
        /// Adjust the "ticks" (axis jumps) so they match the log/log graph data.
        /// Reference: https://scottplot.net/cookbook/5.0/CustomizingTicks/LogScaleTicks/ (ScottPlot docs)
        /// </summary>
        /// <param name="plot">The plot to apply the custom ticks to.</param>
        private static Plot MakeLogPlot(int xMax, int yMax, double[] xs, double[] ys)
        {
            Plot plot = InitPlot(xs, ys);

            // Log tick generator
            ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGen = new();

            // Numeric tick generator using the log tick generator
            ScottPlot.TickGenerators.NumericAutomatic tickGen = new()
            {
                MinorTickGenerator = minorTickGen,

                // Tick formatter, sets label text for each tick
                LabelFormatter = (double y) =>
                {
                    return $"{Math.Pow(10, y):N0}";
                },

                // tell our major tick generator to only show major ticks that are whole integers
                IntegerTicksOnly = true
            };

            // Apply tick generator to axes
            plot.Axes.Left.TickGenerator = tickGen;
            plot.Axes.Bottom.TickGenerator = tickGen;

            // Show grid lines for minor ticks
            var grid = plot.GetDefaultGrid();
            grid.MajorLineStyle.Color = Colors.Black.WithOpacity(.15);
            grid.MinorLineStyle.Color = Colors.Black.WithOpacity(.05);
            grid.MinorLineStyle.Width = 1;

            // Set axis limits
            plot.Axes.SetLimitsX(0, Math.Log10(xMax));
            plot.Axes.SetLimitsY(0, Math.Log10(yMax));

            double[] logXs = xs.Select(Math.Log10).ToArray();
            double[] logYs = ys.Select(Math.Log10).ToArray();

            return plot;
        }
#endif
    }
}
