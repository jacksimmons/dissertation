using System.Collections.Generic;

public static class AlgorithmOutput
{
    private static Dictionary<Nutrient, float> m_prevAvg = new();


    public static string GetDayLabel(Day day)
    {
        string label = $"Portions: {day.Portions.Count}";
        if (Preferences.Instance.gaType == GAType.ParetoDominance)
        {
            AlgPDGA pdga = (AlgPDGA)Algorithm.Instance;
            label += $" Rank: {pdga.Sorting.TryGetDayRank(day)}";
        }
        if (Preferences.Instance.gaType == GAType.SummedFitness)
            label += $" Fitness: {day.Fitness:F2}";

        return label;
    }


    public static string GetAverageStatsLabel()
    {
        string avgStr = "";
        foreach (Nutrient nutrient in Nutrients.Values)
        {
            float sum = 0;
            foreach (Day day in Algorithm.Instance.Population)
            {
                sum += day.GetNutrientAmount(nutrient);
            }

            float avg = sum / Nutrients.Count;
            if (!m_prevAvg.ContainsKey(nutrient)) m_prevAvg[nutrient] = avg;

            avgStr += $"{nutrient}: {avg:F2}(+{avg - m_prevAvg[nutrient]:F2}){Nutrients.GetUnit(nutrient)}\n";

            m_prevAvg[nutrient] = avg;
        }
        return avgStr;
    }
}