using UnityEngine;
using UnityEditor;
using System.IO;
using System.Net;
using System;
using System.Linq;
using System.Text;
using CSvDataImporter.Editor;

public class CsvDataImporter
{
    private const string PREF_IS_PROCESSING = "CsvImporter_IsProcessing";
    private const string PREF_SO_NAME = "CsvImporter_SOName";
    private const string PREF_OUTPUT_FOLDER = "CsvImporter_OutputFolder";
    private const string PREF_CSV_CONTENT = "CsvImporter_CsvContent";

    public static void StartProcess(CsvDataImporterWindow view)
    {
        string csvData = DownloadCsvData(view.SheetUrl);
        if (string.IsNullOrEmpty(csvData)) return;

        EditorPrefs.SetBool(PREF_IS_PROCESSING, true);
        EditorPrefs.SetString(PREF_SO_NAME, view.SOName);
        EditorPrefs.SetString(PREF_OUTPUT_FOLDER, view.OutputFolder);
        EditorPrefs.SetString(PREF_CSV_CONTENT, csvData);

        CreateFolder(view.OutputFolder);

        GenerateClassScript(view.OutputFolder, view.SOName, csvData);

        AssetDatabase.Refresh();
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        if (!EditorPrefs.GetBool(PREF_IS_PROCESSING, false)) return;

        EditorPrefs.SetBool(PREF_IS_PROCESSING, false);

        string soName = EditorPrefs.GetString(PREF_SO_NAME);
        string outputFolder = EditorPrefs.GetString(PREF_OUTPUT_FOLDER);
        string csvData = EditorPrefs.GetString(PREF_CSV_CONTENT);

        GenerateScriptableObjects(outputFolder, soName, csvData);

        EditorPrefs.DeleteKey(PREF_CSV_CONTENT);
    }

    private static string DownloadCsvData(string url)
    {
        try
        {
            if (url.Contains("docs.google.com/spreadsheets"))
            {
                var uri = new Uri(url);

                var segments = uri.AbsolutePath.Split('/');
                var sheetId  = segments[segments.ToList().IndexOf("d") + 1];

                var gid = "0";
                if (uri.Fragment.StartsWith("#gid="))
                    gid = uri.Fragment.Replace("#gid=", "");

                url = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={gid}";
            }

            Debug.Log("Downloading from URL: " + url);

            using (var client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;
                return client.DownloadString(url);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error down csv file: {e.Message}\nURL: {url}");
            return null;
        }
    }

    private static void CreateFolder(string folderAddress)
    {
        if (string.IsNullOrEmpty(folderAddress)) return;

        var parts = folderAddress.Split('/');
        var current = "Assets";

        if (parts[0] == "Assets")
        {
            current = parts[0];
        }

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part) || part == "Assets") continue;

            string fullPath = current + "/" + part;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(current, part);
            }
            current = fullPath;
        }
    }

    private static void GenerateClassScript(string outputFolder, string className, string csvData)
    {
        var lines = csvData.Split('\n');
        if (lines.Length < 2) return;

        var types = lines[0].Split(',');
        var fields = lines[1].Split(',');

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("");
        sb.AppendLine($"[CreateAssetMenu(fileName = \"New{className}\", menuName = \"Generated Data/{className}\")]");
        sb.AppendLine($"public class {className} : ScriptableObject");
        sb.AppendLine("{");

        for (int i = 0; i < fields.Length; i++)
        {
            string type = types[i].Trim();
            string fieldName = fields[i].Trim();
            if(!string.IsNullOrEmpty(fieldName))
            {
                sb.AppendLine($" public {type} {fieldName};");
            }
        }

        sb.AppendLine("}");

        if (!outputFolder.StartsWith("Assets")) outputFolder = "Assets/" + outputFolder;

        string path = $"{outputFolder}/{className}.cs";
        File.WriteAllText(path, sb.ToString());
    }

    private static void GenerateScriptableObjects(string outputFolder, string soName, string csvData)
    {
        var lines = csvData.Split('\n');
        var types = lines[0].Split(',');
        var fields = lines[1].Split(',');

        if (!outputFolder.StartsWith("Assets")) outputFolder = "Assets/" + outputFolder;

        for (var i = 2; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var values = lines[i].Split(',');
            CreateSO(outputFolder, soName, fields, types, values, i - 1);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateSO(string outputFolder, string soName, string[] fields, string[] types, string[] values, int index)
    {
        Type type = Type.GetType(soName);

        if (type == null)
        {
            type = System.Reflection.Assembly.Load("Assembly-CSharp").GetType(soName);
        }

        if (type == null)
        {
            return;
        }

        var so = ScriptableObject.CreateInstance(type);

        for (var c = 0; c < fields.Length; c++)
        {
            if (c >= values.Length) break;

            var fieldName = fields[c].Trim();
            var typeName = types[c].Trim();
            var rawValue = values[c].Trim();

            var fieldInfo = type.GetField(fieldName);

            if (fieldInfo == null) continue;

            try
            {
                var convertedValue = ConvertValue(typeName, rawValue);
                fieldInfo.SetValue(so, convertedValue);
            }
            catch(Exception ex)
            {
                Debug.LogWarning($"Error parse {index}, colum {fieldName}: {ex.Message}");
            }
        }

        string fileName = $"{soName}_{index}";
        int idIndex = Array.IndexOf(fields, "id");
        if (idIndex != -1 && idIndex < values.Length) fileName = $"{soName}_{values[idIndex]}";

        var assetPath = $"{outputFolder}/{fileName}.asset";

        AssetDatabase.CreateAsset(so, assetPath);
    }

    private static object ConvertValue(string type, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (type == "string") return "";
            if (type == "int" || type == "float") return 0;
            if (type == "bool") return false;
        }

        return type.ToLower() switch
        {
            "int" => int.Parse(value),
            "float" => float.Parse(value),
            "string" => value,
            "bool" => bool.Parse(value),
            _ => value
        };
    }
}