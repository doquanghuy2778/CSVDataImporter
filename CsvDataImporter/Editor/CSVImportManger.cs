namespace CSvDataImporter.Editor
{
    using System.Net;
    using UnityEditor;
    using UnityEngine;

    public static class CSVImportManger
    {
        public static string CsvData;

        public static void ProcessData(CsvDataImporterWindow view)
        {
            DownloadCsvData(view);
            CreateFolder(view);
            // GenerateScriptableObjects(view);
        }

        public static void DownloadCsvData(CsvDataImporterWindow view)
        {
            var url = view.SheetUrl;
            CsvData = new WebClient().DownloadString(url);
            Debug.Log("CSV Data Downloaded");
        }

        public static void CreateFolder(CsvDataImporterWindow view)
        {
            if (view.OutputFolder == "")
            {
                Debug.LogError("Please enter a folder name");
            }
            else
            {
                var folderAddress = view.OutputFolder;
                var parts         = folderAddress.Split('/');
                var current       = "";

                for (var i = 0; i < parts.Length; i++)
                {
                    if (string.IsNullOrEmpty(parts[i])) continue;

                    var parent = current == "" ? "Assets" : current;
                    var folder = parts[i];

                    var full = $"{parent}/{folder}";
                    if (!AssetDatabase.IsValidFolder(full))
                    {
                        AssetDatabase.CreateFolder(parent, folder);
                    }

                    current = full;
                }
            }
        }

        private static void CreateSO(string outputFolder, string soName, string[] fields, string[] types, string[] values, int index)
        {
            ScriptableObject so = ScriptableObject.CreateInstance(soName);
            for (int c = 0; c < fields.Length; c++)
            {
                string fieldName = fields[c].Trim();
                string typeName  = types[c].Trim();
                string rawValue  = values[c].Trim();

                var field = so.GetType().GetField(fieldName);

                if (field == null)
                {
                    Debug.LogError($"Field '{fieldName}' not found in SO type '{soName}'.");
                    continue;
                }

                object convertedValue = ConvertValue(typeName, rawValue);
                field.SetValue(so, convertedValue);
            }

            string assetPath = $"{outputFolder}/{soName}_{index}.asset";
            AssetDatabase.CreateAsset(so, assetPath);
            Debug.Log("Created SO: " + assetPath);
        }

        private static object ConvertValue(string type, string value)
        {
            return type switch
            {
                "int"    => int.Parse(value),
                "float"  => float.Parse(value),
                "string" => value,
                "bool"   => bool.Parse(value),
                _        => value
            };
        }
    }
}