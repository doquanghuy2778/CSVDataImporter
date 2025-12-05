namespace CSvDataImporter.Editor
{
    using System.Net;
    using UnityEditor;
    using UnityEngine;

    public static class CSVImportManger
    {
        public static string CsvData;

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
                var    parts         = folderAddress.Split('/');
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
    }
}