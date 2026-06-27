using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class StructDefinitionDatabase
{
    public static Dictionary<string, StructDefinitionData> Load(string relativePath)
    {
        Dictionary<string, StructDefinitionData> definitions = new Dictionary<string, StructDefinitionData>();
        string csvPath = Path.Combine(Application.dataPath, relativePath);
        if (!File.Exists(csvPath))
        {
            Debug.LogWarning("StructDefinition.csv was not found at " + csvPath + ".");
            return definitions;
        }

        string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
        if (lines.Length == 0)
        {
            Debug.LogWarning("StructDefinition.csv is empty.");
            return definitions;
        }

        Dictionary<string, int> headers = BuildHeaderIndexes(ParseCsvLine(lines[0]));
        if (!headers.ContainsKey("건물 이름"))
        {
            Debug.LogWarning("StructDefinition.csv is missing required column: 건물 이름");
            return definitions;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            StructDefinitionData definition;
            if (!TryParseDefinition(ParseCsvLine(lines[i]), headers, i + 1, out definition))
            {
                continue;
            }

            definitions[definition.Name] = definition;
        }

        return definitions;
    }

    public static string[] ParseCsvLine(string line)
    {
        List<string> fields = new List<string>();
        StringBuilder field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(field.ToString());
                field.Length = 0;
            }
            else
            {
                field.Append(c);
            }
        }

        fields.Add(field.ToString());
        return fields.ToArray();
    }

    public static string NormalizeCsvText(string value)
    {
        return string.IsNullOrEmpty(value) ? value : value.Replace("\\n", "\n");
    }

    private static Dictionary<string, int> BuildHeaderIndexes(string[] headers)
    {
        Dictionary<string, int> indexes = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++)
        {
            string header = headers[i].Trim();
            if (!indexes.ContainsKey(header))
            {
                indexes.Add(header, i);
            }
        }

        return indexes;
    }

    private static bool TryParseDefinition(string[] columns, Dictionary<string, int> headers, int lineNumber, out StructDefinitionData definition)
    {
        definition = null;
        string name = GetString(columns, headers, "건물 이름", string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("StructDefinition.csv line " + lineNumber + " has empty building name.");
            return false;
        }

        string displayName = GetString(columns, headers, "출력 이름", name).Trim();
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = name;
        }

        int unlockYear;
        int unlockScience;
        int buildCost;
        int buildYears;
        int investCost;
        int repairCost;
        int destroyCost;
        int moneyProduction;
        int peopleIncrease;
        int scienceIncrease;
        int loveIncrease;
        int convenienceIncrease;

        if (!GetInt(columns, headers, "해금 년도", 0, lineNumber, out unlockYear) ||
            !GetInt(columns, headers, "해금 기술력", 0, lineNumber, out unlockScience) ||
            !GetInt(columns, headers, "건설 비용", 0, lineNumber, out buildCost) ||
            !GetInt(columns, headers, "건설 시간", 0, lineNumber, out buildYears) ||
            !GetInt(columns, headers, "지원 비용", 0, lineNumber, out investCost) ||
            !GetInt(columns, headers, "보수 비용", 0, lineNumber, out repairCost) ||
            !GetInt(columns, headers, "철거 비용", 0, lineNumber, out destroyCost) ||
            !GetInt(columns, headers, "자금생산량", 0, lineNumber, out moneyProduction) ||
            !GetInt(columns, headers, "인구수 증가량", 0, lineNumber, out peopleIncrease) ||
            !GetInt(columns, headers, "기술력 증가량", 0, lineNumber, out scienceIncrease) ||
            !GetInt(columns, headers, "사랑 증가량", 0, lineNumber, out loveIncrease) ||
            !GetInt(columns, headers, "편의성 증가량", 0, lineNumber, out convenienceIncrease))
        {
            return false;
        }

        string imagePath = GetString(columns, headers, "이미지 링크", string.Empty).Trim();
        string description = NormalizeCsvText(GetString(columns, headers, "부연설명", "설명글 추가 예정").Trim());
        if (string.IsNullOrEmpty(description))
        {
            description = "설명글 추가 예정";
        }

        string startYear = NormalizeCsvText(GetString(columns, headers, "설립연도", "임시").Trim());
        if (string.IsNullOrEmpty(startYear))
        {
            startYear = "임시";
        }

        definition = new StructDefinitionData(name, displayName, unlockYear, unlockScience, buildCost, buildYears, investCost, repairCost, destroyCost, moneyProduction, peopleIncrease, scienceIncrease, loveIncrease, convenienceIncrease, imagePath, description, startYear);
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

        rawValue = rawValue.Replace(",", string.Empty);
        if (!int.TryParse(rawValue, out value))
        {
            Debug.LogWarning("StructDefinition.csv line " + lineNumber + " column " + header + " must be an integer, value='" + columns[index] + "'.");
            return false;
        }

        return true;
    }
}

public class StructDefinitionData
{
    public readonly string Name;
    public readonly string DisplayName;
    public readonly int UnlockYear;
    public readonly int UnlockScience;
    public readonly int BuildCost;
    public readonly int BuildYears;
    public readonly int InvestCost;
    public readonly int RepairCost;
    public readonly int DestroyCost;
    public readonly int MoneyProduction;
    public readonly int PeopleIncrease;
    public readonly int ScienceIncrease;
    public readonly int LoveIncrease;
    public readonly int ConvenienceIncrease;
    public readonly string ImagePath;
    public readonly string Description;
    public readonly string StartYear;

    public int Money { get { return MoneyProduction; } }
    public int People { get { return PeopleIncrease; } }
    public int Science { get { return ScienceIncrease; } }
    public int Love { get { return LoveIncrease; } }
    public int Convenience { get { return ConvenienceIncrease; } }

    public StructDefinitionData(string name, string displayName, int unlockYear, int unlockScience, int buildCost, int buildYears, int investCost, int repairCost, int destroyCost, int moneyProduction, int peopleIncrease, int scienceIncrease, int loveIncrease, int convenienceIncrease, string imagePath, string description, string startYear)
    {
        Name = name;
        DisplayName = displayName;
        UnlockYear = unlockYear;
        UnlockScience = unlockScience;
        BuildCost = buildCost;
        BuildYears = buildYears;
        InvestCost = investCost;
        RepairCost = repairCost;
        DestroyCost = destroyCost;
        MoneyProduction = moneyProduction;
        PeopleIncrease = peopleIncrease;
        ScienceIncrease = scienceIncrease;
        LoveIncrease = loveIncrease;
        ConvenienceIncrease = convenienceIncrease;
        ImagePath = imagePath;
        Description = description;
        StartYear = startYear;
    }

    public static StructDefinitionData CreateFallback(string name)
    {
        return new StructDefinitionData(name, name, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, string.Empty, "설명글 추가 예정", "임시");
    }
}
