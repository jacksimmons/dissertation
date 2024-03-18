using ScottPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NoGUI
{
    internal class Experiment
    {
        // Number of runs in an experiment.
        public const int NUM_STEPS = 11;

        // Number of algorithms running in parallel.
        public const int NUM_ALGORITHMS = 16;

        // The number of iterations the program will perform if AUTO_ITERATIONS == true.
        private const int NUM_ITERATIONS = 10_000;


        public readonly struct AlgorithmSetResult
        {
            public readonly Algorithm bestAlgorithm;
            public readonly float averageFitness;


            public AlgorithmSetResult(Algorithm bestAlgorithm, float averageFitness)
            {
                this.bestAlgorithm = bestAlgorithm;
                this.averageFitness = averageFitness;
            }
        }


        /// <summary>
        /// Sets the preference to `min`. Then repeatedly runs a RunAlgorithms call, and increases the preference by a step (NUM_STEPS) until `max` is reached.
        /// </summary>
        /// <param name="intPref">A reference to the preference.</param>
        /// <param name="min">The minimum value assigned initially.</param>
        /// <param name="max">The maximum value to be tested.</param>
        public static int RunPreferenceExperiment(ref int intPref, int min, int max)
        {
            Logger.Log($"Running experiment with {NUM_ALGORITHMS} algs, {NUM_ITERATIONS} iters, range [{min}, {max}], steps: {NUM_STEPS}.");

            intPref = min;
            int step = (max - min) / NUM_STEPS + 1;

            bool bestPrefSet = false;
            int bestPrefVal = -1;
            Algorithm experimentBestAlg = null;

            // Run NUM_STEPS more tests
            for (int i = 0; i < NUM_STEPS; i++)
            {
                Logger.Log($"Running step {i}: {intPref}.");
                AlgorithmSetResult result = RunAlgorithmSet(false);
                Logger.Log("\n");

                if (experimentBestAlg == null || result.bestAlgorithm.BestFitness < experimentBestAlg.BestFitness)
                {
                    experimentBestAlg = result.bestAlgorithm;
                    bestPrefSet = true;
                    bestPrefVal = intPref;
                }

                intPref = Math.Min(intPref + step, max);
            }


            if (!bestPrefSet) Logger.Error("Experiment didn't yield a best result.");

            Logger.Log($"Best preference value: {bestPrefVal}. Best fitness {experimentBestAlg.BestFitness}" +
                "\n{experimentBestAlg.BestDay.Verbose()}");
            return bestPrefVal;
        }


        /// <returns>The best performing algorithm that ran, and the average fitness of the set.</returns>
        public static AlgorithmSetResult RunAlgorithmSet(bool verbose = true)
        {
            Console.WriteLine($"Running {NUM_ALGORITHMS} algorithms for {NUM_ITERATIONS} iterations each.");

            ManualResetEvent completionEvent = new(false);
            int threadsLeft = NUM_ALGORITHMS;

            Algorithm bestAlg = null;
            float bestFitnessSum = 0;

            for (int i = 0; i < NUM_ALGORITHMS; i++)
            {
                void Run(object state)
                {
                    object[] args = state as object[];
                    Algorithm alg = RunAlgorithm((bool)args![0], (int)args![1], false);

                    bestFitnessSum += alg.BestFitness;
                    if (bestAlg == null || alg.BestFitness < bestAlg.BestFitness)
                    {
                        bestAlg = alg;
                    }

                    if (Interlocked.Decrement(ref threadsLeft) == 0) completionEvent.Set();
                }

                ThreadPool.QueueUserWorkItem(Run!, new object[] { i == 0 && verbose, i });
            }

            completionEvent.WaitOne();

            float avgBestFitness = bestFitnessSum / NUM_ALGORITHMS;
            Console.WriteLine($"Algorithm set: Average best fitness: {avgBestFitness}");
            Console.WriteLine($"Algorithm set: Overall best fitness: {bestAlg.BestFitness}");

            if (verbose)
            {
                Logger.Log($"Details:\n{bestAlg.BestDay.Verbose()}");
            }

            return new(bestAlg, avgBestFitness);
        }


        /// <returns>The algorithm that ran.</returns>
        public static Algorithm RunAlgorithm(bool outputIterNum, int id = 0, bool verbose = true)
        {
            // Algorithm setup
            AlgorithmRunner core = new();
            if (core.Alg.DatasetError != "")
            {
                Logger.Log($"Dataset Error: {core.Alg.DatasetError}", Severity.Error);
            }


            // Run the iterations
            Stopwatch sw = Stopwatch.StartNew();
            int secondsPassed = 0;

            List<Tuple<float, float, float>> graph = new();
            while (core.Alg.IterNum < NUM_ITERATIONS)
            {
                core.RunIterations(1);

                if (core.Alg.BestDayExists)
                    graph.Add(new(core.Alg.BestFitness, 0, core.Alg.IterNum));

                // Output the current iteration number every second
                if (sw.ElapsedMilliseconds > secondsPassed * 1000)
                {
                    if (outputIterNum)
                        Console.WriteLine($"Iterations: {core.Alg.IterNum}/{NUM_ITERATIONS}", Severity.Log);
                    secondsPassed++;
                }
            }
            sw.Stop();


            // Output results
            if (verbose)
            {
                // Output program best
                Console.WriteLine($"Algorithm best stats: (From Iteration {core.Alg.BestIteration}) [Fitness: {core.Alg.BestFitness}]");
                foreach (Portion p in core.Alg.BestDay.portions)
                {
                    Console.Write("\n" + p.Verbose());
                }
                Console.Write($"\nDay stats:\n{core.Alg.BestDay.Verbose()}\n\nNow rendering graph...\n");


                double[] xs = graph.Select(x => (double)x.Item3).ToArray();
                double[] ys = graph.Select(x => (double)x.Item1).ToArray();

                Plot plot = MakeLinearPlot(core.Alg.IterNum, Preferences.MAX_FITNESS, xs, ys);
                plot.SavePng(FileTools.GetProjectDirectory() + $"Graph_{id}.png", 1920, 1080);
            }
            return core.Alg;
        }


        /// <summary>
        /// Add the results to datapoints, to be output in a graph later.
        /// </summary>
#if !UNITY_64
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
