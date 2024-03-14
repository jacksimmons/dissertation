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

    public Dictionary<Day, float>.KeyCollection Days => m_dayFitnesses.Keys;


    public Population()
    {
        m_dayFitnesses = new();
        m_dayFitnessUpToDate = new();
    }


    public void Add(Day day)
    {
        m_dayFitnesses[day] = -1;
        m_dayFitnessUpToDate[day] = false;
        m_avgFitnessUpToDate = false;
    }


    public void Remove(Day day)
    {
        m_dayFitnesses.Remove(day);
        m_dayFitnessUpToDate.Remove(day);
        m_avgFitnessUpToDate = false;
    }


    public void FlagAsOutdated(Day day)
    {
        m_dayFitnessUpToDate[day] = false;
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
}