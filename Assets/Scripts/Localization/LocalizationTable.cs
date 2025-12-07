using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LocalizationTable", menuName = "Localization/CSV Loader", order = 1)]
public class LocalizationTable : ScriptableObject
{
    public TextAsset csvFile;

    public Dictionary<string, Dictionary<string, string>> Load()
    {
        var result = new Dictionary<string, Dictionary<string, string>>();

        if (csvFile == null)
        {
            Debug.LogError("CSV file not assigned!");
            return result;
        }

        string[] lines = csvFile.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        string[] headers = null;
        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            string[] parts = ParseCsvLine(line);

            if (headers == null)
            {
                headers = parts;
                foreach (string h in headers)
                    if (h != "Key") result[h] = new Dictionary<string, string>();
                continue;
            }

            if (parts.Length < headers.Length) continue;

            string key = parts[0];
            for (int i = 1; i < headers.Length; i++)
            {
                string text = parts[i].Replace("\\n", "\n");
                result[headers[i]][key] = text;
            }
        }
        return result;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(c);
        }
        result.Add(sb.ToString());
        return result.ToArray();
    }
}