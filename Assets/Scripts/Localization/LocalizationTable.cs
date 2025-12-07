using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

[CreateAssetMenu(fileName = "LocalizationTable", menuName = "LLL/Csvv")]
public class LocalizationTable : ScriptableObject
{

    [Header("Для редактора — перетащи CSV сюда")]
    public TextAsset editorCsvFile;

    [Header("Имя файла в StreamingAssets (например: localization.csv)")]
    public string streamingFileName = "localization.csv";

    // Кэшируем текст, чтобы не читать файл каждый раз
    private string cachedCsvText;

    /// <summary>
    /// Возвращает текст CSV — сначала из StreamingAssets, потом из editorCsvFile (fallback)
    /// </summary>
    public string GetCsvText()
    {
        if (!string.IsNullOrEmpty(cachedCsvText))
            return cachedCsvText;

        // 1. Пытаемся загрузить из StreamingAssets (работает и в редакторе, и в билде
        string path = Path.Combine(Application.streamingAssetsPath, streamingFileName);

        if (File.Exists(path))
        {
            try
            {
                cachedCsvText = File.ReadAllText(path, Encoding.UTF8);
                Debug.Log($"[Localization] CSV загружен из StreamingAssets: {path}");
                return cachedCsvText;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Localization] Ошибка чтения из StreamingAssets: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[Localization] Файл не найден в StreamingAssets: {path}");
        }

#if UNITY_EDITOR
        // 2. В редакторе — fallback на перетащенный TextAsset
        if (editorCsvFile != null)
        {
            cachedCsvText = editorCsvFile.text;
            Debug.Log("[Localization] CSV загружен из TextAsset (редактор)");
            return cachedCsvText;
        }
#endif

        Debug.LogError("[Localization] CSV не найден нигде! Укажи файл в StreamingAssets или перетащи в editorCsvFile");
        cachedCsvText = "";
        return "";
    }

    /// <summary>
    /// Основной метод — возвращает готовые словари по языкам
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> Load()
    {
        string csvText = GetCsvText();
        if (string.IsNullOrEmpty(csvText))
            return new Dictionary<string, Dictionary<string, string>>();

        var result = new Dictionary<string, Dictionary<string, string>>();
        string[] lines = csvText.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

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
                    if (h != "Key" && !string.IsNullOrEmpty(h))
                        result[h] = new Dictionary<string, string>();
                continue;
            }

            if (parts.Length == 0 || parts.Length < headers.Length) continue;

            string key = parts[0];

            for (int i = 1; i < headers.Length; i++)
            {
                if (i >= parts.Length) break;
                string text = parts[i].Replace("\\n", "\n");
                result[headers[i]][key] = text;
            }
        }

        return result;
    }

    // Твой проверенный парсер CSV (оставляем как есть — он отличный!)
    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

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
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        result.Add(sb.ToString());
        return result.ToArray();
    }
}