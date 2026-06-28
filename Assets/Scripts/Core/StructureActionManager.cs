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
    private static readonly int[] HouseInvestmentCosts = new int[] { 80, 100, 125, 155, 195, 250, 320, 410, 520, 660, 850, 1100, 1400, 1800, 2300 };
    private const string VisualRootName = "VisualRoot";

    [SerializeField] private string buildPanelPath = "CanBuildStruc/BuildPanel";
    [SerializeField] private string investPanelPath = "CurStruc/StruContainer/InvestPanel";
    [SerializeField] private string repairPanelPath = "CurStruc/StruContainer/RepairPanel";
    [SerializeField] private string destroyPanelPath = "CurStruc/StruContainer/DestPanel";
    [SerializeField] private GameObject newHousePrefab;
    [SerializeField] private GameObject apartmentPrefab;
    [SerializeField] private GameObject newSchoolPrefab;
    [SerializeField] private GameObject newDistrictPrefab;
    [SerializeField] private GameObject newUniversityPrefab;
    [SerializeField] private GameObject structingPrefab;

    private readonly List<ConstructionJob> constructionJobs = new List<ConstructionJob>();
    private readonly List<DemolitionJob> demolitionJobs = new List<DemolitionJob>();
    private readonly List<InvestmentBoost> investmentBoosts = new List<InvestmentBoost>();

    private StructStageManager stageManager;
    private DistrictStructurePanelManager districtPanelManager;
    private ToastPopupManager toastPopupManager;
    private InfoNotificationManager infoNotificationManager;
    private UIManager uiManager;
    private GameObject buildPanel;
    private TextMeshProUGUI buildNameText;
    private TextMeshProUGUI buildDescText;
    private TextMeshProUGUI buildYearText;
    private Button confirmBuildButton;
    private GameObject investPanel;
    private TextMeshProUGUI investNameText;
    private TextMeshProUGUI investDescText;
    private TextMeshProUGUI investExplainText;
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
        BindInvestmentVisualPrefabs();
        SetBuildPanelActive(false);
        SetStructureActionPanelsActive(false);
    }

    private void OnEnable()
    {
        activeInstance = this;
        BindSceneObjects();
        BindInvestmentVisualPrefabs();
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
        SetText(buildDescText, BuildBuildDescription(definition));
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
        StructureInvestmentState investmentState = GetInvestmentState(targetObject);
        int investCost = definition == null ? 0 : GetInvestmentCost(definition, investmentState);
        OpenStructureActionPanel(targetObject, definition, displayName, sourcePanelManager, investPanel, investNameText, investDescText, investNameTemplate, investDescTemplate, investCost, confirmInvestButton, ConfirmInvest);
        SetInvestmentExplainText(targetObject, definition);
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

    public string GetInvestmentStatusText(GameObject targetObject, StructDefinitionData definition)
    {
        StructureInvestmentState investmentState = GetInvestmentState(targetObject);
        if (investmentState == null || investmentState.StructureKind == StructureInvestmentState.InvestmentStructureKind.Unknown)
        {
            return string.Empty;
        }

        int maxSuccess = Mathf.Max(0, investmentState.maxSuccessfulInvestments);
        if (maxSuccess <= 0)
        {
            return string.Empty;
        }

        string status = "강화 " + investmentState.successfulInvestmentCount + "/" + maxSuccess;
        if (investmentState.hasPendingInvestment)
        {
            return status + " | 투자 진행 중";
        }

        if (investmentState.IsAtSuccessLimit)
        {
            return status + " | 최대 강화";
        }

        int nextCost = GetInvestmentCost(definition, investmentState);
        int chancePercent = Mathf.RoundToInt(GetInvestmentSuccessChance(definition, investmentState) * 100f);
        return status + " | 다음 " + FormatMoneyK(nextCost) + " | 성공 " + chancePercent + "%";
    }

    private void SetInvestmentExplainText(GameObject targetObject, StructDefinitionData definition)
    {
        if (investExplainText == null)
        {
            return;
        }

        string status = GetInvestmentStatusText(targetObject, definition);
        if (string.IsNullOrEmpty(status))
        {
            SetText(investExplainText, string.Empty);
            return;
        }

        SetText(investExplainText, "강화 상태: " + status);
    }

    public bool CanInvestInStructure(GameObject targetObject)
    {
        StructureInvestmentState investmentState = GetInvestmentState(targetObject);
        return investmentState != null &&
               investmentState.StructureKind != StructureInvestmentState.InvestmentStructureKind.Unknown &&
               !investmentState.hasPendingInvestment &&
               !investmentState.IsAtSuccessLimit;
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

    public bool IsDemolitionPending(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return false;
        }

        for (int i = 0; i < demolitionJobs.Count; i += 1)
        {
            if (demolitionJobs[i].TargetObject == targetObject)
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

        StructureInvestmentState investmentState = targetObject.GetComponent<StructureInvestmentState>();
        if (investmentState != null)
        {
            multiplier = investmentState.RefreshCurrentStatMultiplier();
            if (multiplier > 1f)
            {
                return true;
            }
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

        bool lacksScience = stageManager.Science < selectedDefinition.UnlockScience;
        bool lacksMoney = stageManager.Money < selectedDefinition.BuildCost;
        if (lacksScience || lacksMoney)
        {
            ShowBuildRequirementShortageToast(lacksScience, lacksMoney);
            Debug.LogWarning("Not enough requirements to build " + selectedDefinition.Name + ". CurrentScience=" + stageManager.Science + ", RequiredScience=" + selectedDefinition.UnlockScience + ", CurrentMoney=" + stageManager.Money + ", RequiredMoney=" + selectedDefinition.BuildCost + ".");
            return;
        }

        if (!stageManager.TrySpendMoney(selectedDefinition.BuildCost))
        {
            ShowMoneyShortageToast();
            Debug.LogWarning("Not enough money to build " + selectedDefinition.Name + "." );
            return;
        }

        ConstructionJob job = new ConstructionJob();
        job.RegionPath = selectedTarget.transform.parent == null ? string.Empty : GetPath(selectedTarget.transform.parent);
        job.RegionName = GetRegionName(selectedTarget);
        job.RegionDisplayName = GetRegionDisplayName(job.RegionName);
        job.RegionTransform = FindRegionTransform(selectedTarget.transform);
        job.StructureKey = selectedDefinition.Name;
        job.StructureDisplayName = selectedDisplayName;
        job.TargetObject = selectedTarget;
        job.RemainingYears = Mathf.Max(1, selectedDefinition.BuildYears);
        job.WorkVisualObject = CreateConstructionWorkVisual(selectedTarget);
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

        StructureInvestmentState investmentState = GetInvestmentState(selectedTarget);
        if (investmentState == null || investmentState.StructureKind == StructureInvestmentState.InvestmentStructureKind.Unknown)
        {
            Debug.LogWarning("Investment state is missing or unsupported for " + selectedDefinition.Name + "." );
            return;
        }

        if (investmentState.hasPendingInvestment)
        {
            Debug.LogWarning("Investment is already pending for " + selectedDefinition.Name + "." );
            return;
        }

        if (investmentState.IsAtSuccessLimit)
        {
            Debug.LogWarning("Investment success limit reached for " + selectedDefinition.Name + "." );
            return;
        }

        int investCost = GetInvestmentCost(selectedDefinition, investmentState);
        if (!stageManager.TrySpendMoney(investCost))
        {
            ShowMoneyShortageToast();
            Debug.LogWarning("Not enough money to invest " + selectedDefinition.Name + "." );
            return;
        }

        investmentState.hasPendingInvestment = true;
        investmentState.pendingResolveYear = stageManager.CurrentYear + 1;
        investmentState.pendingCost = investCost;
        investmentState.pendingSuccessChance = GetInvestmentSuccessChance(selectedDefinition, investmentState);
        investmentState.pendingRegionName = GetRegionName(selectedTarget);
        investmentState.pendingRegionDisplayName = GetRegionDisplayName(investmentState.pendingRegionName);
        investmentState.pendingStructureDisplayName = selectedDisplayName;

        SetStructureActionPanelsActive(false);
        RefreshLinkedUi();
    }

    private void ConfirmRepair()
    {
        if (selectedTarget == null || selectedDefinition == null || stageManager == null)
        {
            return;
        }

        if (!stageManager.TrySpendMoney(selectedDefinition.RepairCost))
        {
            ShowMoneyShortageToast();
            Debug.LogWarning("Not enough money to repair " + selectedDefinition.Name + "." );
            return;
        }

        Debug.Log("Repair is reserved as dummy data for " + selectedDefinition.Name + "." );
        SetStructureActionPanelsActive(false);
        ClearSelection();
    }

    private void ConfirmDestroy()
    {
        if (selectedTarget == null || selectedDefinition == null || stageManager == null)
        {
            return;
        }

        if (IsDemolitionPending(selectedTarget))
        {
            SetStructureActionPanelsActive(false);
            return;
        }

        if (!stageManager.TrySpendMoney(selectedDefinition.DestroyCost))
        {
            ShowMoneyShortageToast();
            Debug.LogWarning("Not enough money to destroy " + selectedDefinition.Name + "." );
            return;
        }

        DemolitionJob job = new DemolitionJob();
        job.TargetObject = selectedTarget;
        job.RegionName = GetRegionName(selectedTarget);
        job.RegionDisplayName = GetRegionDisplayName(job.RegionName);
        job.RegionTransform = FindRegionTransform(selectedTarget == null ? null : selectedTarget.transform);
        job.StructureDisplayName = selectedDisplayName;
        job.RemainingYears = 1;
        job.WorkVisualObject = CreateConstructionWorkVisual(selectedTarget);
        demolitionJobs.Add(job);

        selectedTarget.SetActive(false);
        RemoveInvestmentBoost(selectedTarget);
        SetStructureActionPanelsActive(false);
        if (TutorialDialogueRunner.Instance != null && selectedDefinition != null)
        {
            TutorialDialogueRunner.Instance.NotifyStructureDemolished(selectedDefinition.Name);
        }
        ClearSelection();
        RefreshLinkedUi();
    }

    private void ShowMoneyShortageToast()
    {
        if (toastPopupManager == null)
        {
            BindSceneObjects();
        }

        if (toastPopupManager != null)
        {
            toastPopupManager.ShowMoneyShortage();
        }
    }

    private void ShowBuildRequirementShortageToast(bool lacksScience, bool lacksMoney)
    {
        if (toastPopupManager == null)
        {
            BindSceneObjects();
        }

        if (toastPopupManager == null)
        {
            return;
        }

        if (lacksScience && lacksMoney)
        {
            toastPopupManager.ShowScienceAndMoneyShortage();
            return;
        }

        if (lacksScience)
        {
            toastPopupManager.ShowScienceShortage();
            return;
        }

        toastPopupManager.ShowMoneyShortage();
    }

    private StructureInvestmentState GetInvestmentState(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return null;
        }

        StructureInvestmentState investmentState = targetObject.GetComponent<StructureInvestmentState>();
        if (investmentState == null)
        {
            investmentState = targetObject.AddComponent<StructureInvestmentState>();
        }

        investmentState.ConfigureForStructureName(targetObject.name);
        return investmentState;
    }

    private int GetInvestmentCost(StructDefinitionData definition, StructureInvestmentState investmentState)
    {
        if (definition == null)
        {
            return 0;
        }

        if (investmentState == null)
        {
            return definition.InvestCost;
        }

        int successIndex = Mathf.Max(0, investmentState.successfulInvestmentCount);
        if (investmentState.StructureKind == StructureInvestmentState.InvestmentStructureKind.House)
        {
            return HouseInvestmentCosts[Mathf.Clamp(successIndex, 0, HouseInvestmentCosts.Length - 1)];
        }

        if (investmentState.StructureKind == StructureInvestmentState.InvestmentStructureKind.CommonFacility)
        {
            int costIndex = Mathf.Clamp(successIndex, 0, Mathf.Min(9, HouseInvestmentCosts.Length - 1));
            return Mathf.Max(definition.InvestCost, HouseInvestmentCosts[costIndex]);
        }

        if (investmentState.StructureKind == StructureInvestmentState.InvestmentStructureKind.UniqueStructure)
        {
            int resourceTotal = GetResourceTotal(definition);
            int scaledCost = definition.InvestCost + resourceTotal * 20 * (successIndex + 1);
            return Mathf.Max(definition.InvestCost, scaledCost);
        }

        return definition.InvestCost;
    }

    private float GetInvestmentSuccessChance(StructDefinitionData definition, StructureInvestmentState investmentState)
    {
        if (NCSeoulDebug.Debug.ForceInvestmentSuccessChance)
        {
            return 1f;
        }

        if (definition == null || investmentState == null || stageManager == null)
        {
            return 0f;
        }

        int science = stageManager.Science;
        if (investmentState.StructureKind == StructureInvestmentState.InvestmentStructureKind.House)
        {
            return GetHouseInvestmentSuccessChance(science, investmentState.successfulInvestmentCount);
        }

        if (investmentState.StructureKind == StructureInvestmentState.InvestmentStructureKind.CommonFacility)
        {
            return GetCommonFacilitySuccessChance(science, investmentState.successfulInvestmentCount, GetResourceTotal(definition));
        }

        if (investmentState.StructureKind == StructureInvestmentState.InvestmentStructureKind.UniqueStructure)
        {
            return GetUniqueStructureSuccessChance(science, definition);
        }

        return 0f;
    }

    private float GetHouseInvestmentSuccessChance(int science, int successCount)
    {
        if (successCount < 5)
        {
            return Mathf.Clamp(0.30f + science * 0.00028f, 0.30f, 0.75f);
        }

        if (successCount < 10)
        {
            return Mathf.Clamp(0.08f + science * 0.00036f, 0.10f, 0.88f);
        }

        return Mathf.Clamp(0.05f + science * 0.00022f, 0.08f, 0.65f);
    }

    private float GetCommonFacilitySuccessChance(int science, int successCount, int resourceTotal)
    {
        float resourcePenalty = Mathf.Clamp(resourceTotal * 0.01f, 0f, 0.20f);
        if (successCount < 5)
        {
            return Mathf.Clamp(0.25f - resourcePenalty + science * 0.00024f, 0.12f, 0.70f);
        }

        return Mathf.Clamp(0.06f - resourcePenalty * 0.5f + science * 0.00030f, 0.08f, 0.82f);
    }

    private float GetUniqueStructureSuccessChance(int science, StructDefinitionData definition)
    {
        int resourceTotal = GetResourceTotal(definition);
        float baseChance = Mathf.Clamp(0.28f - resourceTotal * 0.015f, 0.06f, 0.25f);
        float requiredScience = 400f + resourceTotal * 260f;
        float scienceBonus = Mathf.Clamp01(science / requiredScience) * 0.65f;
        return Mathf.Clamp(baseChance + scienceBonus, 0.05f, 0.85f);
    }

    private int GetResourceTotal(StructDefinitionData definition)
    {
        if (definition == null)
        {
            return 0;
        }

        return Mathf.Max(0, definition.MoneyProduction)
            + Mathf.Max(0, definition.PeopleIncrease)
            + Mathf.Max(0, definition.ScienceIncrease)
            + Mathf.Max(0, definition.LoveIncrease)
            + Mathf.Max(0, definition.ConvenienceIncrease);
    }

    private void HandleBeforeYearProduction(int currentYear)
    {
        bool completedAny = ResolvePendingInvestments(currentYear);
        bool demolitionCompletedAny = ResolveDemolitionJobs();
        for (int i = constructionJobs.Count - 1; i >= 0; i -= 1)
        {
            ConstructionJob job = constructionJobs[i];
            job.RemainingYears -= 1;
            if (job.RemainingYears <= 0)
            {
                if (job.WorkVisualObject != null)
                {
                    DestroyInvestmentVisual(job.WorkVisualObject);
                    job.WorkVisualObject = null;
                }

                if (job.TargetObject != null)
                {
                    job.TargetObject.SetActive(true);
                }

                ShowConstructionCompleteNotification(job);
                constructionJobs.RemoveAt(i);
                completedAny = true;
            }
        }

        if (completedAny || demolitionCompletedAny)
        {
            RefreshLinkedUi();
        }
    }

    private bool ResolveDemolitionJobs()
    {
        bool completedAny = false;
        for (int i = demolitionJobs.Count - 1; i >= 0; i -= 1)
        {
            DemolitionJob job = demolitionJobs[i];
            job.RemainingYears -= 1;
            if (job.RemainingYears <= 0)
            {
                if (job.WorkVisualObject != null)
                {
                    DestroyInvestmentVisual(job.WorkVisualObject);
                    job.WorkVisualObject = null;
                }

                if (job.TargetObject != null)
                {
                    job.TargetObject.SetActive(false);
                }

                ShowDemolitionCompleteNotification(job);
                demolitionJobs.RemoveAt(i);
                completedAny = true;
            }
        }

        return completedAny;
    }

    private bool ResolvePendingInvestments(int currentYear)
    {
        StructureInvestmentState[] investmentStates = FindObjectsOfType<StructureInvestmentState>(true);
        bool resolvedAny = false;
        for (int i = 0; i < investmentStates.Length; i += 1)
        {
            StructureInvestmentState investmentState = investmentStates[i];
            if (investmentState == null || !investmentState.hasPendingInvestment || investmentState.pendingResolveYear > currentYear)
            {
                continue;
            }

            investmentState.totalInvestmentAttemptCount += 1;
            bool succeeded = Random.value <= investmentState.pendingSuccessChance;
            if (succeeded)
            {
                investmentState.successfulInvestmentCount += 1;
                ApplyInvestmentVisualMilestone(investmentState);
            }
            else
            {
                investmentState.failedInvestmentCount += 1;
            }

            investmentState.lastInvestmentSucceeded = succeeded;
            investmentState.lastResolvedYear = currentYear;
            investmentState.RefreshCurrentStatMultiplier();
            investmentState.hasPendingInvestment = false;
            investmentState.pendingResolveYear = 0;
            investmentState.pendingCost = 0;
            investmentState.pendingSuccessChance = 0f;
            ShowInvestmentNotification(investmentState, succeeded);
            investmentState.pendingRegionName = string.Empty;
            investmentState.pendingRegionDisplayName = string.Empty;
            investmentState.pendingStructureDisplayName = string.Empty;
            investmentState.ConfigureForStructureName(investmentState.gameObject.name);
            resolvedAny = true;

            Debug.Log("Investment " + (succeeded ? "succeeded" : "failed") + " for " + investmentState.gameObject.name + ". Success=" + investmentState.successfulInvestmentCount + ", Fail=" + investmentState.failedInvestmentCount + ", Year=" + currentYear + ".");
        }

        return resolvedAny;
    }

    private void ShowInvestmentNotification(StructureInvestmentState investmentState, bool succeeded)
    {
        if (investmentState == null)
        {
            return;
        }

        string status = succeeded ? "투자 성공" : "투자 실패";
        string regionName = string.IsNullOrEmpty(investmentState.pendingRegionName) ? GetRegionName(investmentState.gameObject) : investmentState.pendingRegionName;
        string regionDisplayName = string.IsNullOrEmpty(investmentState.pendingRegionDisplayName) ? GetRegionDisplayName(regionName) : investmentState.pendingRegionDisplayName;
        string structureDisplayName = string.IsNullOrEmpty(investmentState.pendingStructureDisplayName) ? investmentState.gameObject.name : investmentState.pendingStructureDisplayName;
        string locationName = FormatLocationName(regionDisplayName, structureDisplayName);
        string desc = succeeded
            ? locationName + "의 투자가 성공적으로 진행됐습니다!"
            : locationName + "의 투자가 실패했습니다.";

        ShowInfoNotification(status, desc, regionName, FindRegionTransform(investmentState.transform));
    }

    private void ShowConstructionCompleteNotification(ConstructionJob job)
    {
        if (job == null)
        {
            return;
        }

        string structureDisplayName = string.IsNullOrEmpty(job.StructureDisplayName) ? job.StructureKey : job.StructureDisplayName;
        string locationName = FormatLocationName(job.RegionDisplayName, structureDisplayName);
        bool isHouse = StructureInvestmentState.IsHouseName(job.StructureKey);
        string desc = isHouse
            ? locationName + "이 성공적으로 건설되었습니다."
            : locationName + GetSubjectParticle(structureDisplayName) + " 놀라운 자태를 뽐내며 건설되었습니다!";

        ShowInfoNotification("건설 완료", desc, job.RegionName, job.RegionTransform);
    }

    private void ShowDemolitionCompleteNotification(DemolitionJob job)
    {
        if (job == null)
        {
            return;
        }

        string structureDisplayName = string.IsNullOrEmpty(job.StructureDisplayName) ? (job.TargetObject == null ? string.Empty : job.TargetObject.name) : job.StructureDisplayName;
        string locationName = FormatLocationName(job.RegionDisplayName, structureDisplayName);
        string desc = locationName + GetSubjectParticle(structureDisplayName) + " 철거되었습니다.";

        ShowInfoNotification("철거 완료", desc, job.RegionName, job.RegionTransform);
    }

    private void ShowInfoNotification(string status, string desc, string regionName, Transform regionTransform)
    {
        if (infoNotificationManager == null)
        {
            BindSceneObjects();
        }

        if (infoNotificationManager != null)
        {
            infoNotificationManager.AddNotification(status, desc, regionName, regionTransform);
        }
    }

    private string FormatLocationName(string regionDisplayName, string structureDisplayName)
    {
        if (string.IsNullOrEmpty(regionDisplayName))
        {
            return structureDisplayName ?? string.Empty;
        }

        if (string.IsNullOrEmpty(structureDisplayName))
        {
            return regionDisplayName;
        }

        return regionDisplayName + "의 " + structureDisplayName;
    }

    private string GetSubjectParticle(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "이";
        }

        char last = value[value.Length - 1];
        if (last < '\uAC00' || last > '\uD7A3')
        {
            return "이";
        }

        int jongSung = (last - '\uAC00') % 28;
        return jongSung == 0 ? "가" : "이";
    }

    private string GetRegionName(GameObject targetObject)
    {
        return targetObject == null ? string.Empty : GetRegionName(targetObject.transform);
    }

    private string GetRegionName(Transform target)
    {
        Transform regionTransform = FindRegionTransform(target);
        return regionTransform == null ? string.Empty : regionTransform.name;
    }

    private string GetRegionDisplayName(string regionName)
    {
        if (uiManager == null)
        {
            uiManager = GetComponent<UIManager>();
        }

        if (uiManager != null)
        {
            return uiManager.GetRegionDisplayName(regionName);
        }

        return regionName;
    }

    private Transform FindRegionTransform(Transform target)
    {
        Transform current = target;
        while (current != null && current.parent != null)
        {
            if (current.parent.name == "Seoul")
            {
                return current;
            }

            current = current.parent;
        }

        return null;
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

    private GameObject CreateConstructionWorkVisual(GameObject targetObject)
    {
        BindInvestmentVisualPrefabs();
        if (targetObject == null || structingPrefab == null)
        {
            return null;
        }

        Transform parent = targetObject.transform.parent;
        GameObject workVisualObject = Instantiate(structingPrefab, parent);
        workVisualObject.name = structingPrefab.name;
        workVisualObject.transform.localPosition = targetObject.transform.localPosition;
        workVisualObject.transform.localRotation = targetObject.transform.localRotation;
        workVisualObject.transform.localScale = targetObject.transform.localScale;
        workVisualObject.SetActive(true);
        return workVisualObject;
    }

    private void BindInvestmentVisualPrefabs()
    {
#if UNITY_EDITOR
        if (newHousePrefab == null)
        {
            newHousePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/NewHouse.prefab");
        }

        if (apartmentPrefab == null)
        {
            apartmentPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ApartMent.prefab");
        }

        if (newSchoolPrefab == null)
        {
            newSchoolPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/NewSchool.prefab");
        }

        if (newDistrictPrefab == null)
        {
            newDistrictPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/NewDistrict.prefab");
        }

        if (newUniversityPrefab == null)
        {
            newUniversityPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/NewUniversity.prefab");
        }

        if (structingPrefab == null)
        {
            structingPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Structing.prefab");
        }
#endif
    }

    private void ApplyInvestmentVisualMilestone(StructureInvestmentState investmentState)
    {
        if (investmentState == null)
        {
            return;
        }

        int targetStage;
        GameObject prefab;
        if (!TryGetInvestmentVisualPrefab(investmentState, out targetStage, out prefab) || prefab == null)
        {
            return;
        }

        if (investmentState.modelStage == targetStage && investmentState.activeVisualInstance != null)
        {
            return;
        }

        Transform visualRoot = GetOrCreateVisualRoot(investmentState.transform);
        HideExistingVisualChildren(visualRoot, investmentState.activeVisualInstance);
        if (investmentState.activeVisualInstance != null)
        {
            DestroyInvestmentVisual(investmentState.activeVisualInstance);
            investmentState.activeVisualInstance = null;
        }

        GameObject instance = Instantiate(prefab, visualRoot);
        instance.name = prefab.name;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        investmentState.activeVisualInstance = instance;
        investmentState.modelStage = targetStage;
        Debug.Log("Investment visual upgraded for " + investmentState.gameObject.name + " to " + prefab.name + ". Stage=" + targetStage + ".");
    }

    private void DestroyInvestmentVisual(GameObject visualObject)
    {
        if (visualObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(visualObject);
        }
        else
        {
            DestroyImmediate(visualObject);
        }
    }

    private bool TryGetInvestmentVisualPrefab(StructureInvestmentState investmentState, out int targetStage, out GameObject prefab)
    {
        targetStage = 0;
        prefab = null;
        if (investmentState == null)
        {
            return false;
        }

        StructureInvestmentState.InvestmentStructureKind kind = investmentState.StructureKind;
        int successCount = investmentState.successfulInvestmentCount;
        if (kind == StructureInvestmentState.InvestmentStructureKind.House)
        {
            if (successCount >= 10)
            {
                targetStage = 2;
                prefab = apartmentPrefab;
                return prefab != null;
            }

            if (successCount >= 5)
            {
                targetStage = 1;
                prefab = newHousePrefab;
                return prefab != null;
            }
        }

        if (kind == StructureInvestmentState.InvestmentStructureKind.CommonFacility && successCount >= 5)
        {
            targetStage = 1;
            if (investmentState.gameObject.name == "School")
            {
                prefab = newSchoolPrefab;
            }
            else if (investmentState.gameObject.name == "DistrictOffice")
            {
                prefab = newDistrictPrefab;
            }
            else if (investmentState.gameObject.name == "University")
            {
                prefab = newUniversityPrefab;
            }

            return prefab != null;
        }

        return false;
    }

    private Transform GetOrCreateVisualRoot(Transform structureRoot)
    {
        Transform visualRoot = structureRoot.Find(VisualRootName);
        if (visualRoot != null)
        {
            return visualRoot;
        }

        GameObject visualRootObject = new GameObject(VisualRootName);
        visualRoot = visualRootObject.transform;
        visualRoot.SetParent(structureRoot, false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;

        List<Transform> originalChildren = new List<Transform>();
        foreach (Transform child in structureRoot)
        {
            if (child != visualRoot && child.name != VisualRootName)
            {
                originalChildren.Add(child);
            }
        }

        for (int i = 0; i < originalChildren.Count; i += 1)
        {
            originalChildren[i].SetParent(visualRoot, true);
        }

        return visualRoot;
    }

    private void HideExistingVisualChildren(Transform visualRoot, GameObject activeVisualInstance)
    {
        if (visualRoot == null)
        {
            return;
        }

        foreach (Transform child in visualRoot)
        {
            if (activeVisualInstance != null && child.gameObject == activeVisualInstance)
            {
                continue;
            }

            child.gameObject.SetActive(false);
        }
    }

    private void BindSceneObjects()
    {
        stageManager = GetComponent<StructStageManager>();
        districtPanelManager = GetComponent<DistrictStructurePanelManager>();
        toastPopupManager = GetComponent<ToastPopupManager>();
        infoNotificationManager = GetComponent<InfoNotificationManager>();
        if (infoNotificationManager == null)
        {
            infoNotificationManager = gameObject.AddComponent<InfoNotificationManager>();
        }

        uiManager = GetComponent<UIManager>();
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }

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
        if (investPanel != null)
        {
            investExplainText = FindText(investPanel.transform, "Explain");
        }
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

    private string BuildBuildDescription(StructDefinitionData definition)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        string moneyDescription = ReplaceToken(buildDescTemplate, "{InvestAmont}", FormatMoneyK(definition.BuildCost));
        return moneyDescription + "\n" + definition.UnlockScience.ToString("N0") + " 기술력 요구";
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
        public string RegionName;
        public string RegionDisplayName;
        public Transform RegionTransform;
        public string StructureKey;
        public string StructureDisplayName;
        public GameObject TargetObject;
        public GameObject WorkVisualObject;
        public int RemainingYears;
    }

    private class DemolitionJob
    {
        public string RegionName;
        public string RegionDisplayName;
        public Transform RegionTransform;
        public string StructureDisplayName;
        public GameObject TargetObject;
        public GameObject WorkVisualObject;
        public int RemainingYears;
    }

    private class InvestmentBoost
    {
        public GameObject TargetObject;
        public int RemainingYears;
        public float Multiplier;
    }
}
