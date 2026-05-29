using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class HierarchyExporter
{
    [MenuItem("Tools/Export Hierarchy To Text")]
    public static void ExportHierarchy()
    {
        StringBuilder sb = new StringBuilder();

        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager
            .GetActiveScene()
            .GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            WriteObject(obj.transform, sb, 0);
        }

        string path = Path.Combine(Application.dataPath, "HierarchyExport.txt");
        File.WriteAllText(path, sb.ToString());

        AssetDatabase.Refresh();

        Debug.Log("Hierarchy exported to: " + path);
    }

    private static void WriteObject(Transform transform, StringBuilder sb, int depth)
    {
        sb.AppendLine(new string(' ', depth * 4) + transform.name);

        foreach (Transform child in transform)
        {
            WriteObject(child, sb, depth + 1);
        }
    }
}