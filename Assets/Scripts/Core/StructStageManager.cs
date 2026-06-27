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


    private int currentYear = InitialYear;
    private int money;
    private int convenience;
    private int science;
    private int people;
    private int love;
    private StatValues pendingValues;

    public event Action<int> BeforeYearProduction;
    public event Action<int> AfterYearProduction;

    public int CurrentYear { get { return currentYear; } }
    public int Money { get { return money; } }
    public int Science { get { return science; } }

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

        if (BeforeYearProduction != null)
        {
            BeforeYearProduction(currentYear);
        }

        RefreshPendingValues();

        money += pendingValues.Money;
        convenience += pendingValues.Convenience;
        science += pendingValues.Science;
        people += pendingValues.People;
        love += pendingValues.Love;

        UpdateMainTexts();
        UpdateYearText();

        if (AfterYearProduction != null)
        {
            AfterYearProduction(currentYear);
        }

        RefreshPendingValues();
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
        loveTexts = FindStatTexts(uiRoot, "LovePanel");

        structureActionManager = uiRoot.GetComponent<StructureActionManager>();

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

    public void RefreshPendingValues()
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
        total.People += values.People;
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
        total.People += definition.PeopleIncrease;
        total.Science += definition.ScienceIncrease;
        total.Love += definition.LoveIncrease;
        total.Convenience += definition.ConvenienceIncrease;
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
