using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PolicyManager : MonoBehaviour
{
    [SerializeField] private string policyDefinitionRelativePath = "Data/PolicyDefinition.csv";
    [SerializeField] private float policyItemSpacing = 186.8f;

    private readonly Dictionary<string, PolicyDefinitionData> policyDefinitions = new Dictionary<string, PolicyDefinitionData>();
    private readonly List<GameObject> policyItems = new List<GameObject>();
    private readonly List<ActivePolicyState> activePolicies = new List<ActivePolicyState>();
    private readonly HashSet<string> usedPolicyNames = new HashSet<string>();

    private StructStageManager stageManager;
    private Button policyButton;
    private GameObject policyPanel;
    private GameObject policyChoicePanel;
    private RectTransform contentRect;
    private RectTransform templateRect;
    private TextMeshProUGUI choiceNameText;
    private TextMeshProUGUI choiceDescText;
    private Button choiceYesButton;
    private Button choiceNoButton;
    private Button closeButton;
    private PolicyDefinitionData selectedPolicy;

    private void Awake()
    {
        BindSceneObjects();
        LoadPolicyDefinitions();
        SetActive(policyPanel, false);
        SetActive(policyChoicePanel, false);
        SetActive(templateRect == null ? null : templateRect.gameObject, false);
    }

    private void OnEnable()
    {
        BindSceneObjects();
        BindButtons();
    }

    private void OnDisable()
    {
        UnbindButtons();
    }

    private void BindSceneObjects()
    {
        stageManager = GetComponent<StructStageManager>();

        Transform uiRoot = transform;
        if (gameObject.name != "UI")
        {
            GameObject uiObject = GameObject.Find("UI");
            if (uiObject != null)
            {
                uiRoot = uiObject.transform;
            }
        }

        Transform policyButtonTransform = uiRoot.Find("PolicyBtn");
        policyButton = policyButtonTransform == null ? null : policyButtonTransform.GetComponent<Button>();
        if (policyButton == null && policyButtonTransform != null)
        {
            policyButton = policyButtonTransform.gameObject.AddComponent<Button>();
        }

        Transform policyPanelTransform = uiRoot.Find("PolicyPanel");
        if (policyPanelTransform != null)
        {
            policyPanel = policyPanelTransform.gameObject;
            Transform contentTransform = policyPanelTransform.Find("Content");
            contentRect = contentTransform == null ? null : contentTransform.GetComponent<RectTransform>();
            Transform templateTransform = contentTransform == null ? null : contentTransform.Find("Template");
            templateRect = templateTransform == null ? null : templateTransform.GetComponent<RectTransform>();

            Transform closeTransform = policyPanelTransform.Find("Close");
            closeButton = closeTransform == null ? null : closeTransform.GetComponent<Button>();
            if (closeButton == null && closeTransform != null)
            {
                closeButton = closeTransform.gameObject.AddComponent<Button>();
            }
        }

        Transform choicePanelTransform = uiRoot.Find("PolicyChoicePanel");
        if (choicePanelTransform != null)
        {
            policyChoicePanel = choicePanelTransform.gameObject;
            choiceNameText = FindText(choicePanelTransform, "Statue");
            choiceDescText = FindText(choicePanelTransform, "Desc");
            choiceYesButton = FindOrAddButton(choicePanelTransform, "Yes");
            choiceNoButton = FindOrAddButton(choicePanelTransform, "No");
        }
    }

    private void LoadPolicyDefinitions()
    {
        policyDefinitions.Clear();
        Dictionary<string, PolicyDefinitionData> loadedDefinitions = PolicyDefinitionDatabase.Load(policyDefinitionRelativePath);
        foreach (KeyValuePair<string, PolicyDefinitionData> pair in loadedDefinitions)
        {
            policyDefinitions[pair.Key] = pair.Value;
        }
    }

    private void BindButtons()
    {
        if (policyButton != null)
        {
            policyButton.onClick.RemoveListener(OpenPolicyPanel);
            policyButton.onClick.AddListener(OpenPolicyPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePolicyPanel);
            closeButton.onClick.AddListener(ClosePolicyPanel);
        }

        if (choiceNoButton != null)
        {
            choiceNoButton.onClick.RemoveListener(ClosePolicyChoicePanel);
            choiceNoButton.onClick.AddListener(ClosePolicyChoicePanel);
        }

        if (choiceYesButton != null)
        {
            choiceYesButton.onClick.RemoveListener(ApplySelectedPolicy);
            choiceYesButton.onClick.AddListener(ApplySelectedPolicy);
        }
    }

    private void UnbindButtons()
    {
        if (policyButton != null)
        {
            policyButton.onClick.RemoveListener(OpenPolicyPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePolicyPanel);
        }

        if (choiceNoButton != null)
        {
            choiceNoButton.onClick.RemoveListener(ClosePolicyChoicePanel);
        }

        if (choiceYesButton != null)
        {
            choiceYesButton.onClick.RemoveListener(ApplySelectedPolicy);
        }
    }

    private void OpenPolicyPanel()
    {
        BindSceneObjects();
        if (policyDefinitions.Count == 0)
        {
            LoadPolicyDefinitions();
        }

        SetActive(policyPanel, true);
        SetActive(templateRect == null ? null : templateRect.gameObject, false);
        PopulateAvailablePolicies();
    }

    private void ClosePolicyPanel()
    {
        SetActive(policyPanel, false);
        SetActive(policyChoicePanel, false);
    }

    private void ClosePolicyChoicePanel()
    {
        SetActive(policyChoicePanel, false);
        selectedPolicy = null;
    }

    private void ApplySelectedPolicy()
    {
        if (selectedPolicy == null || usedPolicyNames.Contains(selectedPolicy.Name))
        {
            ClosePolicyChoicePanel();
            PopulateAvailablePolicies();
            return;
        }

        int currentYear = stageManager == null ? 0 : stageManager.CurrentYear;
        ActivePolicyState state = new ActivePolicyState();
        state.PolicyName = selectedPolicy.Name;
        state.AppliedYear = currentYear;
        state.ExpireYear = currentYear + Mathf.Max(0, selectedPolicy.DurationYears);
        state.EffectToken = selectedPolicy.Effect;
        activePolicies.Add(state);
        usedPolicyNames.Add(selectedPolicy.Name);

        ClosePolicyChoicePanel();
        PopulateAvailablePolicies();
    }

    public bool IsPolicyActive(string policyName)
    {
        if (string.IsNullOrEmpty(policyName))
        {
            return false;
        }

        int currentYear = stageManager == null ? 0 : stageManager.CurrentYear;
        for (int i = 0; i < activePolicies.Count; i += 1)
        {
            ActivePolicyState state = activePolicies[i];
            if (state != null && state.PolicyName == policyName && currentYear < state.ExpireYear)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetFloatEffect(string effectKey, out float value)
    {
        value = 0f;
        if (string.IsNullOrEmpty(effectKey))
        {
            return false;
        }

        int currentYear = stageManager == null ? 0 : stageManager.CurrentYear;
        for (int i = 0; i < activePolicies.Count; i += 1)
        {
            ActivePolicyState state = activePolicies[i];
            if (state == null || currentYear >= state.ExpireYear)
            {
                continue;
            }

            if (TryParseFloatEffect(state.EffectToken, effectKey, out value))
            {
                return true;
            }
        }

        return false;
    }

    public int GetAdjustedBuildCost(StructDefinitionData definition)
    {
        if (definition == null)
        {
            return 0;
        }

        int buildCost = definition.BuildCost;
        float multiplier;
        if (StructureInvestmentState.IsHouseName(definition.Name) && TryGetFloatEffect("HouseBuildCostMultiplier", out multiplier))
        {
            buildCost = Mathf.CeilToInt(buildCost * multiplier);
        }

        return Mathf.Max(0, buildCost);
    }

    private void PopulateAvailablePolicies()
    {
        ClearPolicyItems();
        if (contentRect == null || templateRect == null)
        {
            return;
        }

        int currentYear = stageManager == null ? 0 : stageManager.CurrentYear;
        foreach (PolicyDefinitionData definition in policyDefinitions.Values)
        {
            if (definition.UnlockYear > currentYear)
            {
                continue;
            }

            if (usedPolicyNames.Contains(definition.Name))
            {
                continue;
            }

            CreatePolicyItem(definition);
        }

        ResizeContentForPolicies();
    }

    private void CreatePolicyItem(PolicyDefinitionData definition)
    {
        GameObject itemObject = Instantiate(templateRect.gameObject, contentRect);
        itemObject.name = templateRect.name + "_" + policyItems.Count;
        itemObject.SetActive(true);

        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            itemRect.anchoredPosition = new Vector2(templateRect.anchoredPosition.x, templateRect.anchoredPosition.y - policyItemSpacing * policyItems.Count);
        }

        SetText(FindText(itemObject.transform, "Statue"), definition.Name);
        SetText(FindText(itemObject.transform, "Desc"), definition.Description);
        SetText(FindText(itemObject.transform, "Need"), definition.Requirement);
        BindPolicyItemClick(itemObject, definition);
        policyItems.Add(itemObject);
    }

    private void BindPolicyItemClick(GameObject itemObject, PolicyDefinitionData definition)
    {
        Button[] buttons = itemObject.GetComponentsInChildren<Button>(true);
        if (buttons.Length == 0)
        {
            Button button = itemObject.AddComponent<Button>();
            button.onClick.AddListener(() => OpenPolicyChoicePanel(definition));
            return;
        }

        for (int i = 0; i < buttons.Length; i += 1)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OpenPolicyChoicePanel(definition));
        }
    }

    private void OpenPolicyChoicePanel(PolicyDefinitionData definition)
    {
        selectedPolicy = definition;
        SetText(choiceNameText, definition == null ? string.Empty : definition.Name);
        SetText(choiceDescText, BuildChoiceDescription(definition));
        SetActive(policyChoicePanel, true);
    }

    private string BuildChoiceDescription(PolicyDefinitionData definition)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        return definition.Description
               + "\n유효 기간: " + definition.DurationYears + "년"
               + "\n요구 능력치: " + definition.Requirement;
    }

    private bool TryParseFloatEffect(string effectToken, string effectKey, out float value)
    {
        value = 0f;
        if (string.IsNullOrEmpty(effectToken))
        {
            return false;
        }

        string[] tokens = effectToken.Split(';');
        for (int i = 0; i < tokens.Length; i += 1)
        {
            string token = tokens[i].Trim();
            if (!token.StartsWith(effectKey + "=", System.StringComparison.Ordinal))
            {
                continue;
            }

            string rawValue = token.Substring(effectKey.Length + 1).Trim();
            return float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        return false;
    }

    private void ClearPolicyItems()
    {
        for (int i = policyItems.Count - 1; i >= 0; i -= 1)
        {
            if (policyItems[i] != null)
            {
                Destroy(policyItems[i]);
            }
        }

        policyItems.Clear();
    }

    private void ResizeContentForPolicies()
    {
        if (contentRect == null || templateRect == null || policyItems.Count <= 0)
        {
            return;
        }

        float firstY = templateRect.anchoredPosition.y;
        float lastY = firstY - policyItemSpacing * (policyItems.Count - 1);
        float requiredHeight = Mathf.Abs(lastY) + templateRect.rect.height + policyItemSpacing;
        Vector2 sizeDelta = contentRect.sizeDelta;
        sizeDelta.y = Mathf.Max(sizeDelta.y, requiredHeight);
        contentRect.sizeDelta = sizeDelta;
    }

    private Button FindOrAddButton(Transform parent, string childName)
    {
        Transform child = parent == null ? null : parent.Find(childName);
        if (child == null)
        {
            return null;
        }

        Button button = child.GetComponent<Button>();
        if (button == null)
        {
            button = child.gameObject.AddComponent<Button>();
        }

        return button;
    }

    private TextMeshProUGUI FindText(Transform parent, string childName)
    {
        Transform child = parent == null ? null : parent.Find(childName);
        return child == null ? null : child.GetComponent<TextMeshProUGUI>();
    }

    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    private class ActivePolicyState
    {
        public string PolicyName;
        public int AppliedYear;
        public int ExpireYear;
        public string EffectToken;
    }
}
