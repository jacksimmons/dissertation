using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;


namespace NoGUI
{
    /// <summary>
    /// Main class for the NoGUI component.
    /// </summary>
    internal static class Program
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

            //Experiment exp = new(100_000, 16);
            //Plotter.PlotExperiment(exp.RunBoolExperiment(ref Preferences.Instance.eatsAnimalProduce, "EatsAnimalProduce"));
            //Plotter.PlotExperiment(exp.RunBoolExperiment(ref Preferences.Instance.eatsLactose, "EatsLactose"));
            //Plotter.PlotExperiment(exp.RunBoolExperiment(ref Preferences.Instance.eatsLandMeat, "EatsMeat"));
            //Plotter.PlotExperiment(exp.RunBoolExperiment(ref Preferences.Instance.eatsSeafood, "EatsSeaFood"));

            Experiment exp = new(1_000_000, 16);
            Logger.Log(exp.RunAlgorithm(true).Verbose());
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
