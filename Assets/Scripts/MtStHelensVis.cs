/* LungchBoxVis.cs
 * CSCI 5609
 *
 */

using UnityEngine;
using IVLab.ABREngine;
using IVLab.Utilities;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;


public static class ExtensionMethods
{

    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return ((value - from1) / (to1 - from1)) * (to2 - from2) + from2;
    }

}


public class MtStHelensVis : MonoBehaviour
{
    ////////////////////////////////////////////////////////////////////////////////
    // Things you may need to access to complete the assignment

    // Constants for data files
    private string dataFilePath;
    public const string beforeFileName = "beforeGrid240x346.csv";
    public const string afterFileName = "afterGrid240x346.csv";
    public const string setPath = "set_1";
    float[][][] volume_1;
    Vector3Int dims_1 = new Vector3Int(664, 105, 664);
    Bounds b = new Bounds(Vector3.zero, new Vector3Int(664, 300, 664));

    // declare various vasassets
    private ColormapVisAsset divGO;

    // List to store all data impressions (per vis method) in
    private List<List<IDataImpression>> impressionsPerMethod = new List<List<IDataImpression>>();

    //
    ////////////////////////////////////////////////////////////////////////////////

    // Keep track of which vis mode we're currently displaying!
    // You shouldn't need to mess with these.
    private int currentVisMode = 1;
    bool isFirstFrame = true;

    // Method 1: Basic point visualization -- partially implemented for you!
    //
    // The general process is:
    //  1. Obtain a RawDataset by using the RawDatasetAdapter
    //  2. Import the raw dataset into ABR
    //  3. Create your data impression(s) and style them with size, color, texture, etc.
    //  4. Register the data impressions with the ABREngine
    //  5. Save the impressions into this script so we can easily turn them on/off
    void PrepareMethod1()
    {
        // Convert the point lists into ABR format
        RawDataset abrVolume = RawDatasetAdapter.VoxelsToVolume(volume_1, "firstData", dims_1, b);

        // Import the point data into ABR
        KeyData vol = ABREngine.Instance.Data.ImportRawDataset(abrVolume);

        // Create data impression
        SimpleVolumeDataImpression gi = new SimpleVolumeDataImpression();
        gi.keyData = vol;
        //Color white = new Color(1.0f, 1.0f, 1.0f);
        //gi.colormap = ColormapVisAsset.SolidColor(white);
        gi.colorVariable = vol.GetScalarVariables()[0];
        gi.colormap = divGO;
        //gi.opacitymap = vol.GetScalarVariables()[0];
        gi.opacitymap = PrimitiveGradient.Default();

        // Register impressions with the engine
        ABREngine.Instance.RegisterDataImpression(gi);

        // Add a reference to these impressions so we can easily turn them on/off later
        impressionsPerMethod.Add(new List<IDataImpression> {gi});
    }

    // Start is run when you press 'Play' in Unity - this is similar to
    // 'setup()' in Processing.
    void Start()
    {
        // Wait for ABREngine to initialize
        while (!ABREngine.Instance.IsInitialized);

        // Set the data file path to StreamingAssets
        dataFilePath = Application.streamingAssetsPath;

        // Start by loading in the data from CSV files.
        LoadData();

        // Prepare each method here
        PrepareMethod1();
    }

    // Update is run each frame -- this is similar to Processing's 'draw()' function
    void Update()
    {
        // Set every layer to invisible if the user has pressed any key (need to
        // re-render the visualization with new layers)
        int oldVisMode = currentVisMode;
        // do stuff, change modes
        bool modeChanged = oldVisMode != currentVisMode;
        if (modeChanged || isFirstFrame)
        {
            foreach (var impression in ABREngine.Instance.GetAllDataImpressions())
            {
                impression.RenderHints.Visible = false;
            }
            Debug.Log("Showing comparison mode " + currentVisMode);
        }

        // Turn on the impressions associated with this mode
        if (currentVisMode - 1 < impressionsPerMethod.Count)
        {
            foreach (var impression in impressionsPerMethod[currentVisMode - 1])
            {
                impression.RenderHints.Visible = true;
            }
        }

        // Re-render the visualization if user pressed a key
        if (modeChanged || isFirstFrame)
        {
            ABREngine.Instance.Render();
            isFirstFrame = false;
        }
    }

    // Load the data from CSV files into point lists for you to access, and load into ABR data format.
    void LoadData()
    {
        // Load in the raw coordinates from CSV (convert from right-hand z-up to unity's left-hand y-up)
        volume_1 = loadCTcsv(Path.Combine(dataFilePath, setPath), dims_1);

        // load some colors
        divGO = ABREngine.Instance.VisAssets.LoadVisAsset<ColormapVisAsset>(new Guid("2fe08fbe-6f5e-4c11-a1df-ccce0c04cfb9"));
    }

    /// <summary>
    /// Import a series of CSV files into a 3d int array. 
    /// Normalizes all values from 0 to 1
    /// </summary>
    float[][][] loadCTcsv(string csvDirPath, Vector3 dims)
    {
        // create uniform jagged array
        float[][][] volume = new float[(int)(dims[0])][][];
        for (int i = 0; i < dims[0]; i++)
        {
            volume[i] = new float[(int)(dims[1])][];
            for (int j = 0; j < dims[1]; j++)
            {
                volume[i][j] = new float[(int)(dims[2])];
            }
        }
        // fill the array with data
        Debug.Log("Loading set " + csvDirPath);
        DirectoryInfo place = new DirectoryInfo(csvDirPath);
        FileInfo[] Files = place.GetFiles();
        FileInfo file;
        for (int f=0; f<dims[1]; f++)
        {
            file = Files[f];
            if (file.Name.EndsWith("meta"))
            {
                continue;
            }
            using (StreamReader reader = new StreamReader(Path.Combine(csvDirPath, file.Name)))
            {
                string line = reader.ReadLine();
                for (int i=0; i<dims[0]; i++)
                {
                    string[] contents = line.Trim().Split(',');
                    for (int j=0; j<dims[2]; j++)
                    {
                        volume[i][f][j] = float.Parse(contents[j]);
                    }
                    line = reader.ReadLine();
                }
            }
        }
        // normalize the data in the array
        Debug.Log("Normalizing set " + csvDirPath);
        float maxVal = float.MinValue;
        float minVal = float.MaxValue;
        for (int i=0; i<dims[0]; i++)
        {
            for (int j=0; j<dims[1]; j++)
            {
                for (int k=0; k<dims[2]; k++)
                {
                    if (volume[i][j][k] < minVal)
                    {
                        minVal = volume[i][j][k];
                    }
                    if (volume[i][j][k] > maxVal)
                    {
                        maxVal = volume[i][j][k];
                    }
                }
            }
        }
        for (int i = 0; i < dims[0]; i++)
        {
            for (int j = 0; j < dims[1]; j++)
            {
                for (int k = 0; k < dims[2]; k++)
                {
                    volume[i][j][k] = volume[i][j][k].Remap(minVal, maxVal, 0, 1);
                }
            }
        }
        return volume;
    }
}
