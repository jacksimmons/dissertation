using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class PlotViewBehaviour : MonoBehaviour
{
    private List<string> m_paths;
    private List<GameObject> m_plots;
    private int m_activePlotIndex = 0;

    [SerializeField]
    private TMP_Text m_plotNameTxt;
    [SerializeField]
    private GameObject m_plotContainer;
    [SerializeField]
    private GameObject m_plotTemplate;


    // Reload every time this panel is enabled.
    private void OnEnable()
    {
        string plotDirectory = Application.persistentDataPath + "/Plots/";

        // Ensure plots folder exists
        if (!Directory.Exists(plotDirectory)) Directory.CreateDirectory(plotDirectory);

        // Recreate all plot objects; delete the old ones
        UITools.DestroyAllChildren(m_plotContainer.transform);
        m_paths = Directory.GetFiles(plotDirectory).ToList();

        if (m_paths.Count == 0)
        {
            SetNoPlotsRemaining();
            return;
        }

        // Note: Makes the first plot in the array visible
        m_plots = LoadPlots(m_paths);
        m_plotNameTxt.text = Path.GetFileName(m_paths[m_activePlotIndex]);
    }

    
    private void SetNoPlotsRemaining() => m_plotNameTxt.text = "No plots remaining.";


    private List<GameObject> LoadPlots(List<string> plotPaths)
    {
        return plotPaths.Select(p => LoadPlot(p)).ToList();
    }


    private GameObject LoadPlot(string path)
    {
        // All of this program's plots are output as .png; any others are not valid plots.
        if (Path.GetExtension(path) != ".png")
        {
            Logger.Warn($"Invalid plot {path} was in the /plots/ user folder.");
            return null;
        }

        GameObject go = Instantiate(m_plotTemplate, m_plotContainer.transform);
        go.transform.Find("Image").GetComponent<Image>().sprite = Sprite.Create(PlotToImage(path), new(0, 0, 640, 480), new());
        go.transform.Find("DeleteBtn").GetComponent<Button>().onClick.AddListener(OnDeletePlotPressed);

        return go;
    }


    private Texture2D PlotToImage(string path)
    {
        Texture2D texture = new(1, 1);
        texture.LoadImage(File.ReadAllBytes(path));
        return texture;
    }


    public void OnPlotNavBtnPressed(bool right)
    {
        if (m_paths.Count == 0) return;

        UITools.OnNavBtnPressed(right, m_plots.ToArray(), ref m_activePlotIndex);
        m_plotNameTxt.text = Path.GetFileName(m_paths[m_activePlotIndex]);
    }


    public void OnDeletePlotPressed()
    {
        int toBeDeleted = m_activePlotIndex;

        // Go to the next plot (if there is one)
        SetNoPlotsRemaining(); // Default text which is overwritten if there are plots remaining.
        OnPlotNavBtnPressed(true);

        // Start with destroying the Unity object
        GameObject go = m_plots[toBeDeleted];
        m_plots.RemoveAt(toBeDeleted);
        Destroy(go);

        // Remove the persistent file
        string path = m_paths[toBeDeleted];
        m_paths.RemoveAt(toBeDeleted);
        File.Delete(path);
    }
}
