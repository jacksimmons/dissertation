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
            Preferences.Instance = prefs;

            // Algorithm setup
            AlgorithmRunnerCore core = new();

            // Input loop
            int input = GetInput();
            while (input != -1)
            {
                float ms = core.RunIterations(input);

                Console.WriteLine($"\nIterations: {core.IterNum - 1}");
                Console.WriteLine("Average stats:");
                Console.Write(AlgorithmOutput.GetAverageStatsLabel());
                Console.WriteLine($"Execution took {ms}ms.\n", Severity.Log);

                input = GetInput();
            }
        }


        private static int GetInput()
        {
            Console.Write("Enter the number of iterations to perform:\n> ");
            string? line = Console.ReadLine();

            if (line != null && int.TryParse(line, out int value))
            {
                return value;
            }
            return -1;
        }
    }
}
