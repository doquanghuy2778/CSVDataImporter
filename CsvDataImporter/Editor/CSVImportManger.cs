using UnityEngine;
using UnityEditor;
using System.IO;
using System.Net;
using System;
using System.Text;
using CSvDataImporter.Editor;

public class CsvDataImporter
{
    // Các key để lưu trạng thái vào EditorPrefs giữa các lần biên dịch
    private const string PREF_IS_PROCESSING = "CsvImporter_IsProcessing";
    private const string PREF_SO_NAME = "CsvImporter_SOName";
    private const string PREF_OUTPUT_FOLDER = "CsvImporter_OutputFolder";
    private const string PREF_CSV_CONTENT = "CsvImporter_CsvContent"; // Lưu tạm nội dung CSV

    // Hàm chính được gọi từ Button trong Window của bạn
    public static void StartProcess(CsvDataImporterWindow view)
    {
        // 1. Tải dữ liệu
        string csvData = DownloadCsvData(view.SheetUrl);
        if (string.IsNullOrEmpty(csvData)) return;

        // 2. Lưu các thông số cần thiết vào EditorPrefs để dùng lại sau khi Unity Reload
        EditorPrefs.SetBool(PREF_IS_PROCESSING, true);
        EditorPrefs.SetString(PREF_SO_NAME, view.SOName);
        EditorPrefs.SetString(PREF_OUTPUT_FOLDER, view.OutputFolder);
        EditorPrefs.SetString(PREF_CSV_CONTENT, csvData); // Lưu nội dung CSV

        // 3. Tạo thư mục
        CreateFolder(view.OutputFolder);

        // 4. Tạo file Class C#
        GenerateClassScript(view.OutputFolder, view.SOName, csvData);

        // 5. Bắt buộc Unity biên dịch lại
        Debug.Log("Đang tạo Class và biên dịch lại... Vui lòng đợi.");
        AssetDatabase.Refresh();
    }

    // Hàm này sẽ TỰ ĐỘNG chạy sau khi Unity biên dịch xong
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        // Kiểm tra xem có phải đang trong quá trình xử lý của tool này không
        if (!EditorPrefs.GetBool(PREF_IS_PROCESSING, false)) return;

        // Reset cờ để không chạy lại lần sau
        EditorPrefs.SetBool(PREF_IS_PROCESSING, false);

        // Lấy lại dữ liệu đã lưu
        string soName = EditorPrefs.GetString(PREF_SO_NAME);
        string outputFolder = EditorPrefs.GetString(PREF_OUTPUT_FOLDER);
        string csvData = EditorPrefs.GetString(PREF_CSV_CONTENT);

        Debug.Log("Biên dịch xong! Bắt đầu tạo Scriptable Objects...");
        GenerateScriptableObjects(outputFolder, soName, csvData);

