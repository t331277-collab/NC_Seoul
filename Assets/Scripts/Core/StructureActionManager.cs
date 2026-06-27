using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StructureActionManager : MonoBehaviour
{
    private const string DefaultBuildNameTemplate = "{StruName} 건축 승인서";
    private const string DefaultBuildDescTemplate = "{InvestAmont} 만큼의 금액으로 건설을 허가함 ";
    private const string DefaultBuildYearTemplate = "{\uAC74\uC124\uC2DC\uAC04} 만큼의 시간 소모";
    private const string DefaultInvestNameTemplate = "{StruName} 지원 공문";
    private const string DefaultInvestDescTemplate = "{InvestAmont} 만큼의 금액을 지원.";
    private const string DefaultRepairNameTemplate = "{StruName} 보수 명령서";
    private const string DefaultRepairDescTemplate = "{InvestAmont} 만큼의 금액으로 해당 건물을 보수를 명함";
    private const string DefaultDestroyNameTemplate = "{StruName} 철거 명령서";
    private const string DefaultDestroyDescTemplate = "{InvestAmont} 만큼의 금액으로 해당 건물의 철거를 명령함";
    private const float InvestMultiplier = 1.5f;

    [SerializeField] private string buildPanelPath = "CanBuildStruc/BuildPanel";
    [SerializeField] private string investPanelPath = "CurStruc/StruContainer/InvestPanel";
    [SerializeField] private string repairPanelPath = "CurStruc/StruContainer/RepairPanel";
    [SerializeField] private string destroyPanelPath = "CurStruc/StruContainer/DestPanel";

    private readonly List<ConstructionJob> constructionJobs = new List<ConstructionJob>();
    private readonly List<InvestmentBoost> investmentBoosts = new List<InvestmentBoost>();

    private StructStageManager stageManager;
    private DistrictStructurePanelManager districtPanelManager;
    private GameObject buildPanel;
    private TextMeshProUGUI buildNameText;
    private TextMeshProUGUI buildDescText;
    private TextMeshProUGUI buildYearText;
    private Button confirmBuildButton;
    private GameObject investPanel;
    private TextMeshProUGUI investNameText;
    private TextMeshProUGUI investDescText;
    private Button confirmInvestButton;
    private GameObject repairPanel;
    private TextMeshProUGUI repairNameText;
    private TextMeshProUGUI repairDescText;
    private Button confirmRepairButton;
    private GameObject destroyPanel;
    private TextMeshProUGUI destroyNameText;
    private TextMeshProUGUI destroyDescText;
    private Button confirmDestroyButton;
    private string buildNameTemplate;
    private string buildDescTemplate;
    private string buildYearTemplate;
    private string investNameTemplate;
    private string investDescTemplate;
    private string repairNameTemplate;
    private string repairDescTemplate;
    private string destroyNameTemplate;
    private string destroyDescTemplate;
    private GameObject selectedTarget;
    private StructDefinitionData selectedDefinition;
    private string selectedDisplayName;
    private static StructureActionManager activeInstance;

    private void Awake()
    {
        BindSceneObjects();
        SetBuildPanelActive(false);
        SetStructureActionPanelsActive(false);
    }

    private void OnEnable()
    {
        activeInstance = this;
        BindSceneObjects();
        if (stageManager != null)
        {
            stageManager.BeforeYearProduction -= HandleBeforeYearProduction;
            stageManager.BeforeYearProduction += HandleBeforeYearProduction;
            stageManager.AfterYearProduction -= HandleAfterYearProduction;
            stageManager.AfterYearProduction += HandleAfterYearProduction;
        }
    }

    private void OnDisable()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }

        if (stageManager != null)
        {
            stageManager.BeforeYearProduction -= HandleBeforeYearProduction;
            stageManager.AfterYearProduction -= HandleAfterYearProduction;
        }
    }

    public static bool TryCloseOpenBuildPanel()
    {
        if (activeInstance == null)
        {
            activeInstance = FindObjectOfType<StructureActionManager>();
        }

        if (activeInstance == null)
        {
            return false;
        }

        return activeInstance.TryCloseBuildPanel();
    }

    public bool TryCloseBuildPanel()
    {
        BindSceneObjects();
        bool closedAny = false;

        if (buildPanel != null && buildPanel.activeSelf)
        {
            SetBuildPanelActive(false);
            closedAny = true;
        }

        if (CloseOpenStructureActionPanel())
        {
            closedAny = true;
        }

        if (!closedAny)
        {
            return false;
        }

        ClearSelection();
        return true;
    }

    public void OpenBuildPanel(GameObject targetObject, StructDefinitionData definition, string displayName, DistrictStructurePanelManager sourcePanelManager)
    {
        if (targetObject == null || definition == null)
        {
            return;
        }

        BindSceneObjects();
        SelectStructure(targetObject, definition, displayName, sourcePanelManager);

        SetText(buildNameText, ReplaceToken(buildNameTemplate, "{StruName}", selectedDisplayName));
        SetText(buildDescText, ReplaceToken(buildDescTemplate, "{InvestAmont}", FormatMoneyK(definition.BuildCost)));
        SetText(buildYearText, ReplaceToken(buildYearTemplate, "{\uAC74\uC124\uC2DC\uAC04}", definition.BuildYears.ToString()));

        if (confirmBuildButton != null)
        {
            confirmBuildButton.onClick.RemoveListener(ConfirmBuild);
            confirmBuildButton.onClick.AddListener(ConfirmBuild);
        }

        SetStructureActionPanelsActive(false);
        SetBuildPanelActive(true);
    }

    public void OpenInvestPanel(GameObject targetObject, StructDefinitionData definition, string displayName, DistrictStructurePanelManager sourcePanelManager)
    {
        BindSceneObjects();
        OpenStructureActionPanel(targetObject, definition, displayName, sourcePanelManager, investPanel, investNameText, investDescText, investNameTemplate, investDescTemplate, definition == null ? 0 : definition.InvestCost, confirmInvestButton, ConfirmInvest);
    }

    public void OpenRepairPanel(GameObject targetObject, StructDefinitionData definition, string displayName, DistrictStructurePanelManager sourcePanelManager)
    {
        BindSceneObjects();
        OpenStructureActionPanel(targetObject, definition, displayName, sourcePanelManager, repairPanel, repairNameText, repairDescText, repairNameTemplate, repairDescTemplate, definition == null ? 0 : definition.RepairCost, confirmRepairButton, ConfirmRepair);
    }

    public void OpenDestroyPanel(GameObject targetObject, StructDefinitionData definition, string displayName, DistrictStructurePanelManager sourcePanelManager)
    {
        BindSceneObjects();
        OpenStructureActionPanel(targetObject, definition, displayName, sourcePanelManager, destroyPanel, destroyNameText, destroyDescText, destroyNameTemplate, destroyDescTemplate, definition == null ? 0 : definition.DestroyCost, confirmDestroyButton, ConfirmDestroy);
    }

    public bool IsConstructionPending(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return false;
        }

        for (int i = 0; i < constructionJobs.Count; i += 1)
        {
            if (constructionJobs[i].TargetObject == targetObject)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetProductionMultiplier(GameObject targetObject, out float multiplier)
    {
        multiplier = 1f;
        if (targetObject == null)
        {
            return false;
        }

        for (int i = 0; i < investmentBoosts.Count; i += 1)
        {
            InvestmentBoost boost = investmentBoosts[i];
            if (boost.TargetObject == targetObject && boost.RemainingYears > 0)
            {
                multiplier = boost.Multiplier;
                return true;
            }
        }

        return false;
    }

    private void OpenStructureActionPanel(GameObject targetObject, StructDefinitionData definition, string displayName, DistrictStructurePanelManager sourcePanelManager, GameObject panel, TextMeshProUGUI nameText, TextMeshProUGUI descText, string nameTemplate, string descTemplate, int cost, Button confirmButton, UnityEngine.Events.UnityAction confirmAction)
    {
        if (targetObject == null || definition == null || panel == null)
        {
            return;
        }

        BindSceneObjects();
        SelectStructure(targetObject, definition, displayName, sourcePanelManager);
        SetText(nameText, ReplaceToken(nameTemplate, "{StruName}", selectedDisplayName));
        SetText(descText, ReplaceToken(descTemplate, "{InvestAmont}", FormatMoneyK(cost)));

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(confirmAction);
            confirmButton.onClick.AddListener(confirmAction);
        }

        SetBuildPanelActive(false);
        SetStructureActionPanelsActive(false);
        panel.SetActive(true);
    }

    private void SelectStructure(GameObject targetObject, StructDefinitionData definition, string displayName, DistrictStructurePanelManager sourcePanelManager)
    {
        selectedTarget = targetObject;
        selectedDefinition = definition;
        selectedDisplayName = string.IsNullOrEmpty(displayName) ? definition.DisplayName : displayName;
        districtPanelManager = sourcePanelManager == null ? districtPanelManager : sourcePanelManager;
    }

    private void ConfirmBuild()
    {
        if (selectedTarget == null || selectedDefinition == null || stageManager == null)
        {
            return;
        }

        if (IsConstructionPending(selectedTarget))
        {
            SetBuildPanelActive(false);
            return;
        }

        if (!stageManager.TrySpendMoney(selectedDefinition.BuildCost))
        {
            Debug.LogWarning("Not enough money to build " + selectedDefinition.Name + "." );
            return;
        }

        ConstructionJob job = new ConstructionJob();
        job.RegionPath = selectedTarget.transform.parent == null ? string.Empty : GetPath(selectedTarget.transform.parent);
        job.StructureKey = selectedDefinition.Name;
        job.TargetObject = selectedTarget;
        job.RemainingYears = Mathf.Max(1, selectedDefinition.BuildYears);
        constructionJobs.Add(job);

        SetBuildPanelActive(false);
        RefreshLinkedUi();
    }

    private void ConfirmInvest()
    {
        if (selectedTarget == null || selectedDefinition == null || stageManager == null)
        {
            return;
        }

        if (!stageManager.TrySpendMoney(selectedDefinition.InvestCost))
        {
            Debug.LogWarning("Not enough money to invest " + selectedDefinition.Name + "." );
            return;
        }

        InvestmentBoost boost = FindInvestmentBoost(selectedTarget);
        if (boost == null)
        {
            boost = new InvestmentBoost();
            boost.TargetObject = selectedTarget;
            investmentBoosts.Add(boost);
        }

        boost.RemainingYears = Random.Range(1, 6);
        boost.Multiplier = InvestMultiplier;
        SetStructureActionPanelsActive(false);
        RefreshLinkedUi();
    }

    private void ConfirmRepair()
    {
        if (selectedDefinition != null)
        {
            Debug.Log("Repair is reserved as dummy data for " + selectedDefinition.Name + "." );
        }

        SetStructureActionPanelsActive(false);
        ClearSelection();
    }

    private void ConfirmDestroy()
    {
        if (selectedTarget == null || selectedDefinition == null || stageManager == null)
        {
            return;
        }

        if (!stageManager.TrySpendMoney(selectedDefinition.DestroyCost))
        {
            Debug.LogWarning("Not enough money to destroy " + selectedDefinition.Name + "." );
            return;
        }

        selectedTarget.SetActive(false);
        RemoveInvestmentBoost(selectedTarget);
        SetStructureActionPanelsActive(false);
        ClearSelection();
        RefreshLinkedUi();
    }

    private void HandleBeforeYearProduction(int currentYear)
    {
        bool completedAny = false;
        for (int i = constructionJobs.Count - 1; i >= 0; i -= 1)
        {
            ConstructionJob job = constructionJobs[i];
            job.RemainingYears -= 1;
            if (job.RemainingYears <= 0)
            {
                if (job.TargetObject != null)
                {
                    job.TargetObject.SetActive(true);
                }

                constructionJobs.RemoveAt(i);
                completedAny = true;
            }
        }

        if (completedAny)
        {
            RefreshLinkedUi();
        }
    }

    private void HandleAfterYearProduction(int currentYear)
    {
        bool changed = false;
        for (int i = investmentBoosts.Count - 1; i >= 0; i -= 1)
        {
            investmentBoosts[i].RemainingYears -= 1;
            if (investmentBoosts[i].RemainingYears <= 0 || investmentBoosts[i].TargetObject == null)
            {
                investmentBoosts.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
        {
            RefreshLinkedUi();
        }
    }

    private void BindSceneObjects()
    {
        stageManager = GetComponent<StructStageManager>();
        districtPanelManager = GetComponent<DistrictStructurePanelManager>();

        Transform buildPanelTransform = transform.Find(buildPanelPath);
        if (buildPanelTransform != null)
        {
            buildPanel = buildPanelTransform.gameObject;
            buildNameText = FindText(buildPanelTransform, "StruName");
            buildDescText = FindText(buildPanelTransform, "Desc");
            buildYearText = FindText(buildPanelTransform, "Year");
            Transform buttonTransform = buildPanelTransform.Find("BuildBtn");
            confirmBuildButton = buttonTransform == null ? null : buttonTransform.GetComponent<Button>();
        }

        BindActionPanel(investPanelPath, "InvestBtn", ref investPanel, ref investNameText, ref investDescText, ref confirmInvestButton);
        BindActionPanel(repairPanelPath, "InvestBtn", ref repairPanel, ref repairNameText, ref repairDescText, ref confirmRepairButton);
        BindActionPanel(destroyPanelPath, "DestBtn", ref destroyPanel, ref destroyNameText, ref destroyDescText, ref confirmDestroyButton);

        if (string.IsNullOrEmpty(buildNameTemplate) || !buildNameTemplate.Contains("{StruName}"))
        {
            buildNameTemplate = DefaultBuildNameTemplate;
        }

        if (string.IsNullOrEmpty(buildDescTemplate) || !buildDescTemplate.Contains("{InvestAmont}"))
        {
            buildDescTemplate = DefaultBuildDescTemplate;
        }

        if (string.IsNullOrEmpty(buildYearTemplate) || !buildYearTemplate.Contains("{\uAC74\uC124\uC2DC\uAC04}"))
        {
            buildYearTemplate = DefaultBuildYearTemplate;
        }

        if (string.IsNullOrEmpty(investNameTemplate) || !investNameTemplate.Contains("{StruName}"))
        {
            investNameTemplate = ReadTemplate(investNameText, DefaultInvestNameTemplate);
        }

        if (string.IsNullOrEmpty(investDescTemplate) || !investDescTemplate.Contains("{InvestAmont}"))
        {
            investDescTemplate = ReadTemplate(investDescText, DefaultInvestDescTemplate);
        }

        if (string.IsNullOrEmpty(repairNameTemplate) || !repairNameTemplate.Contains("{StruName}"))
        {
            repairNameTemplate = ReadTemplate(repairNameText, DefaultRepairNameTemplate);
        }

        if (string.IsNullOrEmpty(repairDescTemplate) || !repairDescTemplate.Contains("{InvestAmont}"))
        {
            repairDescTemplate = ReadTemplate(repairDescText, DefaultRepairDescTemplate);
        }

        if (string.IsNullOrEmpty(destroyNameTemplate) || !destroyNameTemplate.Contains("{StruName}"))
        {
            destroyNameTemplate = ReadTemplate(destroyNameText, DefaultDestroyNameTemplate);
        }

        if (string.IsNullOrEmpty(destroyDescTemplate) || !destroyDescTemplate.Contains("{InvestAmont}"))
        {
            destroyDescTemplate = ReadTemplate(destroyDescText, DefaultDestroyDescTemplate);
        }
    }

    private void BindActionPanel(string panelPath, string buttonName, ref GameObject panel, ref TextMeshProUGUI nameText, ref TextMeshProUGUI descText, ref Button button)
    {
        Transform panelTransform = transform.Find(panelPath);
        if (panelTransform == null)
        {
            return;
        }

        panel = panelTransform.gameObject;
        nameText = FindText(panelTransform, "StruName");
        descText = FindText(panelTransform, "Desc");
        Transform buttonTransform = panelTransform.Find(buttonName);
        button = buttonTransform == null ? null : buttonTransform.GetComponent<Button>();
    }

    private TextMeshProUGUI FindText(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform child = parent.Find(childName);
        return child == null ? null : child.GetComponent<TextMeshProUGUI>();
    }

    private void RefreshLinkedUi()
    {
        if (districtPanelManager != null)
        {
            districtPanelManager.RefreshVisiblePanels();
        }

        if (stageManager != null)
        {
            stageManager.RefreshPendingValues();
        }
    }

    private bool CloseOpenStructureActionPanel()
    {
        bool closedAny = false;
        if (investPanel != null && investPanel.activeSelf)
        {
            investPanel.SetActive(false);
            closedAny = true;
        }

        if (repairPanel != null && repairPanel.activeSelf)
        {
            repairPanel.SetActive(false);
            closedAny = true;
        }

        if (destroyPanel != null && destroyPanel.activeSelf)
        {
            destroyPanel.SetActive(false);
            closedAny = true;
        }

        return closedAny;
    }

    private void SetBuildPanelActive(bool isActive)
    {
        if (buildPanel != null)
        {
            buildPanel.SetActive(isActive);
        }
    }

    private void SetStructureActionPanelsActive(bool isActive)
    {
        if (investPanel != null)
        {
            investPanel.SetActive(isActive);
        }

        if (repairPanel != null)
        {
            repairPanel.SetActive(isActive);
        }

        if (destroyPanel != null)
        {
            destroyPanel.SetActive(isActive);
        }
    }

    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private string ReadTemplate(TextMeshProUGUI text, string fallback)
    {
        if (text == null || string.IsNullOrEmpty(text.text))
        {
            return fallback;
        }

        if (fallback.Contains("{StruName}") && !text.text.Contains("{StruName}"))
        {
            return fallback;
        }

        if (fallback.Contains("{InvestAmont}") && !text.text.Contains("{InvestAmont}"))
        {
            return fallback;
        }

        return text.text;
    }

    private string ReplaceToken(string template, string token, string value)
    {
        if (string.IsNullOrEmpty(template))
        {
            return value;
        }

        return template.Replace(token, value);
    }

    private string FormatMoneyK(int amount)
    {
        return amount.ToString("N0") + "K";
    }

    private InvestmentBoost FindInvestmentBoost(GameObject targetObject)
    {
        for (int i = 0; i < investmentBoosts.Count; i += 1)
        {
            if (investmentBoosts[i].TargetObject == targetObject)
            {
                return investmentBoosts[i];
            }
        }

        return null;
    }

    private void RemoveInvestmentBoost(GameObject targetObject)
    {
        for (int i = investmentBoosts.Count - 1; i >= 0; i -= 1)
        {
            if (investmentBoosts[i].TargetObject == targetObject)
            {
                investmentBoosts.RemoveAt(i);
            }
        }
    }

    private void ClearSelection()
    {
        selectedTarget = null;
        selectedDefinition = null;
        selectedDisplayName = null;
    }

    private string GetPath(Transform target)
    {
        List<string> parts = new List<string>();
        while (target != null)
        {
            parts.Add(target.name);
            target = target.parent;
        }

        parts.Reverse();
        return string.Join("/", parts.ToArray());
    }

    private class ConstructionJob
    {
        public string RegionPath;
        public string StructureKey;
        public GameObject TargetObject;
        public int RemainingYears;
    }

    private class InvestmentBoost
    {
        public GameObject TargetObject;
        public int RemainingYears;
        public float Multiplier;
    }
}
