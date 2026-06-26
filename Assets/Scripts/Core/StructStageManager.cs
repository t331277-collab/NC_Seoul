using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StructStageManager : MonoBehaviour
{
    private const int InitialYear = 1945;
    private const string StructPrefix = "Stru";
    private const string IgnoredStructName = "Stru_CommonSense";

    [SerializeField] private string structDefinitionRelativePath = "Data/StructDefinition.csv";

    private readonly Dictionary<string, StructDefinition> structDefinitions = new Dictionary<string, StructDefinition>();

    private Transform seoulRoot;
    private Button nextYearButton;
    private TextMeshProUGUI yearText;
    private StatTexts moneyTexts;
    private StatTexts convenienceTexts;
    private StatTexts scienceTexts;
    private StatTexts peopleTexts;
    private StatTexts loveTexts;

    private int currentYear = InitialYear;
    private int money;
    private int convenience;
    private int science;
    private int people;
    private int love;
    private StatValues pendingValues;

    private void Awake()
    {
        BindSceneObjects();
        LoadStructDefinitions();
        InitializeValues();
        RefreshPendingValues();
    }

    private void OnEnable()
    {
        if (nextYearButton != null)
        {
            nextYearButton.onClick.AddListener(ApplyNextYear);
        }
    }

    private void OnDisable()
    {
        if (nextYearButton != null)
        {
            nextYearButton.onClick.RemoveListener(ApplyNextYear);
        }
    }

    public void ApplyNextYear()
    {
        money += pendingValues.Money;
        convenience += pendingValues.Convenience;
        science += pendingValues.Science;
        people += pendingValues.People;
        love += pendingValues.Love;
        currentYear += 1;

        UpdateMainTexts();
        UpdateYearText();
        RefreshPendingValues();
    }

    private void BindSceneObjects()
    {
        GameObject seoulObject = GameObject.Find("Seoul");
        if (seoulObject != null)
        {
            seoulRoot = seoulObject.transform;
        }

        Transform uiRoot = transform;
        if (gameObject.name != "UI")
        {
            GameObject uiObject = GameObject.Find("UI");
            if (uiObject != null)
            {
                uiRoot = uiObject.transform;
            }
        }

        moneyTexts = FindStatTexts(uiRoot, "MoneyPanel");
        convenienceTexts = FindStatTexts(uiRoot, "ConveniencePanel");
        scienceTexts = FindStatTexts(uiRoot, "SciencePanel");
        if (scienceTexts.MainText == null)
        {
            scienceTexts = FindStatTexts(uiRoot, "SciecnePanel");
        }
        peopleTexts = FindStatTexts(uiRoot, "PeoplePanel");
        loveTexts = FindStatTexts(uiRoot, "LovePanel");

        Transform yearPanel = uiRoot.Find("YearPanel");
        if (yearPanel != null)
        {
            yearText = FindText(yearPanel, "Text (TMP)");
        }

        Transform nextYearTransform = uiRoot.Find("NextYearBtn");
        if (nextYearTransform != null)
        {
            nextYearButton = nextYearTransform.GetComponent<Button>();
        }
    }

    private void LoadStructDefinitions()
    {
        structDefinitions.Clear();

        string csvPath = Path.Combine(Application.dataPath, structDefinitionRelativePath);
        if (!File.Exists(csvPath))
        {
            Debug.LogWarning($"StructDefinition.csv was not found at {csvPath}.");
            return;
        }

        string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] columns = line.Split(',');
            if (columns.Length < 8)
            {
                continue;
            }

            if (!TryParseDefinition(columns, out StructDefinition definition))
            {
                continue;
            }

            structDefinitions[definition.Name] = definition;
        }
    }

    private bool TryParseDefinition(string[] columns, out StructDefinition definition)
    {
        definition = default;

        string structName = columns[0].Trim();
        if (string.IsNullOrEmpty(structName))
        {
            return false;
        }

        if (!int.TryParse(columns[2].Trim(), out int unlockYear) ||
            !int.TryParse(columns[3].Trim(), out int moneyProduction) ||
            !int.TryParse(columns[4].Trim(), out int peopleIncrease) ||
            !int.TryParse(columns[5].Trim(), out int scienceIncrease) ||
            !int.TryParse(columns[6].Trim(), out int loveIncrease) ||
            !int.TryParse(columns[7].Trim(), out int convenienceIncrease))
        {
            return false;
        }

        definition = new StructDefinition(structName, unlockYear, moneyProduction, peopleIncrease, scienceIncrease, loveIncrease, convenienceIncrease);
        return true;
    }

    private void InitializeValues()
    {
        currentYear = ReadTextNumber(yearText, InitialYear);
        money = ReadTextNumber(moneyTexts.MainText, 0);
        convenience = ReadTextNumber(convenienceTexts.MainText, 0);
        science = ReadTextNumber(scienceTexts.MainText, 0);
        people = ReadTextNumber(peopleTexts.MainText, 0);
        love = ReadTextNumber(loveTexts.MainText, 0);

        UpdateMainTexts();
        UpdateYearText();
    }

    private void RefreshPendingValues()
    {
        pendingValues = CalculateCurrentStructValues();

        SetText(moneyTexts.PlusMinusText, FormatPending(pendingValues.Money));
        SetText(convenienceTexts.PlusMinusText, FormatPending(pendingValues.Convenience));
        SetText(scienceTexts.PlusMinusText, FormatPending(pendingValues.Science));
        SetText(peopleTexts.PlusMinusText, FormatPending(pendingValues.People));
        SetText(loveTexts.PlusMinusText, FormatPending(pendingValues.Love));
    }

    private StatValues CalculateCurrentStructValues()
    {
        StatValues total = default;
        if (seoulRoot == null)
        {
            return total;
        }

        AddStructValues(seoulRoot, ref total);
        return total;
    }

    private void AddStructValues(Transform parent, ref StatValues total)
    {
        foreach (Transform child in parent)
        {
            if (child.gameObject.activeInHierarchy && child.name != IgnoredStructName && child.name.StartsWith(StructPrefix, StringComparison.Ordinal))
            {
                AddStructValue(child.name, ref total);
            }

            AddStructValues(child, ref total);
        }
    }

    private void AddStructValue(string structName, ref StatValues total)
    {
        if (!structDefinitions.TryGetValue(structName, out StructDefinition definition))
        {
            return;
        }

        if (currentYear < definition.UnlockYear)
        {
            return;
        }

        total.Money += definition.MoneyProduction;
        total.People += definition.PeopleIncrease;
        total.Science += definition.ScienceIncrease;
        total.Love += definition.LoveIncrease;
        total.Convenience += definition.ConvenienceIncrease;
    }

    private void UpdateMainTexts()
    {
        SetText(moneyTexts.MainText, money.ToString());
        SetText(convenienceTexts.MainText, convenience.ToString());
        SetText(scienceTexts.MainText, science.ToString());
        SetText(peopleTexts.MainText, people.ToString());
        SetText(loveTexts.MainText, love.ToString());
    }

    private void UpdateYearText()
    {
        SetText(yearText, currentYear.ToString());
    }

    private StatTexts FindStatTexts(Transform uiRoot, string panelName)
    {
        if (uiRoot == null)
        {
            return default;
        }

        Transform panel = uiRoot.Find(panelName);
        if (panel == null)
        {
            return default;
        }

        return new StatTexts
        {
            MainText = FindText(panel, "Text (TMP)"),
            PlusMinusText = FindText(panel, "PlusMinus")
        };
    }

    private TextMeshProUGUI FindText(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform child = parent.Find(childName);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<TextMeshProUGUI>();
    }

    private int ReadTextNumber(TextMeshProUGUI text, int fallback)
    {
        if (text == null || string.IsNullOrWhiteSpace(text.text))
        {
            return fallback;
        }

        string value = text.text.Trim();
        if (int.TryParse(value, out int parsedValue))
        {
            return parsedValue;
        }

        return fallback;
    }

    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private string FormatPending(int value)
    {
        if (value > 0)
        {
            return "+" + value;
        }

        return value.ToString();
    }

    private struct StructDefinition
    {
        public readonly string Name;
        public readonly int UnlockYear;
        public readonly int MoneyProduction;
        public readonly int PeopleIncrease;
        public readonly int ScienceIncrease;
        public readonly int LoveIncrease;
        public readonly int ConvenienceIncrease;

        public StructDefinition(string name, int unlockYear, int moneyProduction, int peopleIncrease, int scienceIncrease, int loveIncrease, int convenienceIncrease)
        {
            Name = name;
            UnlockYear = unlockYear;
            MoneyProduction = moneyProduction;
            PeopleIncrease = peopleIncrease;
            ScienceIncrease = scienceIncrease;
            LoveIncrease = loveIncrease;
            ConvenienceIncrease = convenienceIncrease;
        }
    }

    private struct StatTexts
    {
        public TextMeshProUGUI MainText;
        public TextMeshProUGUI PlusMinusText;
    }

    private struct StatValues
    {
        public int Money;
        public int Convenience;
        public int Science;
        public int People;
        public int Love;
    }
}
