using System;
using System.Collections.Generic;


public class AlgSFGA : AlgGA
{
    protected override Day Selection(List<Day> included, bool selectBest = true)
    {
        Tuple<Day, Day> days = SelectDays(included);
        Day a = days.Item1;
        Day b = days.Item2;


        float fitA = m_population.GetFitness(a);
        float fitB = m_population.GetFitness(b);


        if (selectBest)
        {
            if (fitA < fitB) return a;
            if (fitB < fitA) return b;
        }
        else
        {
            if (fitA > fitB) return a;
            if (fitB > fitA) return b;
        }

        return Tiebreak(a, b);
    }


    /// <summary>
    /// In a summed fitness algorithm, just randomly pick the tiebreak winner.
    /// </summary>
    protected Day Tiebreak(Day a, Day b)
    {
        if (Rand.Next(0, 2) == 1)
            return a;
        return b;
    }
}