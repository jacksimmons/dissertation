using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopulationView : MonoBehaviour
{
    [SerializeField]
    private GameObject m_dayItem;
    [SerializeField]
    private GameObject m_popContent;

    private Day m_currentlyDisplayedDay;

    /// <summary>
    /// Buttons for selecting a portion to view details about.
    /// </summary>
    private int m_currentPortionIndex;
    [SerializeField]
    private Button m_prevPortionBtn;
    [SerializeField]
    private Button m_nextPortionBtn;

    /// </summary>
    /// Text fields displaying details about the selected day.
    /// <summary>
    [SerializeField]
    private TMP_Text m_dayText;

    /// </summary>
    /// Text fields displaying details about the selected portion.
    /// <summary>
    [SerializeField]
    private TMP_Text m_portionNumText;
    [SerializeField]
    private TMP_Text m_portionText;

    /// <summary>
    /// Text field displaying averaged details about the whole population.
    /// </summary>
    [SerializeField]
    private TMP_Text m_avgDayText;


    public void UpdatePopView()
    {
        for (int i = 1; i < m_popContent.transform.childCount; i++)
        {
            Destroy(m_popContent.transform.GetChild(i).gameObject);
        }


        // Display the best day
        Day bestDay = Algorithm.Instance.BestDay;
        if (bestDay != null)
        {
            Transform bestDayBtn = m_popContent.transform.GetChild(0);

            bestDayBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            bestDayBtn.GetComponent<Button>().onClick.AddListener(() => OnPopButtonPressed(bestDay));

            bestDayBtn.GetComponentInChildren<TMP_Text>().text = $"Best day (Fitness {Algorithm.Instance.BestFitness})";
        }


        // Display days in the population
        foreach (var kvp in Algorithm.Instance.Population)
        {
            GameObject obj = Instantiate(m_dayItem, m_popContent.transform);

            obj.transform.GetChild(0).GetComponent<TMP_Text>().text = AlgorithmOutput.GetDayLabel(kvp.Key);

            Button btn = obj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnPopButtonPressed(kvp.Key));
            obj.SetActive(true); // The template is always inactive, so need to explicitly make the copy active
        }


        // Refresh the current day UI if still in the population, or is the best day
        if (m_currentlyDisplayedDay != null && Algorithm.Instance.BestDay != null)
        {
            if (Algorithm.Instance.BestDay.IsEqualTo(m_currentlyDisplayedDay) || Algorithm.Instance.Population.ContainsKey(m_currentlyDisplayedDay))
            {
                UpdatePortionUI(m_currentlyDisplayedDay, 0);
                UpdateDayUI(m_currentlyDisplayedDay);
            }
            else
            {
                ClearPortionUI();
                ClearDayUI();
            }
        }
        

        // Set the average stats text
        m_avgDayText.text = Algorithm.Instance.GetAverageStatsLabel();
    }


    private void OnPopButtonPressed(Day day)
    {
        m_currentlyDisplayedDay = day;
        UpdateDayUI(day);

        if (day.portions.Count > 0)
        {
            m_currentPortionIndex = 0;
            UpdatePortionUI(day, 0);
        }

        // Activate the portion navigation buttons
        m_prevPortionBtn.gameObject.SetActive(true);
        m_nextPortionBtn.gameObject.SetActive(true);

        // Clear any existing listeners before the next step (e.g. from previous calls to this method).
        m_prevPortionBtn.onClick.RemoveAllListeners();
        m_nextPortionBtn.onClick.RemoveAllListeners();
        
        // Add a listener to each of the portion buttons, so that they navigate through the portions when
        // a day is selected.
        m_prevPortionBtn.onClick.AddListener(() => OnPortionNavBtnPressed(day, false));
        m_nextPortionBtn.onClick.AddListener(() => OnPortionNavBtnPressed(day, true));
    }


    /// <summary>
    /// Displays details for the currently selected day.
    /// </summary>
    private void UpdateDayUI(Day day)
    {
        m_dayText.text = day.Verbose();
    }


    /// <summary>
    /// Displays details for the portion at the provided index.
    /// </summary>
    /// <param name="portionIndex">The provided portion index.</param>
    private void UpdatePortionUI(Day day, int portionIndex)
    {
        if (portionIndex > day.portions.Count)
        {
            Logger.Log("Day has no portions (it was unexpectedly modified).", Severity.Warning);
            return;
        }

        m_portionNumText.text = $"Portion {portionIndex+1}/{day.portions.Count}";
        m_portionText.text = day.portions[portionIndex].Verbose();
    }


    private void OnPortionNavBtnPressed(Day day, bool right)
    {
        int nextIndex;
        if (right)
        {
            nextIndex = ++m_currentPortionIndex;
            if (nextIndex >= day.portions.Count)
                nextIndex = m_currentPortionIndex = 0;
            UpdatePortionUI(day, nextIndex);
            return;
        }

        nextIndex = --m_currentPortionIndex;
        if (nextIndex < 0)
            nextIndex = m_currentPortionIndex = day.portions.Count - 1;
        UpdatePortionUI(day, nextIndex);
    }


    private void ClearPortionUI()
    {
        // Reset associated variables
        m_currentPortionIndex = 0;

        // Deactivate the portion navigation buttons
        m_prevPortionBtn.gameObject.SetActive(false);
        m_nextPortionBtn.gameObject.SetActive(false);

        // Make the portion details text fields invisible
        m_portionNumText.text = "";
        m_portionText.text = "";
    }


    private void ClearDayUI()
    {
        m_dayText.text = "";
    }
}
