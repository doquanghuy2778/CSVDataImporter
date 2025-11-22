namespace CSvDataImporter.Editor
{
    using System.Net;
    using UnityEngine;

    public static class CSVImportManger
    {
        public static string CsvData;

        public static void DownloadCsvData(CsvDataImporterWindow view)
        {
            string url = view.SheetUrl;
            CsvData = new WebClient().DownloadString(url);
            Debug.Log("CSV Data Downloaded");
        }
    }
}