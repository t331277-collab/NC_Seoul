using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StructureActionManager : MonoBehaviour
{
    private const string DefaultBuildNameTemplate = "{StruName} 건축 승인서";
    private const string DefaultBuildDescTemplate = "{InvestAmont} 만큼의 금액으로 건설을 허가함 ";
    private const string DefaultBuildYearTemplate = "{\uAC74\uC124\uC2DC\uAC04} 만큼의 시간 소모";

    [SerializeField] private string buildPanelPath = "CanBuildStruc/BuildPanel";

    private readonly List<ConstructionJob> constructionJobs = new List<ConstructionJob>();

    private StructStageManager stageManager;
    private DistrictStructurePanelManager districtPanelManager;
    private GameObject buildPanel;
    private TextMeshProUGUI buildNameText;
    private TextMeshProUGUI buildDescText;
    private TextMeshProUGUI buildYearText;
    private Button confirmBuildButton;
    private string buildNameTemplate;
    private string buildDescTemplate;
    private string buildYearTemplate;
    private GameObject selectedTarget;
    private StructDefinitionData selectedDefinition;
    private string selectedDisplayName;
    private static StructureActionManager activeInstance;

    private void Awake()
    {
        BindSceneObjects();
        SetBuildPanelActive(false);
    }

    private void OnEnable()
    {
        activeInstance = this;
        BindSceneObjects();
        if (stageManager != null)
        {
            stageManager.BeforeYearProduction -= HandleBeforeYearProduction;
            stageManager.BeforeYearProduction += HandleBeforeYearProduction;
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
        if (buildPanel == null || !buildPanel.activeSelf)
        {
            return false;
        }

        SetBuildPanelActive(false);
        selectedTarget = null;
        selectedDefinition = null;
        selectedDisplayName = null;
        return true;
    }

    public void OpenBuildPanel(GameObject targetObject, StructDefinitionData definition, string displayName, DistrictStructurePanelManager sourcePanelManager)
    {
        if (targetObject == null || definition == null)
        {
            return;
        }

        BindSceneObjects();
        selectedTarget = targetObject;
        selectedDefinition = definition;
        selectedDisplayName = string.IsNullOrEmpty(displayName) ? definition.DisplayName : displayName;
        districtPanelManager = sourcePanelManager == null ? districtPanelManager : sourcePanelManager;

        SetText(buildNameText, ReplaceToken(buildNameTemplate, "{StruName}", selectedDisplayName));
        SetText(buildDescText, ReplaceToken(buildDescTemplate, "{InvestAmont}", FormatMoneyK(definition.BuildCost)));
        SetText(buildYearText, ReplaceToken(buildYearTemplate, "{\uAC74\uC124\uC2DC\uAC04}", definition.BuildYears.ToString()));

        if (confirmBuildButton != null)
        {
            confirmBuildButton.onClick.RemoveListener(ConfirmBuild);
            confirmBuildButton.onClick.AddListener(ConfirmBuild);
        }

        SetBuildPanelActive(true);
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
            Debug.LogWarning("Not enough money to build " + selectedDefinition.Name + ".");
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

    private void SetBuildPanelActive(bool isActive)
    {
        if (buildPanel != null)
        {
            buildPanel.SetActive(isActive);
        }
    }

    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
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
}
