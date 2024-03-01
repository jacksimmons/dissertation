using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoGUI
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Preferences.Instance = new();
            Preferences.Instance.MakeVegan();

            AlgorithmRunnerCore core = new AlgorithmRunnerCore();
            
            float ms = core.RunIterations(1);
            Static.Log($"Execution took {ms}ms.", Severity.Log);
        }
    }
}
