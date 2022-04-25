/* LungchBoxVis.cs
 * CSCI 5609
 *
 */

using UnityEngine;
using UnityEngine.UI;
using IVLab.ABREngine;
using IVLab.Utilities;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;


public class MtStHelensVis : MonoBehaviour
{

    // Constants for data files
    private string dataFilePath;
    private static string[] setPaths = new string[] { "set_1", "set_2", "set_3", "set_4" };
    private float[][] grad_points = new float[][] { new float[] { 0.27f, 0.32f },
                                                    new float[] { 0.5f, 0.55f },
                                                    new float[] { 0.5f, 0.55f },
                                                    new float[] { 0.5f, 0.55f } };
    List<float[][][]> volumes = new List<float[][][]> { };
    List<Vector3Int> dims = new List<Vector3Int> { };
    List<Bounds> v_bounds = new List<Bounds> { };
    List<AnimationCurve> opacity_curves = new List<AnimationCurve> { };
    private int COLUMNS = 1;
    private int ROWS = 1;
    List<LineRenderer> c_divs = new List<LineRenderer> { };
    List<LineRenderer> r_divs = new List<LineRenderer> { };
    List<LineRenderer> b_divs = new List<LineRenderer> { };
    private float div_widths = 0.05f;
    private float box_widths = 0.002f;
    private Vector3[] box_coords = new Vector3[] { Vector3.zero, Vector3.zero };
    private bool clicked = false;
    private bool hardcode_add = false;
    private bool opacity_changed = true;

    // gradient
    private ColormapVisAsset divGO;

    // List to store all data impressions in
    private List<SimpleVolumeDataImpression> all_impressions = new List<SimpleVolumeDataImpression>();

    // list to store all groups in
    private List<DataImpressionGroup> all_groups = new List<DataImpressionGroup>();

    //slider
    public Slider slide = null;

    //
    ////////////////////////////////////////////////////////////////////////////////

