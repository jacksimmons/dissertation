using System.Collections.Generic;

public static class AlgorithmOutput
{
    public static string GetDayLabel(Day day)
    {
        string label = $"Portions: {day.portions.Count} ";
        label += Algorithm.Instance.EvaluateDay(day);
        return label;
    }
}