using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class PolicyDefinitionDatabase
{
    public static Dictionary<string, PolicyDefinitionData> Load(string relativePath)
    {
        Dictionary<string, PolicyDefinitionData> definitions = new Dictionary<string, PolicyDefinitionData>();
        string csvPath = Path.Combine(Application.dataPath, relativePath);
        if (!File.Exists(csvPath))
        {
            Debug.LogWarning("PolicyDefinition.csv was not found at " + csvPath + ".");
            return definitions;
        }

        string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
        if (lines.Length == 0)
        {
            Debug.LogWarning("PolicyDefinition.csv is empty.");
            return definitions;
        }

        Dictionary<string, int> headers = BuildHeaderIndexes(StructDefinitionDatabase.ParseCsvLine(lines[0]));
        if (!headers.ContainsKey("정책이름"))
        {
            Debug.LogWarning("PolicyDefinition.csv is missing required column: 정책이름");
            return definitions;
        }

        for (int i = 1; i < lines.Length; i += 1)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            PolicyDefinitionData definition;
            if (!TryParseDefinition(StructDefinitionDatabase.ParseCsvLine(lines[i]), headers, i + 1, out definition))
            {
                continue;
            }

            definitions[definition.Name] = definition;
        }

        return definitions;
    }

    private static Dictionary<string, int> BuildHeaderIndexes(string[] headers)
    {
        Dictionary<string, int> indexes = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i += 1)
        {
            string header = headers[i].Trim();
            if (!indexes.ContainsKey(header))
            {
                indexes.Add(header, i);
            }
        }

        return indexes;
    }

    private static bool TryParseDefinition(string[] columns, Dictionary<string, int> headers, int lineNumber, out PolicyDefinitionData definition)
    {
        definition = null;
        string name = GetString(columns, headers, "정책이름", string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("PolicyDefinition.csv line " + lineNumber + " has empty policy name.");
            return false;
        }

        int durationYears;
        int unlockYear;
        if (!GetInt(columns, headers, "정책 유효 기간", 0, lineNumber, out durationYears) ||
            !GetInt(columns, headers, "해금 년도", 0, lineNumber, out unlockYear))
        {
            return false;
        }

        string description = StructDefinitionDatabase.NormalizeCsvText(GetString(columns, headers, "정책 설명", string.Empty).Trim());
        string effect = GetString(columns, headers, "정책 유효 내용", string.Empty).Trim();
        string requirement = GetString(columns, headers, "요구 능력치", "없음").Trim();
        if (string.IsNullOrEmpty(requirement))
        {
            requirement = "없음";
        }

        definition = new PolicyDefinitionData(name, description, durationYears, effect, requirement, unlockYear);
        return true;
    }

    private static string GetString(string[] columns, Dictionary<string, int> headers, string header, string fallback)
    {
        int index;
        if (!headers.TryGetValue(header, out index) || index < 0 || index >= columns.Length)
        {
            return fallback;
        }

        return columns[index];
    }

    private static bool GetInt(string[] columns, Dictionary<string, int> headers, string header, int fallback, int lineNumber, out int value)
    {
        value = fallback;
        int index;
        if (!headers.TryGetValue(header, out index) || index < 0 || index >= columns.Length)
        {
            return true;
        }

        string rawValue = columns[index].Trim();
        if (string.IsNullOrEmpty(rawValue))
        {
            return true;
        }

        rawValue = rawValue.Replace(",", string.Empty).Replace("년", string.Empty);
        if (!int.TryParse(rawValue, out value))
        {
            Debug.LogWarning("PolicyDefinition.csv line " + lineNumber + " column " + header + " must be an integer, value='" + columns[index] + "'.");
            return false;
        }

        return true;
    }
}

public class PolicyDefinitionData
{
    public readonly string Name;
    public readonly string Description;
    public readonly int DurationYears;
    public readonly string Effect;
    public readonly string Requirement;
    public readonly int UnlockYear;

    public PolicyDefinitionData(string name, string description, int durationYears, string effect, string requirement, int unlockYear)
    {
        Name = name;
        Description = description;
        DurationYears = durationYears;
        Effect = effect;
        Requirement = requirement;
        UnlockYear = unlockYear;
    }
}
