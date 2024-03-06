using System.Collections.Generic;


public class AlgSFGA : AlgGA
{
    protected override Day Selection(List<Day> candidates, bool selectBest = true)
    {
        int indexA = Rand.Next(0, candidates.Count);
        // Ensure B is different to A by adding an amount less than the list size, then %-ing it.
        int indexB = (indexA + Rand.Next(1, candidates.Count - 1)) % candidates.Count;

        float fitA = candidates[indexA].Fitness;
        float fitB = candidates[indexB].Fitness;

        if (selectBest)
        {
            if (fitA < fitB) return candidates[indexA];
            if (fitB < fitA) return candidates[indexB];
        }
        else
        {
            if (fitA > fitB) return candidates[indexA];
            if (fitB > fitA) return candidates[indexB];
        }

        return Tiebreak(candidates[indexA], candidates[indexB]);
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