public class AlgSFGA : AlgGA
{
    protected override Day Selection(List<Day> candidates, bool selectBest = true)
    {
        int indexA = m_rand.Next(0, candidates.Count);
        // Ensure B is different to A by adding an amount less than the list size, then %-ing it.
        int indexB = (indexA + m_rand.Next(1, candidates.Count - 1)) % candidates.Count;

        float fitA = candidates[indexA].Fitness;
        float fitB = candidates[indexB].Fitness;

        Day selected;

        if (fitA < fitB)
        {
            if (selectBest) selected = candidates[indexA];
            else selected = candidates[indexB];
        }
        else if (fitB < fitA)
        {
            if (selectBest) selected = candidates[indexB];
            else selected = candidates[indexA];
        }
        else
        {
            if (m_rand.Next(0, 2) == 1) selected = candidates[indexA];
            else selected = candidates[indexB];
        }

        return selected;
    }


    /// <summary>
    /// In a summed fitness algorithm, just randomly pick the tiebreak winner.
    /// </summary>
    protected override Day SelectionTiebreak(Day a, Day b)
    {
        if (m_rand.Next(0, 2) == 1)
            return a;
        return b;
    }
}