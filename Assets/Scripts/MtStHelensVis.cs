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


public class MtStHelensVis : MonoBehaviour
{
    ////////////////////////////////////////////////////////////////////////////////
    // Things you may need to access to complete the assignment

    // Constants for data files
    private string dataFilePath;
    public const string setPath = "set_1";
    float[][][] volume_1;
    Vector3Int dims_1;
    Bounds b;
    private int COLUMNS = 4;
    private int ROWS = 1;

    // declare various vasassets
    private ColormapVisAsset divGO;

    // List to store all data impressions in
    private List<IDataImpression> all_impressions = new List<IDataImpression>();

    // list to store all groups in
    private List<DataImpressionGroup> all_groups = new List<DataImpressionGroup>();

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

    // Start is run when you press 'Play' in Unity - this is similar to
    // 'setup()' in Processing.
    void add_volume(int column, int row, string name)
    {
        // Convert the point lists into ABR format
        RawDataset abrVolume = RawDatasetAdapter.VoxelsToVolume(volume_1, "firstData", dims_1, b);

        // Import the point data into ABR
        KeyData vol = ABREngine.Instance.Data.ImportRawDataset(abrVolume);

        // Create data impressions
        SimpleVolumeDataImpression gi = new SimpleVolumeDataImpression();
        gi.keyData = vol;
        //Color white = new Color(1.0f, 1.0f, 1.0f);
        //gi.colormap = ColormapVisAsset.SolidColor(white);
        gi.colorVariable = vol.GetScalarVariables()[0];
        gi.colormap = divGO;
        float[] points = new float[] { 0.32f, 0.37f };
        string[] values = new string[] { "0%", "100%" };
        PrimitiveGradient grad = new PrimitiveGradient(Guid.NewGuid(), points, values);
        gi.opacitymap = grad;

        // create groups
        string cell_name = "cell " + column.ToString() + " " + row.ToString();
        DataImpressionGroup group = ABREngine.Instance.CreateDataImpressionGroup(cell_name, new Vector3(0, 0, 0));

        // Register impressions with the engine
        ABREngine.Instance.RegisterDataImpression(gi, group, true);

        // Add a reference to these impressions so we can easily turn them on/off later
        all_impressions.Add(gi);
        all_groups.Add(group);

    }

    void Start()
    {
        // Wait for ABREngine to initialize
        while (!ABREngine.Instance.IsInitialized);

        // Set the data file path to StreamingAssets
        dataFilePath = Application.streamingAssetsPath;

        // Start by loading in the data from CSV files.
        LoadData();

        for (int c = 0; c < COLUMNS; c++)
        {
            add_volume(c, 0, "uh");
        }
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
        foreach (var impression in all_impressions)
        {
            impression.RenderHints.Visible = true;
        }

        // make the groups rotate without moving their position relative to the camera
        GameObject cam = GameObject.Find("Camera Pivot");
        foreach (DataImpressionGroup group in all_groups)
        {
            int[] coords = coords_from_name(group.Name);
            GameObject group_GO = ABREngine.Instance.GetEncodedGameObject(new List<Guid>(group.GetDataImpressions().Keys)[0]).gameObject;
            Vector3 position = new Vector3(2.0f * coords[0] - COLUMNS + 1, 2.5f * (coords[1] - (ROWS - 1) / 2), 0);
            group_GO.transform.SetPositionAndRotation(cam.transform.TransformVector(position), group_GO.transform.rotation);
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
        dims_1 = getCTdims(Path.Combine(dataFilePath, setPath));
        b = new Bounds(Vector3.zero, dims_1);
        volume_1 = loadCTcsv(Path.Combine(dataFilePath, setPath), dims_1);


        // load some colors
        divGO = ABREngine.Instance.VisAssets.LoadVisAsset<ColormapVisAsset>(new Guid("3c8e4f57-427e-4408-b26b-21f3c1619c62"));
    }

    Vector3Int getCTdims(string csvDirPath)
    {
        Vector3Int dims = new Vector3Int(0, 0, 0);
        DirectoryInfo place = new DirectoryInfo(csvDirPath);
        FileInfo[] Files = place.GetFiles();
        FileInfo file;
        for (int f = 0; f < Files.Length; f++)
        {
            file = Files[f];
            if (file.Name.EndsWith("csv"))
            {
                dims[1]++;
                if (dims[0] != 0 & dims[2] != 0)
                {
                    continue;
                }
            }
            else
            {
                continue;
            }
            dims[0] = 0;
            dims[2] = 0;
            using (StreamReader reader = new StreamReader(Path.Combine(csvDirPath, file.Name)))
            {
                string line = reader.ReadLine();
                dims[2] = line.Trim().Split(',').Length;
                while (line != null)
                {
                    dims[0]++;
                    line = reader.ReadLine();
                }
            }
        }
        return dims;
    }

    /// <summary>
    /// Import a series of CSV files into a 3d int array. 
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
        int k = 0;
        for (int f=0; f<Files.Length; f++)
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
                        volume[i][k][j] = float.Parse(contents[j]);
                    }
                    line = reader.ReadLine();
                }
            }
            k++;
        }
        return volume;
    }

    int[] coords_from_name(string name)
    {
        string[] nums = name.Split(' ');
        return new int[] { int.Parse(nums[1]), int.Parse(nums[2]) };
    }
}
