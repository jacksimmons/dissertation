using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// Commented 26/3
/// <summary>
/// A Unity script for handling the Plot Viewing menu.
/// </summary>
public class PlotViewBehaviour : MonoBehaviour
{
    /// <summary>
    /// Label showing the filename of the plot on screen.
    /// </summary>
    [SerializeField]
    private TMP_Text m_plotNameTxt;

    /// <summary>
    /// A list of the file paths to each plot.
    /// </summary>
    private List<string> m_paths;

    /// <summary>
    /// GameObject of the transform which acts as parent to all plot GameObjects.
    /// </summary>
    [SerializeField]
    private GameObject m_plotContainer;

    /// <summary>
    /// Prefab for a plot GameObject.
    /// </summary>
    [SerializeField]
    private GameObject m_plotTemplate;

    /// <summary>
    /// A list of plot GameObjects (each visualises a plot image).
    /// </summary>
    private List<GameObject> m_plots;

    /// <summary>
    /// The current plot which is displayed on screen.
    /// </summary>
    private int m_activePlotIndex = 0;


    private void OnEnable()
    {
        // Reset plot index
        m_activePlotIndex = 0;
        string plotDirectory = Application.persistentDataPath + "/Plots/";

        // Ensure plots folder exists
        if (!Directory.Exists(plotDirectory)) Directory.CreateDirectory(plotDirectory);

        // Recreate all plot objects; delete the old ones
        UITools.DestroyAllChildren(m_plotContainer.transform);
        m_paths = Directory.GetFiles(plotDirectory).ToList();

        // Handle the case where there are no plots
        if (m_paths.Count == 0)
        {
            SetNoPlotsRemaining();
            return;
        }

        // Load all plots (make them invisible)
        m_plots = LoadPlots(m_paths);

        // Make the first plot (0) in the array visible
        m_plots[m_activePlotIndex].SetActive(true);
        m_plotNameTxt.text = Path.GetFileName(m_paths[m_activePlotIndex]);
    }

    
    /// <summary>
    /// Handles the UI changes necessary to reflect that no plots are left.
    /// </summary>
    private void SetNoPlotsRemaining() => m_plotNameTxt.text = "No plots remaining.";


    /// <summary>
    /// Converts a list of plot paths into a list of plot GameObjects.
    /// </summary>
    /// <param name="plotPaths">The plot paths.</param>
    /// <returns>The plot GameObjects.</returns>
    private List<GameObject> LoadPlots(List<string> plotPaths)
    {
        return plotPaths.Select(p => LoadPlot(p)).ToList();
    }


    /// <summary>
    /// Converts a single plot path into a single plot GameObject.
    /// </summary>
    /// <param name="path">The plot's path.</param>
    /// <returns>The instantiated plot GameObject.</returns>
    private GameObject LoadPlot(string path)
    {
        // All of this program's plots are output as .png; any others are not valid plots.
        string ext = Path.GetExtension(path);
        if (ext != ".png")
        {
            Logger.Warn($"The provided plot path is invalid (expected .png, got {ext}) [{path}].");
            return null;
        }

        // Instantiate (clone) the plot template prefab, and set it as a child of the plot container object.
        GameObject go = Instantiate(m_plotTemplate, m_plotContainer.transform);

        // Setup plot GameObject properties (plot image, and a button which deletes the plot when pressed)
        go.transform.Find("Image").GetComponent<Image>().sprite = Sprite.Create(PlotToImage(path), new(0, 0, 640, 480), new());
        go.transform.Find("DeleteBtn").GetComponent<Button>().onClick.AddListener(OnDeletePlotPressed);

        // Make the plot invisible until enabled
        go.SetActive(false);
        return go;
    }


    /// <summary>
    /// Loads a plot by path into a Unity texture.
    /// </summary>
    /// <param name="path">The plot path.</param>
    /// <returns>A Texture2D of the plot.</returns>
    private Texture2D PlotToImage(string path)
    {
        // Create the texture object. Note:
        // Dimensions are set when creating the sprite (see LoadPlot:go.transform.Find("Image") (...)), and don't matter here.
        Texture2D texture = new(1, 1);

        // Read the image as a binary file, and load it into the texture
        texture.LoadImage(File.ReadAllBytes(path));

        return texture;
    }


    /// <summary>
    /// Moves to the next plot in the list when called.
    /// </summary>
    /// <param name="right">Moving: `true` => right, `false` => left.</param>
    public void OnPlotNavBtnPressed(bool right)
    {
        if (m_paths.Count == 0) return;

        UITools.OnNavBtnPressed(right, m_plots.ToArray(), ref m_activePlotIndex);
        m_plotNameTxt.text = Path.GetFileName(m_paths[m_activePlotIndex]);
    }


    /// <summary>
    /// Deletes the current plot file from disk, and from the display.
    /// </summary>
    public void OnDeletePlotPressed()
    {
        int toBeDeleted = m_activePlotIndex;

        // Start with destroying the Unity object
        GameObject go = m_plots[toBeDeleted];
        m_plots.RemoveAt(toBeDeleted);
        Destroy(go);

        // Remove the persistent file
        string path = m_paths[toBeDeleted];
        m_paths.RemoveAt(toBeDeleted);
        File.Delete(path);

        // Go to the next plot, if there is one
        if (m_paths.Count > 0) OnPlotNavBtnPressed(true);
        // Otherwise, show on UI that no plots remain
        else                   SetNoPlotsRemaining();
    }
}
