namespace  CSvDataImporter.Editor
{
    using UnityEditor;
    using UnityEngine;

    public static class CsvGenerateWindows
    {
        [MenuItem("Tools/Csv Data Importer")]
        public static void OpenTool()
        {
            EditorWindow.GetWindow<CsvDataImporterWindow>("CSV Data Importer");
        }
    }

    public class CsvDataImporterWindow : EditorWindow
    {
        public string SheetUrl      = "";
        public string OutputFolder  = "DataSO";
        public string SOName        = "";

        private void OnGUI()
        {
            GUILayout.Label("CSV Config", EditorStyles.centeredGreyMiniLabel);

            this.OutputFolderSelect();
            this.FillTheSheetUrl();
            this.FillTheSOName();
            this.ButtonGenerate();
            // this.ButtonRefresh();
        }

        private void OutputFolderSelect()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output Folder:", GUILayout.Width(150));
            this.OutputFolder = EditorGUILayout.TextField(this.OutputFolder);
            EditorGUILayout.EndHorizontal();
        }

        private void FillTheSheetUrl()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("CSV File Path:", GUILayout.Width(150));
            this.SheetUrl = EditorGUILayout.TextField(this.SheetUrl);
            EditorGUILayout.EndHorizontal();
        }

        private void FillTheSOName()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scriptable Object Name:", GUILayout.Width(150));
            this.SOName = EditorGUILayout.TextField(this.SOName);
            EditorGUILayout.EndHorizontal();
        }

        private void ButtonGenerate()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Generate", GUILayout.Height(30), GUILayout.Width(150)))
            {
                 if(this.SheetUrl == "")
                 {
                     Debug.LogError("Sheet URL is empty!");
                     return;
                 }
                 CsvDataImporter.StartProcess(this);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void ButtonRefresh()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh",GUILayout.Height(30), GUILayout.Width(150)))
            {
                Debug.Log("Refreshing CSV Data");
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}