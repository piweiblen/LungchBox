using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor;

/// <summary>
/// Builds a tarball of each custom dependency specified in `packageNamesToBuild`.
/// </summary>
[ExecuteInEditMode]
public class Builder : MonoBehaviour
{
    public bool build;

    private string buildFolder;
    public List<string> packageNamesToBuild = new List<string>();

    // // Update is called once per frame
    void Update()
    {
        if (build)
        {
            buildFolder = Path.Combine(Application.dataPath, "..", "Packages");
            List();
            build = false;
        }
    }

    static ListRequest Request;

    void List()
    {
        Request = Client.List();    // List packages installed for the project
        EditorApplication.update += Progress;
    }

    void Progress()
    {
        if (Request.IsCompleted)
        {
            if (Request.Status == StatusCode.Success)
            {
                int n = 0;
                foreach (var package in Request.Result)
                {
                    if (packageNamesToBuild.Contains(package.name))
                    {
                        Client.Pack(package.resolvedPath, buildFolder);
                        Debug.Log("Built package: " + package.name);
                        n += 1;
                    }
                }
                Debug.Log($"Built {n} packages to {buildFolder}");
            }
            else if (Request.Status >= StatusCode.Failure)
                Debug.Log(Request.Error.message);

            EditorApplication.update -= Progress;
        }
    }
}
