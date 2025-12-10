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
            GenerateScriptableObjects(view);
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
            var so = ScriptableObject.CreateInstance(soName);
            for (var c = 0; c < fields.Length; c++)
            {
                var fieldName = fields[c].Trim();
                var typeName  = types[c].Trim();
                var rawValue  = values[c].Trim();

                var field = so.GetType().GetField(fieldName);

                if (field == null)
                {
                    Debug.LogError($"Field '{fieldName}' not found in SO type '{soName}'.");
                    continue;
                }

                var convertedValue = ConvertValue(typeName, rawValue);
                field.SetValue(so, convertedValue);
            }

            var assetPath = $"{outputFolder}/{soName}_{index}.asset";
            AssetDatabase.CreateAsset(so, assetPath);
            Debug.Log("Created SO: " + assetPath);
        }

        public static void GenerateScriptableObjects(CsvDataImporterWindow view)
        {
            if (string.IsNullOrEmpty(CsvData))
            {
                Debug.LogError("CSVData is empty. Did you download it?");
                return;
            }

            var lines = CsvData.Split('\n');

            if (lines.Length < 3)
            {
                Debug.LogError("CSV must contain at least 3 rows.");
                return;
            }

            var types  = lines[0].Split(',');
            var fields = lines[1].Split(',');

            var outputFolder = view.OutputFolder.TrimEnd('/');
            var soName       = view.SOName;

            for (var i = 2; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var values = lines[i].Split(',');
                CreateSO(outputFolder, soName, fields, types, values, i - 1);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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