    // Keep track of which vis mode we're currently displaying!
    // You shouldn't need to mess with these.
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
    void add_volume(int column, int row, int index)
    {
        if (column + 1 > COLUMNS)
        {
            for (int i = 0; i < column + 1 - COLUMNS; i++)
            {
                add_line(c_divs, div_widths, Color.white);
            }
            COLUMNS = column + 1;
        }
        if (row + 1 > ROWS)
        {
            for (int i = 0; i < row + 1 - ROWS; i++)
            {
                add_line(r_divs, div_widths, Color.white);
            }
            ROWS = row + 1;
        }
        // Convert the point lists into ABR format
        RawDataset abrVolume = RawDatasetAdapter.VoxelsToVolume(volumes[index], "data", dims[index], v_bounds[index]);

        // Import the point data into ABR
        KeyData vol = ABREngine.Instance.Data.ImportRawDataset(abrVolume);

        // Create data impressions
        SimpleVolumeDataImpression gi = new SimpleVolumeDataImpression();
        gi.keyData = vol;
        gi.colorVariable = vol.GetScalarVariables()[0];
        gi.colormap = divGO;
        string[] values = new string[] { "0%", "100%" };
        PrimitiveGradient grad = new PrimitiveGradient(Guid.NewGuid(), grad_points[index], values);
        gi.opacitymap = grad;

        // create animation curve
        AnimationCurve a_curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 100));

        // create groups
        string cell_name = "cell " + column.ToString() + " " + row.ToString();
        DataImpressionGroup group = ABREngine.Instance.CreateDataImpressionGroup(cell_name, new Vector3(0, 0, 0));

        // Register impressions with the engine
        ABREngine.Instance.RegisterDataImpression(gi, group, true);

        // Add a reference to these impressions so we can easily turn them on/off later
        all_impressions.Add(gi);
        all_groups.Add(group);

        opacity_changed = true;
    }

    void Start()
    {
        // Wait for ABREngine to initialize
        while (!ABREngine.Instance.IsInitialized);

        // Set the data file path to StreamingAssets
        dataFilePath = Application.streamingAssetsPath;

        // Start by loading in the data from CSV files.
        LoadData();

        Camera.main.backgroundColor = Color.black;


        add_volume(0, 0, 0);
        add_volume(1, 0, 1);
    }

    // Update is run each frame -- this is similar to Processing's 'draw()' function
    void Update()
    {

        // Turn on the impressions associated with this mode
        if (opacity_changed || slide != null)
        {
            for (int i = 0; i < all_impressions.Count; i++)
            {
                all_impressions[i].RenderHints.Visible = true;

                string[] values = new string[] { "0%", "100%" };
                if (slide != null)
                {
                    float point = slide.value;
                    if (i == 0)
                    {
                        point = point / 1.8f;
                    }
                    PrimitiveGradient grad = new PrimitiveGradient(Guid.NewGuid(), new float[] { point, point + 0.03f }, values);
                    all_impressions[i].opacitymap = grad;
                    all_impressions[i].RenderHints.StyleChanged = true;
                }
            }
            opacity_changed = false;
            isFirstFrame = true;  // first frame on which this line was called most recently, anyways
        } else
        {
            Slider [] idk = ABREngine.Instance.gameObject.GetComponentsInChildren<Slider>();
            if (idk.Length != 0)
            {
                slide = idk[0];
                slide.value = 0.5f;
            }
        }

        // make the groups rotate without moving their position relative to the camera
        GameObject cam = GameObject.Find("Camera Pivot");
        foreach (DataImpressionGroup group in all_groups)
        {
            // coords
            int[] coords = coords_from_name(group.Name);
            // position
            GameObject group_GO = ABREngine.Instance.GetEncodedGameObject(new List<Guid>(group.GetDataImpressions().Keys)[0]).gameObject;
            Vector3 position = new Vector3(2.0f * coords[0] - COLUMNS + 1, 2.5f * (-coords[1] + (ROWS - 1) / 2.0f), 0);
            group_GO.transform.SetPositionAndRotation(cam.transform.TransformVector(position), group_GO.transform.rotation);
        }
        for (int i = 0; i < c_divs.Count; i++)
        {
            Vector3 start_pos = new Vector3(2.0f * i - COLUMNS + 2f, 1.25f * ROWS, 0);
            Vector3 end_pos = new Vector3(2.0f * i - COLUMNS + 2f, -1.25f * ROWS, 0);
            c_divs[i].SetPosition(0, cam.transform.TransformVector(start_pos));
            c_divs[i].SetPosition(1, cam.transform.TransformVector(end_pos));
        }
        for (int i = 0; i < r_divs.Count; i++)
        {
            Vector3 start_pos = new Vector3(COLUMNS, - 2.5f * i, 0);
            Vector3 end_pos = new Vector3(-COLUMNS, - 2.5f * i, 0);
            r_divs[i].SetPosition(0, cam.transform.TransformVector(start_pos));
            r_divs[i].SetPosition(1, cam.transform.TransformVector(end_pos));
        }

        if (Input.GetKeyDown("b"))
        {
            box_coords[0] = Input.mousePosition;
            clicked = true;
        }


        if (clicked)
        {

            if (Input.GetKeyUp("b"))
            {
                if (!hardcode_add)
                {
                    hardcode_add = true;
                    isFirstFrame = true;  // first frame of the rest of the frames, anyways
                    add_volume(0, 1, 2);
                    add_volume(1, 1, 3);
                }
                box_coords[0] = Input.mousePosition;
                clicked = false;
            }
            box_coords[1] = Input.mousePosition;

            // position selection box
            Vector3 up_left = Camera.main.ScreenToWorldPoint(new Vector3(box_coords[0].x, box_coords[0].y, 1));
            Vector3 up_right = Camera.main.ScreenToWorldPoint(new Vector3(box_coords[1].x, box_coords[0].y, 1));
            Vector3 down_left = Camera.main.ScreenToWorldPoint(new Vector3(box_coords[0].x, box_coords[1].y, 1));
            Vector3 down_right = Camera.main.ScreenToWorldPoint(new Vector3(box_coords[1].x, box_coords[1].y, 1));
            b_divs[0].SetPosition(0, up_left);
            b_divs[0].SetPosition(1, up_right);
            b_divs[1].SetPosition(0, up_right);
            b_divs[1].SetPosition(1, down_right);
            b_divs[2].SetPosition(0, down_right);
            b_divs[2].SetPosition(1, down_left);
            b_divs[3].SetPosition(0, down_left);
            b_divs[3].SetPosition(1, up_left);
        }

        // Re-render the visualization
        if (isFirstFrame)
        {
            ABREngine.Instance.Render();
            isFirstFrame = false;
        }
    }

    // Load the data from CSV files into point lists for you to access, and load into ABR data format.
    void LoadData()
    {
        // Load in the raw coordinates from CSV (convert from right-hand z-up to unity's left-hand y-up)

        foreach (string path in setPaths)
        {
            dims.Add(getCTdims(Path.Combine(dataFilePath, path)));
            v_bounds.Add(new Bounds(Vector3.zero, dims.Last()));
            volumes.Add(loadCTcsv(Path.Combine(dataFilePath, path), dims.Last()));
            opacity_curves.Add(new AnimationCurve());
        }
        // prep box
        for (int i = 0; i < 4; i++)
        {
            add_line(b_divs, box_widths, Color.green);
        }

        divGO = make_grad(new float[] { 0.0f, 0.5f, 1.0f },
                          new Color[] { Color.red, Color.red, Color.red });
        divGO = ColormapVisAsset.SolidColor(Color.white);
    }


    ColormapVisAsset make_grad(float[] poss, Color[] colors)
    {
        int width = 100;
        Texture2D text = new Texture2D(width, 1);
        int cind = 1;
        for (int i=0; i<width; i++)
        {
            float spot = (float)i / (float)width;
            if (spot > poss[cind])
            {
                cind++;
            }
            float perc = (spot - poss[cind - 1]) / (poss[cind] - poss[cind - 1]);
            text.SetPixel(i, 0, Color.Lerp(colors[cind - 1], colors[cind], perc));
        }
        return new ColormapVisAsset(text);
    }

    void add_line(List<LineRenderer> lines, float width, Color color)
    {
        // set the color of the line
        SimpleVolumeDataImpression gi = new SimpleVolumeDataImpression();
        DataImpressionGroup group = ABREngine.Instance.CreateDataImpressionGroup("new name", new Vector3(0, 0, 0));
        ABREngine.Instance.RegisterDataImpression(gi, group, true);
        GameObject gObject = ABREngine.Instance.GetEncodedGameObject(new List<Guid>(group.GetDataImpressions().Keys)[0]).gameObject;
        LineRenderer line_r = gObject.AddComponent<LineRenderer>();
        line_r.sharedMaterial = new Material(Shader.Find("UI/Default"));
        line_r.startColor = color;
        line_r.endColor = color;

        // set width of the renderer
        line_r.startWidth = width;
        line_r.endWidth = width;

        // set the position
        line_r.SetPosition(0, Vector3.zero);
        line_r.SetPosition(1, Vector3.zero);

        lines.Add(line_r);

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