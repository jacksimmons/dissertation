using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


/// <summary>
/// Tests for Genetic Algorithm functions.
/// </summary>
public class Test_GA : GeneticAlgorithm
{
    [Test]
    public void TestSelectionOperator()
    {
        Tuple<Day, Day> selected = Selection(GetStartingPopulation());
    }
}
