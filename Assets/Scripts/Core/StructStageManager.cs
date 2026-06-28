using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StructStageManager : MonoBehaviour
{
    private const int InitialYear = 1945;
    private const string IgnoredStructName = "Stru_CommonSense";
    private const int HouseCapacityMultiplier = 10;
    private const float PopulationCapacityGrowthFactor = 0.01f;
    private const float PopulationMoneyGrowthFactor = 0.005f;
    private const float PopulationScienceGrowthFactor = 0.005f;
    private const float ScienceMoneyBonusFactor = 0.1f;
    private static readonly Color32 PeopleOverCapacityColor = new Color32(220, 64, 64, 255);
    private const float CutSceneFadeDuration = 1f;
    private const float CutSceneTextInterval = 0.5f;
    private const float CutSceneHoldDuration = 1f;



    [SerializeField] private string structDefinitionRelativePath = "Data/StructDefinition.csv";

    private readonly Dictionary<string, StructDefinitionData> structDefinitions = new Dictionary<string, StructDefinitionData>();

    private Transform seoulRoot;
    private Button nextYearButton;
    private TextMeshProUGUI yearText;
    private StatTexts moneyTexts;
    private StatTexts convenienceTexts;
    private StatTexts scienceTexts;
    private StatTexts peopleTexts;
    private StatTexts loveTexts;
    private StructureActionManager structureActionManager;
    private Transform cutSceneRoot;
    private CanvasGroup cutSceneCanvasGroup;
    private TextMeshProUGUI cutSceneYearNumberText;
    private TextMeshProUGUI cutSceneYearNameText;
    private bool isYearTransitionPlaying;
    private InfoNotificationManager infoNotificationManager;
    private ProtoTypeEndingManager protoTypeEndingManager;
    private bool pendingProtoTypeEnding;


    private int currentYear = InitialYear;
    private int money;
    private int convenience;
    private int science;
    private int currentPopulation;
    private int populationCapacity;
    private int populationGrowthPreview;
    private int populationCapacityDeltaPreview;
    private int love;
    private StatValues pendingValues;
    private Color peopleNormalColor = Color.white;
    private bool hasPeopleNormalColor;

    public event Action<int> BeforeYearProduction;
    public event Action<int> AfterYearProduction;

    public int CurrentYear { get { return currentYear; } }
    public int Money { get { return money; } }
    public int Science { get { return science; } }
    public int CurrentPopulation { get { return currentPopulation; } }
    public int PopulationCapacity { get { return populationCapacity; } }
    public int PopulationGrowthPreview { get { return populationGrowthPreview; } }
    public int PopulationCapacityDeltaPreview { get { return populationCapacityDeltaPreview; } }

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
            nextYearButton.onClick.AddListener(HandleNextYearButtonClicked);
        }
    }

private void OnDisable()
    {
        if (nextYearButton != null)
        {
            nextYearButton.onClick.RemoveListener(HandleNextYearButtonClicked);
        }
    }

private void HandleNextYearButtonClicked()
    {
        if (isYearTransitionPlaying)
        {
            return;
        }

        if (cutSceneRoot == null || cutSceneCanvasGroup == null || cutSceneYearNumberText == null || cutSceneYearNameText == null)
        {
            ApplyNextYear();
            return;
        }

        StartCoroutine(PlayNextYearTransition());
    }

