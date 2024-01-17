using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// This script specifically handles menu changes - when this happens, the
/// current menu panel is deactivated, and the new one is activated.
/// </summary>
public partial class MenuHandler : MonoBehaviour
{
    [SerializeField]
    private GameObject m_menuPanel;
    [SerializeField]
    private GameObject m_algorithmsPanel;
    [SerializeField]
    private GameObject m_preferencesPanel;
    [SerializeField]
    private GameObject m_graphsPanel;

    [SerializeField]
    private GameObject m_backButton;

    // Allows access of the previous menus, used for making previous menus invisible
    // and for backtracking (up and to the main menu). Due to tree-like menu structure,
    // this stack will reliably always return to the main menu before allowing another
    // menu to be navigated.
    // Must always have the main menu as its bottom element.
    private Stack<GameObject> m_panelStack;


    private void Start()
    {
        m_panelStack = new Stack<GameObject>();
        m_panelStack.Push(m_menuPanel);

        // Load and cache the user's preferences into Preferences.Saved.
        Saving.LoadFromFile<Preferences>("Preferences.dat");
        // Visual update in Preferences menu.
        UpdateDietPreferenceButtons();
        UpdateBodyPreferenceButtons();

        // Add listeners
        m_weightGoalDropdown.onValueChanged.AddListener((int value) => OnWeightGoalChanged(value));
        m_weightInputField.onSubmit.AddListener((string value) => OnWeightInputChanged(value));
        m_heightInputField.onSubmit.AddListener((string value) => OnHeightInputChanged(value));
    }


    /// <summary>
    /// Listener function - called by buttons when they are clicked, along with a panel.
    /// 
    /// Changes the current menu panel to a different one. (Under the hood, this is pushed
    /// onto the stack, and the top element is the current menu.)
    /// </summary>
    /// <param name="newPanel">The new panel. Must NOT be the main menu.</param>
    public void PushNewPanel(GameObject newPanel)
    {
        // Set previous panel to inactive; set new panel to active.
        m_panelStack.Peek().SetActive(false);
        newPanel.SetActive(true);
        // Also enable the back button as we aren't on the main menu.
        m_backButton.SetActive(true);

        m_panelStack.Push(newPanel);
    }


    public void OnBackPressed()
    {
        // Can reliably pop the top element, as this won't be the main menu.
        // The back button is not visible on the main menu.
        m_panelStack.Pop().SetActive(false);
        m_panelStack.Peek().SetActive(true);

        // Back button must then disappear if this led to the main menu.
        if (m_panelStack.Count == 1)
        {
            m_backButton.SetActive(false);
        }
    }
}