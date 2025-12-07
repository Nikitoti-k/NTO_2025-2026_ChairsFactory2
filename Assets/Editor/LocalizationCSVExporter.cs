// LocalizationCSVExporter.cs
// Путь: Assets/Editor/LocalizationCSVExporter.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public static class LocalizationCSVExporter
{
    [MenuItem("Tools/Localization/Export Current Localization to CSV")]
    private static void ExportToCSV()
    {
        string savePath = EditorUtility.SaveFilePanel("Save localization CSV", "", "localization", "csv");
        if (string.IsNullOrEmpty(savePath)) return;

        var sb = new StringBuilder();
        sb.AppendLine("Key,RU,EN"); // Заголовок

        // Берём все ключи из русского словаря (он полный)
        foreach (var kvp in LocalizationData.RU)
        {
            string key = kvp.Key;
            string ru = kvp.Value.Replace("\n", "\\n"); // Экранируем переносы
            string en = LocalizationData.EN.TryGetValue(key, out var enValue)
                ? enValue.Replace("\n", "\\n")
                : "";

            sb.AppendLine($"{key},{EscapeCsv(ru)},{EscapeCsv(en)}");
        }

        File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"Localization exported to CSV: {savePath}");
        EditorUtility.RevealInFinder(savePath);
    }

    private static string EscapeCsv(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
        {
            s = s.Replace("\"", "\"\"");
            return $"\"{s}\"";
        }
        return s;
    }
}