private IEnumerator PlayNextYearTransition()
    {
        isYearTransitionPlaying = true;
        if (nextYearButton != null)
        {
            nextYearButton.interactable = false;
        }

        cutSceneRoot.gameObject.SetActive(true);
        cutSceneYearNumberText.gameObject.SetActive(false);
        cutSceneYearNameText.gameObject.SetActive(false);
        SetCutSceneAlpha(0f);

        yield return FadeCutScene(0f, 1f, CutSceneFadeDuration);

        ApplyNextYear();
        SetText(cutSceneYearNumberText, currentYear.ToString());
        SetText(cutSceneYearNameText, GetGanjiYearName(currentYear));

        yield return new WaitForSeconds(CutSceneTextInterval);
        cutSceneYearNumberText.gameObject.SetActive(true);

        yield return new WaitForSeconds(CutSceneTextInterval);
        cutSceneYearNameText.gameObject.SetActive(true);

        yield return new WaitForSeconds(CutSceneHoldDuration);
        yield return FadeCutScene(1f, 0f, CutSceneFadeDuration);

        cutSceneRoot.gameObject.SetActive(false);
        if (nextYearButton != null)
        {
            nextYearButton.interactable = true;
        }
        isYearTransitionPlaying = false;
        ShowPendingProtoTypeEnding();
    }

    private IEnumerator FadeCutScene(float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        SetCutSceneAlpha(fromAlpha);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetCutSceneAlpha(Mathf.Lerp(fromAlpha, toAlpha, progress));
            yield return null;
        }

        SetCutSceneAlpha(toAlpha);
    }

    private void SetCutSceneAlpha(float alpha)
    {
        if (cutSceneCanvasGroup != null)
        {
            cutSceneCanvasGroup.alpha = alpha;
        }
    }

    private string GetGanjiYearName(int year)
    {
        string[] heavenlyStems = { "경", "신", "임", "계", "갑", "을", "병", "정", "무", "기" };
        string[] earthlyBranches = { "신", "유", "술", "해", "자", "축", "인", "묘", "진", "사", "오", "미" };
        return heavenlyStems[year % heavenlyStems.Length] + earthlyBranches[year % earthlyBranches.Length] + "년";
    }


    public void ApplyNextYear()
    {
        currentYear += 1;
        ClearPreviousYearNotifications();

        if (BeforeYearProduction != null)
        {
            BeforeYearProduction(currentYear);
        }

        RefreshPopulationPreview(true);

        money += pendingValues.Money;
        convenience += pendingValues.Convenience;
        science += pendingValues.Science;
        currentPopulation += pendingValues.People;
        love += pendingValues.Love;

        UpdateMainTexts();
        UpdateYearText();

        if (AfterYearProduction != null)
        {
            AfterYearProduction(currentYear);
        }

        RefreshPendingValues();
        RequestProtoTypeEndingIfNeeded();
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount < 0 || money < amount)
        {
            return false;
        }

        money -= amount;
        UpdateMainTexts();
        RefreshPendingValues();
        return true;
    }

    public void SetMoney(int amount)
    {
        money = Mathf.Max(0, amount);
        UpdateMainTexts();
        RefreshPendingValues();
    }

    public void SetScience(int amount)
    {
        science = Mathf.Max(0, amount);
        UpdateMainTexts();
        RefreshPendingValues();
    }

    public StatValues GetStructureProduction(string structureKey)
    {
        StatValues values = default;
        if (string.IsNullOrEmpty(structureKey))
        {
            return values;
        }

        AddStructValue(structureKey, ref values);
        return values;
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
        CapturePeopleNormalColor();
        loveTexts = FindStatTexts(uiRoot, "LovePanel");

        structureActionManager = uiRoot.GetComponent<StructureActionManager>();
        infoNotificationManager = uiRoot.GetComponent<InfoNotificationManager>();
        protoTypeEndingManager = uiRoot.GetComponent<ProtoTypeEndingManager>();
        if (protoTypeEndingManager == null)
        {
            protoTypeEndingManager = uiRoot.gameObject.AddComponent<ProtoTypeEndingManager>();
        }

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

        Transform cutSceneTransform = uiRoot.Find("CutScene");
        if (cutSceneTransform != null)
        {
            cutSceneRoot = cutSceneTransform;
            cutSceneCanvasGroup = cutSceneRoot.GetComponent<CanvasGroup>();
            if (cutSceneCanvasGroup == null)
            {
                cutSceneCanvasGroup = cutSceneRoot.gameObject.AddComponent<CanvasGroup>();
            }
            cutSceneYearNumberText = FindText(cutSceneRoot, "TXT_NUM");
            cutSceneYearNameText = FindText(cutSceneRoot, "TXT_YEAR");
            SetCutSceneAlpha(0f);
            cutSceneRoot.gameObject.SetActive(false);
        }

    }

    private void LoadStructDefinitions()
    {
        structDefinitions.Clear();

        Dictionary<string, StructDefinitionData> loadedDefinitions = StructDefinitionDatabase.Load(structDefinitionRelativePath);
        foreach (KeyValuePair<string, StructDefinitionData> pair in loadedDefinitions)
        {
            structDefinitions[pair.Key] = pair.Value;
        }
    }

    private void ClearPreviousYearNotifications()
    {
        if (infoNotificationManager == null)
        {
            BindSceneObjects();
        }

        if (infoNotificationManager != null)
        {
            infoNotificationManager.ClearNotificationsForNewYear();
        }
    }

    private void RequestProtoTypeEndingIfNeeded()
    {
        if (currentYear < 2027)
        {
            return;
        }

        pendingProtoTypeEnding = true;
        if (!isYearTransitionPlaying)
        {
            ShowPendingProtoTypeEnding();
        }
    }

    private void ShowPendingProtoTypeEnding()
    {
        if (!pendingProtoTypeEnding)
        {
            return;
        }

        if (protoTypeEndingManager == null)
        {
            BindSceneObjects();
        }

        if (protoTypeEndingManager != null)
        {
            protoTypeEndingManager.ShowEnding();
            pendingProtoTypeEnding = false;
        }
    }

    private void InitializeValues()
    {
        currentYear = ReadTextNumber(yearText, InitialYear);
        money = ReadTextNumber(moneyTexts.MainText, 0);
        convenience = ReadTextNumber(convenienceTexts.MainText, 0);
        science = ReadTextNumber(scienceTexts.MainText, 0);
        populationCapacity = CalculateCurrentPopulationCapacity();
        currentPopulation = ReadPopulationText(peopleTexts.MainText, 0);
        if (currentPopulation <= 0 && populationCapacity > 0)
        {
            currentPopulation = Mathf.FloorToInt(populationCapacity * 0.6f);
        }
        love = ReadTextNumber(loveTexts.MainText, 0);

        UpdateMainTexts();
        UpdateYearText();
    }

    public void RefreshPendingValues()
    {
        RefreshPopulationPreview(true);

        ClearPlusMinusTexts();
    }

    private void RefreshPopulationPreview(bool updatePopulationCapacity)
    {
        int calculatedCapacity = CalculateCurrentPopulationCapacity();
        populationCapacityDeltaPreview = calculatedCapacity - populationCapacity;
        if (updatePopulationCapacity)
        {
            populationCapacity = calculatedCapacity;
        }

        pendingValues = CalculateCurrentStructValues();
        populationGrowthPreview = CalculatePopulationGrowth(convenience, currentPopulation, calculatedCapacity, money, science);
        pendingValues.People = populationGrowthPreview;
        pendingValues.Money += CalculatePopulationMoneyBonus(currentPopulation, calculatedCapacity, science);
        pendingValues.Convenience -= CalculateOverCapacityConveniencePenalty(currentPopulation, calculatedCapacity);

        UpdateMainTexts();
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
            if (IsProductionTarget(child))
            {
                AddStructValue(child, ref total);
            }

            AddStructValues(child, ref total);
        }
    }

    private bool IsProductionTarget(Transform target)
    {
        return target != null &&
               target.gameObject.activeInHierarchy &&
               target.name != IgnoredStructName &&
               structDefinitions.ContainsKey(target.name);
    }

    private void AddStructValue(Transform target, ref StatValues total)
    {
        if (target == null)
        {
            return;
        }

        StatValues values = default;
        AddStructValue(target.name, ref values);

        float multiplier;
        if (structureActionManager != null && structureActionManager.TryGetProductionMultiplier(target.gameObject, out multiplier))
        {
            values = Multiply(values, multiplier);
        }

        total.Money += values.Money;
        total.Science += values.Science;
        total.Love += values.Love;
        total.Convenience += values.Convenience;
    }

    private void AddStructValue(string structName, ref StatValues total)
    {
        if (!structDefinitions.TryGetValue(structName, out StructDefinitionData definition))
        {
            return;
        }

        if (currentYear < definition.UnlockYear)
        {
            return;
        }

        total.Money += definition.MoneyProduction;
        total.Science += definition.ScienceIncrease;
        total.Love += definition.LoveIncrease;
        total.Convenience += definition.ConvenienceIncrease;
    }

    private int CalculateCurrentPopulationCapacity()
    {
        if (seoulRoot == null)
        {
            return 0;
        }

        return CalculatePopulationCapacityRecursive(seoulRoot);
    }

    private int CalculatePopulationCapacityRecursive(Transform parent)
    {
        int total = 0;
        foreach (Transform child in parent)
        {
            total += GetStructurePopulationCapacity(child);
            total += CalculatePopulationCapacityRecursive(child);
        }

        return total;
    }

    private int GetStructurePopulationCapacity(Transform target)
    {
        if (target == null ||
            !target.gameObject.activeInHierarchy ||
            target.name == IgnoredStructName ||
            !IsHouseStructureName(target.name) ||
            !structDefinitions.TryGetValue(target.name, out StructDefinitionData definition))
        {
            return 0;
        }

        int baseCapacity = Mathf.Max(0, definition.PeopleIncrease) * HouseCapacityMultiplier;
        float multiplier;
        if (structureActionManager != null && structureActionManager.TryGetProductionMultiplier(target.gameObject, out multiplier))
        {
            baseCapacity = Mathf.CeilToInt(baseCapacity * multiplier);
        }

        return baseCapacity;
    }

    private bool IsHouseStructureName(string structureName)
    {
        return structureName == "House1" ||
               structureName == "House2" ||
               structureName == "House3" ||
               structureName == "House4";
    }

    private int CalculatePopulationGrowth(int convenienceValue, int populationValue, int capacityValue, int moneyValue, int scienceValue)
    {
        if (capacityValue <= 0)
        {
            return 0;
        }

        float occupancyRate = (float)populationValue / capacityValue;
        if (occupancyRate >= 1f)
        {
            return 0;
        }

        int baseGrowth = Mathf.CeilToInt(Mathf.Max(0, convenienceValue) * 0.015f);
        int capacityGrowth = Mathf.CeilToInt(capacityValue * PopulationCapacityGrowthFactor);
        int resourceGrowth = CalculatePopulationResourceGrowthBonus(moneyValue, scienceValue);
        float capacityPressure = Mathf.Clamp01((1f - occupancyRate) / 0.4f);
        return Mathf.Max(0, Mathf.CeilToInt(baseGrowth * capacityPressure) + capacityGrowth + resourceGrowth);
    }

    private int CalculatePopulationResourceGrowthBonus(int moneyValue, int scienceValue)
    {
        float moneyGrowth = Mathf.Max(0, moneyValue) * PopulationMoneyGrowthFactor;
        float scienceGrowth = Mathf.Max(0, scienceValue) * PopulationScienceGrowthFactor;
        return Mathf.FloorToInt(moneyGrowth + scienceGrowth);
    }

    private int CalculatePopulationMoneyBonus(int populationValue, int capacityValue, int scienceValue)
    {
        int effectivePopulation = capacityValue <= 0 ? 0 : Mathf.Min(populationValue, capacityValue);
        int populationBonus = Mathf.FloorToInt(effectivePopulation * 0.02f);
        int scienceBonus = Mathf.FloorToInt(Mathf.Max(0, scienceValue) * ScienceMoneyBonusFactor);
        return populationBonus + scienceBonus;
    }

    private int CalculateOverCapacityConveniencePenalty(int populationValue, int capacityValue)
    {
        int overCapacity = Mathf.Max(0, populationValue - capacityValue);
        return Mathf.CeilToInt(overCapacity * 0.1f);
    }

    private StatValues Multiply(StatValues values, float multiplier)
    {
        values.Money = Mathf.CeilToInt(values.Money * multiplier);
        values.People = Mathf.CeilToInt(values.People * multiplier);
        values.Science = Mathf.CeilToInt(values.Science * multiplier);
        values.Love = Mathf.CeilToInt(values.Love * multiplier);
        values.Convenience = Mathf.CeilToInt(values.Convenience * multiplier);
        return values;
    }

    private void UpdateMainTexts()
    {
        SetText(moneyTexts.MainText, FormatValueWithPending(money, pendingValues.Money));
        SetText(convenienceTexts.MainText, FormatValueWithPending(convenience, pendingValues.Convenience));
        SetText(scienceTexts.MainText, FormatValueWithPending(science, pendingValues.Science));
        SetText(peopleTexts.MainText, FormatPeopleValue());
        UpdatePeopleTextColor();
        SetText(loveTexts.MainText, FormatValueWithPending(love, pendingValues.Love));
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
        if (value.Contains("/"))
        {
            value = value.Split('/')[0].Trim();
        }

        value = ExtractFirstIntegerText(value);
        if (int.TryParse(value, out int parsedValue))
        {
            return parsedValue;
        }

        return fallback;
    }

    private string ExtractFirstIntegerText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        bool hasDigit = false;
        for (int i = 0; i < value.Length; i += 1)
        {
            char c = value[i];
            if (char.IsDigit(c))
            {
                builder.Append(c);
                hasDigit = true;
                continue;
            }

            if (c == '-' && !hasDigit && builder.Length == 0)
            {
                builder.Append(c);
                continue;
            }

            if (c == ',')
            {
                continue;
            }

            if (hasDigit)
            {
                break;
            }

            if (builder.Length == 1 && builder[0] == '-')
            {
                builder.Length = 0;
            }
        }

        return hasDigit ? builder.ToString() : string.Empty;
    }

    private int ReadPopulationText(TextMeshProUGUI text, int fallback)
    {
        return ReadTextNumber(text, fallback);
    }

    private void CapturePeopleNormalColor()
    {
        if (hasPeopleNormalColor || peopleTexts.MainText == null)
        {
            return;
        }

        peopleNormalColor = peopleTexts.MainText.color;
        hasPeopleNormalColor = true;
    }

    private void UpdatePeopleTextColor()
    {
        if (peopleTexts.MainText == null)
        {
            return;
        }

        if (populationCapacity > 0 && currentPopulation > populationCapacity)
        {
            peopleTexts.MainText.color = PeopleOverCapacityColor;
            return;
        }

        peopleTexts.MainText.color = peopleNormalColor;
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

    private string FormatValueWithPending(int currentValue, int pendingValue)
    {
        return currentValue.ToString() + "(" + FormatPending(pendingValue) + ")";
    }

    private string FormatPeopleValue()
    {
        return currentPopulation.ToString()
               + "(" + FormatPending(pendingValues.People) + ") / "
               + populationCapacity.ToString()
               + " (" + FormatPending(populationCapacityDeltaPreview) + ")";
    }

    private void ClearPlusMinusTexts()
    {
        SetText(moneyTexts.PlusMinusText, string.Empty);
        SetText(convenienceTexts.PlusMinusText, string.Empty);
        SetText(scienceTexts.PlusMinusText, string.Empty);
        SetText(peopleTexts.PlusMinusText, string.Empty);
        SetText(loveTexts.PlusMinusText, string.Empty);
    }

    private struct StatTexts
    {
        public TextMeshProUGUI MainText;
        public TextMeshProUGUI PlusMinusText;
    }

    public struct StatValues
    {
        public int Money;
        public int Convenience;
        public int Science;
        public int People;
        public int Love;
    }
}
