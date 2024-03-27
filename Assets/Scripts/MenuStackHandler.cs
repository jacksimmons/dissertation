using System.Collections.Generic;
using TMPro;
using UnityEngine;


// Commented 26/3
/// <summary>
/// Unity script for handling menu changes - when this happens, the
/// current menu panel is deactivated, and the new one is activated.
/// </summary>
public class MenuStackHandler : MonoBehaviour
{
    /// <summary>
    /// The base menu of the whole program (Main Menu). Is always at the bottom of the
    /// menu stack system, and cannot be popped.
    /// </summary>
    [SerializeField]
    private GameObject m_menuPanel;

    /// <summary>
    /// A persistent button which, when pressed, pops the top menu from the stack (so
    /// the menu gets one layer closer to the main menu).
    /// 
    /// Is disabled when on the main menu and enabled otherwise, to prevent popping the main
    /// menu from the stack.
    /// </summary>
    [SerializeField]
    private GameObject m_backButton;

    /// <summary>
    /// A stack which controls the currently-viewed menu. As a user clicks on a new menu,
    /// it gets pushed to this stack, and becomes enabled, while the previous menu gets
    /// disabled.
    /// 
    /// If a user goes back, the menu gets popped, and the reverse happens.
    /// 
    /// Its bottom element is always the main menu.
    /// </summary>
    private Stack<GameObject> m_panelStack;

    /// <summary>
    /// A persistent popup panel which can be enabled to display certain errors or
    /// important messages to the user.
    /// </summary>
    [SerializeField]
    private GameObject m_popupPanel;


    private void Awake()
    {
        // Initialise the stack datastructure, with the main menu as its bottom element.
        m_panelStack = new Stack<GameObject>();
        m_panelStack.Push(m_menuPanel);

        // Load and cache the user's preferences into Preferences.Instance.
        Saving.LoadPreferences();
    }


    /// <summary>
    /// [Listener function]
    /// 
    /// Changes the current menu panel to a different one (pushes the new menu onto the
    /// menu stack).
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


    /// <summary>
    /// [Listener function]
    /// 
    /// Called by the back button. Pops the top panel from the stack, causing the UI to
    /// go (back) to the next menu down the stack.
    /// 
    /// Also disables the back button if this led back to the main menu.
    /// </summary>
    public void OnBackPressed()
    {
        // Can reliably pop the top element, as this won't be the main menu. This is
        // because the back button is not enabled on the main menu.
        m_panelStack.Pop().SetActive(false);
        m_panelStack.Peek().SetActive(true);

        // Back button must then disappear if this led to the main menu.
        if (m_panelStack.Count == 1)
        {
            m_backButton.SetActive(false);
        }
    }


    /// <summary>
    /// Enables the popup panel, with a certain message.
    /// </summary>
    /// <param name="title">The title of the popup.</param>
    /// <param name="message">The main message of the popup.</param>
    /// <param name="messageColour">The colour of the message.</param>
    public void ShowPopup(string title, string message, Color messageColour)
    {
        // Enable the popup
        m_popupPanel.SetActive(true);

        // Set the title and message texts directly
        m_popupPanel.transform.GetChild(0).GetComponent<TMP_Text>().text = title;
        TMP_Text messageText = m_popupPanel.transform.GetChild(1).GetComponent<TMP_Text>();
        messageText.text = message;
        messageText.color = messageColour;
    }


    /// <summary>
    /// [Listener function, called by the popup panel]
    /// 
    /// Disables the popup panel.
    /// </summary>
    public void HidePopup()
    {
        m_popupPanel.SetActive(false);
    }
}