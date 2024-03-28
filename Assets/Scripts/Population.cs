using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


/// <summary>
/// A class which handles the data of a population of Days. Stores whether fitness is updated, which becomes false whenever
/// the portions of a day are modified. If a fitness is not updated, the Population recalculates it, but only when it is
/// requested.
/// </summary>
public class Population
{
    private Dictionary<Day, float> m_dayFitnesses;
    private Dictionary<Day, bool> m_dayFitnessUpToDate;
    private float m_avgFitness;
    private bool m_avgFitnessUpToDate;

    public ReadOnlyDictionary<Day, float> DayFitnesses;
    private List<Day> m_sortedDays;
    private bool m_sortedDaysUpToDate = false;


    public Population()
    {
        m_dayFitnesses = new();
        m_dayFitnessUpToDate = new();
        DayFitnesses = new(m_dayFitnesses);
    }


    public void Add(Day day)
    {
        m_dayFitnesses[day] = -1;
        m_dayFitnessUpToDate[day] = false;
        FlagPopulationAsOutdated();
    }


    public void Remove(Day day)
    {
        m_dayFitnesses.Remove(day);
        m_dayFitnessUpToDate.Remove(day);
        FlagPopulationAsOutdated();
    }


    /// <summary>
    /// Flag a day as outdated so the population knows to recalculate its fitness
    /// when it is next needed.
    /// </summary>
    public void FlagAsOutdated(Day day)
    {
        m_dayFitnessUpToDate[day] = false;
        FlagPopulationAsOutdated();
    }


    /// <summary>
    /// Flag a population change as having occurred - various parameters must be
    /// recalculated when next needed.
    /// </summary>
    private void FlagPopulationAsOutdated()
    {
        m_avgFitnessUpToDate = false;
        m_sortedDaysUpToDate = false;
    }


    public float GetFitness(Day day)
    {
        if (m_dayFitnessUpToDate[day]) return m_dayFitnesses[day];

        // Need to update it first.
        return UpdateAndGetFitness(day);
    }


    private float UpdateAndGetFitness(Day day)
    {
        m_dayFitnesses[day] = day.GetFitness();
        m_dayFitnessUpToDate[day] = true;
        m_avgFitnessUpToDate = false;
        return m_dayFitnesses[day];
    }


    public float GetAvgFitness()
    {
        if (m_avgFitnessUpToDate) return m_avgFitness;

        m_avgFitness = m_dayFitnesses.Sum(x => m_dayFitnessUpToDate[x.Key] ? x.Value : UpdateAndGetFitness(x.Key));
        return m_avgFitness;
    }


    /// <summary>
    /// Sorts a list of days, in increasing fitness (0 is best).
    /// </summary>
    public List<Day> GetSortedPopulation(bool reversed = false)
    {
        // Can early exit, provided the sorted days member is up to date.
        if (m_sortedDaysUpToDate)
        {
            List<Day> sortedDays = new(m_sortedDays);
            if (reversed)
            {
                sortedDays.Reverse();
            }
            return sortedDays;
        }

        List<Day> sortedPop = DayFitnesses.Keys.ToList();
        SortDayList(sortedPop);

        // Update the flag
        m_sortedDays = new(sortedPop);
        m_sortedDaysUpToDate = true;

        // Reverse the list separate to m_sortedDays
        if (reversed) sortedPop.Reverse();

        return sortedPop;
    }


    public void SortDayList(List<Day> days, bool reversed = false)
    {
        days.Sort((Day a, Day b) =>
        {
            float fA = GetFitness(a);
            float fB = GetFitness(b);

            if (fA < fB) return -1;
            if (fA == fB) return 0;
            return 1;
        });

        if (reversed) days.Reverse();
    }
}