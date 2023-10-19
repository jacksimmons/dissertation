using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EA;


/// <summary>
/// A fixed-size amount of a food.
/// </summary>
public class Portion
{
    public string Name = "Unnamed";
    public float Scale = 1;

    public float KCAL;

    /// <summary>
    /// In grams.
    /// </summary>
    public float Protein, Fat, Carbs;

    /// <summary>
    /// Per 100g portion.
    /// </summary>
    public float Saturates;
    public float Sugars;


    public void Print()
    {
        Console.WriteLine($"{Name}\n kcal {KCAL}, protein {Protein}, fat {Fat}, carbs {Carbs}\n");
    }
}


public class Meal
{
    public List<Portion> Portions = new();


    public void Print()
    {
        foreach (Portion portion in Portions)
        {
            portion.Print();
        }
    }
}
