using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class XPLevelTableLoader : MonoBehaviour
{
    private const string DefaultJsonAssetPath = "Assets/Script/XP/XPLevels.json";

    public TextAsset jsonFile;

    [SerializeField] private List<XPLevelEntry> loadedLevels = new List<XPLevelEntry>();

    public void EnsureLoaded()
    {
        if (loadedLevels == null || loadedLevels.Count == 0)
        {
            Reload();
        }
    }

    public void Reload()
    {
        string json = ReadJsonText();
        loadedLevels = ParseLevels(json);

        if (loadedLevels.Count == 0)
        {
            loadedLevels.Add(new XPLevelEntry(1, 100));
        }

        loadedLevels.Sort((a, b) => a.level.CompareTo(b.level));
    }

    public int GetRequiredXPForLevel(int level)
    {
        EnsureLoaded();

        int normalizedLevel = Mathf.Max(1, level);
        XPLevelEntry closestEntry = loadedLevels[0];

        for (int i = 0; i < loadedLevels.Count; i++)
        {
            XPLevelEntry entry = loadedLevels[i];
            if (entry.level == normalizedLevel)
            {
                return Mathf.Max(1, entry.maxXP);
            }

            if (entry.level > normalizedLevel)
            {
                break;
            }

            closestEntry = entry;
        }

        return Mathf.Max(1, closestEntry.maxXP);
    }

    private void Reset()
    {
        TryAssignDefaultJsonFile();
        Reload();
    }

    private void Awake()
    {
        EnsureLoaded();
    }

    private void OnValidate()
    {
        TryAssignDefaultJsonFile();
    }

    private string ReadJsonText()
    {
        if (jsonFile != null && !string.IsNullOrWhiteSpace(jsonFile.text))
        {
            return jsonFile.text;
        }

        string fullPath = Path.Combine(Application.dataPath, "Script/XP/XPLevels.json");
        if (File.Exists(fullPath))
        {
            return File.ReadAllText(fullPath);
        }

        return string.Empty;
    }

    private static List<XPLevelEntry> ParseLevels(string json)
    {
        List<XPLevelEntry> result = new List<XPLevelEntry>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }

        XPLevelJsonData jsonData = JsonUtility.FromJson<XPLevelJsonData>(json);
        if (jsonData == null || jsonData.levels == null)
        {
            return result;
        }

        for (int i = 0; i < jsonData.levels.Length; i++)
        {
            string rawEntry = jsonData.levels[i];
            if (string.IsNullOrWhiteSpace(rawEntry))
            {
                continue;
            }

            string[] parts = rawEntry.Split(':');
            if (parts.Length != 2)
            {
                continue;
            }

            if (!int.TryParse(parts[0].Trim(), out int level))
            {
                continue;
            }

            if (!int.TryParse(parts[1].Trim(), out int maxXP))
            {
                continue;
            }

            if (level <= 0 || maxXP <= 0)
            {
                continue;
            }

            bool replacedExisting = false;
            for (int j = 0; j < result.Count; j++)
            {
                if (result[j].level != level)
                {
                    continue;
                }

                result[j] = new XPLevelEntry(level, maxXP);
                replacedExisting = true;
                break;
            }

            if (!replacedExisting)
            {
                result.Add(new XPLevelEntry(level, maxXP));
            }
        }

        return result;
    }

    private void TryAssignDefaultJsonFile()
    {
#if UNITY_EDITOR
        if (jsonFile == null)
        {
            jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultJsonAssetPath);
        }
#endif
    }

    [Serializable]
    private class XPLevelJsonData
    {
        public string[] levels;
    }
}

[Serializable]
public class XPLevelEntry
{
    public int level;
    public int maxXP;

    public XPLevelEntry(int level, int maxXP)
    {
        this.level = level;
        this.maxXP = maxXP;
    }
}
