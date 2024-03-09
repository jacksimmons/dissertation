using ScottPlot;
using System;
using System.Collections.Generic;
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
        private const bool AUTO_ITERATIONS = false;


        // Set this to > 0 to automatically run iters until MAX_ITERS
        private const int ITER_SPACING = 1;

        // No iterations can occur past this number. Make it infinity for it to
        // go forever.
        private const int MAX_ITERS = 1_000_000;

        // Max fitness plotted on the output graph.
        private const int MAX_FITNESS = 1_000_000;


        public static void Main()
        {
            // Preferences setup
            Preferences prefs = new();
            prefs.MakeVegan();
            prefs.algorithmType = typeof(AlgACO).FullName!;
            Console.Write(prefs.Verbose());
            Preferences.Instance = prefs;


            // Algorithm setup (and error handling)
            AlgorithmRunnerCore core = new();
            if (core.Alg.DatasetError != "")
            {
                throw new Exception($"Dataset Error: {core.Alg.DatasetError}");
            }


            if (!AUTO_ITERATIONS)
            {
                // Input loop
                int numIters = GetIterInput();
                while (numIters != -1 && core.Alg.IterNum < MAX_ITERS)
                {
                    float ms = core.RunIterations(numIters);

                    Console.WriteLine($"\nIteration Num: {core.Alg.IterNum}");

                    // Print the average stats of the population
                    Console.WriteLine("Average stats:");
                    Console.Write(Algorithm.Instance.GetAverageStatsLabel() + "\n");

                    // Print the best ever stats
                    if (core.Alg.BestDay != null)
                    {
                        string fitness = float.IsPositiveInfinity(core.Alg.BestFitness) ? "inf" : core.Alg.BestFitness.ToString();
                        Console.WriteLine($"Best stats: (From Iteration {core.Alg.BestIteration}) [Fitness: {fitness}]");
                        Console.Write(core.Alg.BestDay.Verbose() + "\n");
                    }

                    Console.WriteLine($"Execution took {ms}ms.\n", Severity.Log);

                    numIters = GetIterInput();
                }
            }
            else
            {
                Console.WriteLine($"Running iterations, please wait...", Severity.Log);
                Stopwatch sw = Stopwatch.StartNew();
                int secondsPassed = 0;
                while (core.Alg.IterNum < MAX_ITERS)
                {
                    core.RunIterations(ITER_SPACING);

                    if (sw.ElapsedMilliseconds > secondsPassed * 1000)
                    {
                        Console.WriteLine($"Iterations: {core.Alg.IterNum}/{MAX_ITERS}", Severity.Log);
                        secondsPassed++;
                    }
                }
            }


            // Output program best
            Console.WriteLine($"Done.\n\nProgram best stats: (From Iteration {core.Alg.BestIteration}) [Fitness: {core.Alg.BestFitness}]");
            Console.Write(core.Alg.BestDay.Verbose() + "\n\nNow rendering graph...\n");


            // Output graph
            Plot plot = new();

            double[] xs = core.datapoints.Select(x => (double)x.Item3).ToArray();
            double[] ys = core.datapoints.Select(x => (double)x.Item1).ToArray();
            double[] logXs = xs.Select(Math.Log10).ToArray();
            double[] logYs = ys.Select(Math.Log10).ToArray();


            // Labels
            plot.Title(Preferences.Instance.algorithmType);
            plot.XLabel("Iteration");
            plot.YLabel("Fitness");


            // Create scatter plot
            var scatter = plot.Add.Scatter(logXs, logYs);
            scatter.MarkerStyle = MarkerStyle.None; // Remove dots, as they form a thick line when spaced closely


            // Render the graph
            ApplyCustomPlotTicks(plot);
            plot.Axes.SetLimitsX(0, Math.Log10(core.Alg.IterNum));
            plot.Axes.SetLimitsY(0, Math.Log10(MAX_FITNESS));
            plot.SavePng(FileTools.GetProjectDirectory() + "Graph.png", 1920, 1080);
        }


        /// <summary>
        /// (Unused)
        /// Allows the user to input the nutrients in command-line.
        /// </summary>
        private static float[]? GetNutrientInput()
        {
            float[] floats = new float[Nutrients.Count];
            for (int i = 0; i < Nutrients.Count; i++)
            {
                Console.Write($"[float] Enter the value for {Nutrients.Values[i]}\n> ");
                string? line = Console.ReadLine();

                if (line != null && float.TryParse(line, out floats[i]))
                {
                    if (floats[i] < 0)
                        return null;

                    continue;
                }

                // Restart the iteration
                Console.WriteLine("Invalid input. Please try again.");
                i--;
            }

            return floats;
        }


        private static int GetIterInput()
        {
            Console.Write("[int] Enter the number of iterations to perform:\n> ");
            string? line = Console.ReadLine();

            if (line != null && int.TryParse(line, out int value))
            {
                return value;
            }
            return -1;
        }


        /// <summary>
        /// Adjust the "ticks" (axis jumps) so they match the log/log graph data.
        /// </summary>
        /// <param name="plot">The plot to apply the custom ticks to.</param>
        private static void ApplyCustomPlotTicks(Plot plot)
        {
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
        }
    }
}
