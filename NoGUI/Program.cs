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
        public static void Main()
        {
            // Preferences setup
            Preferences prefs = new();
            prefs.MakeVegan();
            prefs.algorithmType = typeof(AlgSFGA).FullName!; // Set the type in typeof(*)
            prefs.populationSize = 10;
            Console.Write(prefs.Verbose());
            Preferences.Instance = prefs;

            Stopwatch sw = Stopwatch.StartNew();
            Experiment.RunPreferenceExperiment(ref Preferences.Instance.populationSize, 10, 100);
            sw.Stop();

            Console.WriteLine($"Program execution time: {(float)sw.ElapsedMilliseconds / 1000}s.");
        }


        //private static int GetIterInput()
        //{
        //    Console.Write("[int] Enter the number of iterations to perform:\n> ");
        //    string? line = Console.ReadLine();

        //    if (line != null && int.TryParse(line, out int value) && value >= 0)
        //    {
        //        return value;
        //    }
        //    return -1;
        //}
    }
}