        // Dọn dẹp bộ nhớ tạm (tuỳ chọn)
        EditorPrefs.DeleteKey(PREF_CSV_CONTENT);
    }

    // -----------------------------------------------------------------------
    // PHẦN LOGIC CHI TIẾT
    // -----------------------------------------------------------------------

    private static string DownloadCsvData(string url)
    {
        // --- XỬ LÝ LINK GOOGLE SHEET ---
        // Nếu link là link xem (chứa /edit), ta đổi nó thành link xuất file CSV (/export)
        if (url.Contains("docs.google.com") && url.Contains("/edit"))
        {
            // Link gốc: .../d/KEY/edit#gid=0
            // Link cần: .../d/KEY/export?format=csv&gid=0

            // Thay thế đoạn /edit... thành đoạn export
            // Lưu ý: Nếu link có #gid=... thì phải đổi thành &gid=... để WebClient hiểu
            url = url.Replace("/edit", "/export?format=csv");

            // Xử lý GID (Tab ID) nếu có (chuyển dấu # thành &)
            if (url.Contains("#gid="))
            {
                url = url.Replace("#gid=", "&gid=");
            }
        }

        Debug.Log("Downloading from URL: " + url); // Log ra để kiểm tra link cuối cùng

        try
        {
            // Dùng WebClient để tải
            using (var client = new WebClient())
            {
                // Encoding UTF8 để không lỗi font tiếng Việt
                client.Encoding = System.Text.Encoding.UTF8;
                string data = client.DownloadString(url);

                Debug.Log("CSV Data Downloaded Successfully");
                return data;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Lỗi tải CSV: " + e.Message + "\nURL: " + url);
            // Nếu lỗi 401/403 nghĩa là bạn chưa share quyền "Anyone with link"
            return null;
        }
    }

    private static void CreateFolder(string folderAddress)
    {
        if (string.IsNullOrEmpty(folderAddress)) return;

        var parts = folderAddress.Split('/');
        var current = "Assets"; // Bắt đầu từ Assets để an toàn

        // Nếu người dùng nhập "Assets/Data" thì xử lý đúng, nếu nhập "Data" cũng xử lý đúng
        if (parts[0] == "Assets")
        {
            current = parts[0];
            // Bỏ qua phần tử đầu nếu là Assets để vòng lặp chạy từ phần tử tiếp theo
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
                sb.AppendLine($"    public {type} {fieldName};");
            }
        }

        sb.AppendLine("}");

        // Đảm bảo đường dẫn đúng format của Unity
        if (!outputFolder.StartsWith("Assets")) outputFolder = "Assets/" + outputFolder;

        string path = $"{outputFolder}/{className}.cs";
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"Đã tạo file class tại: {path}");
    }

    // Hàm này đã được tách ra để không phụ thuộc vào `view` (vì `view` có thể bị mất sau khi reload)
    private static void GenerateScriptableObjects(string outputFolder, string soName, string csvData)
    {
        var lines = csvData.Split('\n');
        var types = lines[0].Split(',');
        var fields = lines[1].Split(',');

        // Đảm bảo đường dẫn
        if (!outputFolder.StartsWith("Assets")) outputFolder = "Assets/" + outputFolder;

        for (var i = 2; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var values = lines[i].Split(',');
            CreateSO(outputFolder, soName, fields, types, values, i - 1);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>HOÀN TẤT QUÁ TRÌNH IMPORT!</color>");
    }

    private static void CreateSO(string outputFolder, string soName, string[] fields, string[] types, string[] values, int index)
    {
        // Lúc này Unity đã biên dịch xong, nên Type.GetType sẽ tìm thấy class mới
        // Lưu ý: Nếu Class nằm trong namespace, bạn cần cung cấp "Namespace.ClassName, AssemblyName"
        Type type = Type.GetType(soName);

        if (type == null)
        {
            // Thử tìm trong Assembly-CSharp nếu tìm bình thường không thấy
            type = System.Reflection.Assembly.Load("Assembly-CSharp").GetType(soName);
        }

        if (type == null)
        {
            Debug.LogError($"Vẫn không tìm thấy Class '{soName}'. Hãy đảm bảo tên Class trong CSV trùng với tên file.");
            return;
        }

        var so = ScriptableObject.CreateInstance(type);

        for (var c = 0; c < fields.Length; c++)
        {
            if (c >= values.Length) break; // Tránh lỗi index out of range

            var fieldName = fields[c].Trim();
            var typeName = types[c].Trim();
            var rawValue = values[c].Trim();

            var fieldInfo = type.GetField(fieldName); // Dùng type lấy được ở trên

            if (fieldInfo == null) continue;

            try
            {
                var convertedValue = ConvertValue(typeName, rawValue);
                fieldInfo.SetValue(so, convertedValue);
            }
            catch(Exception ex)
            {
                Debug.LogWarning($"Lỗi parse dữ liệu dòng {index}, cột {fieldName}: {ex.Message}");
            }
        }

        // Tạo tên file SO đẹp hơn (ví dụ dùng ID nếu có, ở đây dùng index tạm)
        // Check xem có cột "id" không để đặt tên file
        string fileName = $"{soName}_{index}";
        int idIndex = Array.IndexOf(fields, "id");
        if (idIndex != -1 && idIndex < values.Length) fileName = $"{soName}_{values[idIndex]}";

        var assetPath = $"{outputFolder}/{fileName}.asset";

        // Tạo file asset, nếu đã có thì ghi đè (CreateAsset tự xử lý unique name, nhưng ta nên check)
        AssetDatabase.CreateAsset(so, assetPath);
    }

    private static object ConvertValue(string type, string value)
    {
        // Xử lý thêm trường hợp chuỗi rỗng để tránh lỗi Parse
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
            "bool" => bool.Parse(value), // Lưu ý: bool.Parse cần chuỗi "True"/"False"
            _ => value
        };
    }
}