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

    private Algorithm m_currentAlg;
    private ReadOnlyDictionary<Day, Fitness> m_currentPop;

    /// <summary>
    /// Buttons for selecting a portion to view details about.
    /// </summary>
    private int m_currentPortionIndex;
    [SerializeField]
    private Button m_prevPortionBtn;
    [SerializeField]
    private Button m_nextPortionBtn;

    /// </summary>
    /// Text fields displaying details about the selected portion.
    /// <summary>
    [SerializeField]
    private TMP_Text m_portionNumText;
    [SerializeField]
    private TMP_Text m_portionText;


    public void UpdatePopView(Algorithm algorithm)
    {
        foreach (Transform child in m_popContent.transform)
        {
            Destroy(child.gameObject);
        }

        m_currentAlg = algorithm;
        m_currentPop = algorithm.GetPopulation();
        foreach (Day day in m_currentPop.Keys)
        {
            GameObject obj = Instantiate(m_dayItem, m_popContent.transform);
            obj.transform.GetChild(0).GetComponent<TMP_Text>().text = $"Portions: {day.Portions.Count}, Fitness: {m_currentPop[day].Value}";
            obj.GetComponent<Button>().onClick.AddListener(() => OnPopButtonPressed(day));
            obj.SetActive(true); // The template is always inactive, so need to explicitly make the copy active
        }
    }


    private void OnPopButtonPressed(Day day)
    {
        if (day.Portions.Count > 0)
        {
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
    /// Displays details for the portion at the provided index.
    /// </summary>
    /// <param name="portionIndex"></param>
    private void UpdatePortionUI(Day day, int portionIndex)
    {
        if (portionIndex > day.Portions.Count)
        {
            Debug.LogWarning("Day has no portions (it was unexpectedly modified).");
            return;
        }

        m_portionNumText.text = $"Portion {portionIndex+1}/{day.Portions.Count}";
        m_portionText.text = m_currentAlg.PortionToString(day.Portions[portionIndex]);
    }


    public void OnPortionNavBtnPressed(Day day, bool right)
    {
        int nextIndex;
        if (right)
        {
            nextIndex = ++m_currentPortionIndex;
            if (nextIndex >= day.Portions.Count)
                nextIndex = m_currentPortionIndex = 0;
            UpdatePortionUI(day, nextIndex);
            return;
        }

        nextIndex = --m_currentPortionIndex;
        if (nextIndex < 0)
            nextIndex = m_currentPortionIndex = day.Portions.Count - 1;
        UpdatePortionUI(day, nextIndex);
    }


    public void ClearPortionUI()
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
}
