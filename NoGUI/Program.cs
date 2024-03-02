using System;
using System.Collections.Generic;
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
        public static void Main()
        {
            // Preferences setup
            Preferences prefs = new();
            prefs.MakeVegan();
            prefs.algType = AlgorithmType.GA;
            Console.Write(prefs.Verbose());


            // Get some preferences input
            Console.WriteLine("Enter your goals below:\n");
            float[]? goals = GetNutrientInput();
            if (goals != null) prefs.goals = goals;

            Preferences.Instance = prefs;

            // Algorithm setup
            AlgorithmRunnerCore core = new();

            // Input loop
            int numIters = GetIterInput();
            while (numIters != -1)
            {
                float ms = core.RunIterations(numIters);

                Console.WriteLine($"\nIterations: {core.IterNum - 1}");
                Console.WriteLine("Average stats:");
                Console.Write(AlgorithmOutput.GetAverageStatsLabel());
                Console.WriteLine($"Execution took {ms}ms.\n", Severity.Log);

                numIters = GetIterInput();
            }
        }


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
    }
}
