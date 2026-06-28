using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DistrictStructurePanelManager : MonoBehaviour
{
    private const string IgnoredStructName = "Stru_CommonSense";
    private const int HouseCapacityMultiplier = 10;
    private const float ContentTopPadding = 16f;
    private const string DistrictNameToken = "{\uC9C0\uC5ED\uC774\uB984}";
    private const string DistrictOfficeSuffix = "\uAD6C\uCCAD";

    [SerializeField] private string structDefinitionRelativePath = "Data/StructDefinition.csv";
    [SerializeField] private float itemSpacing = 235f;

    private readonly Dictionary<string, StructDefinitionData> structDefinitions = new Dictionary<string, StructDefinitionData>();
    private readonly Dictionary<string, Sprite> loadedSprites = new Dictionary<string, Sprite>();
    private readonly List<GameObject> currentItems = new List<GameObject>();
    private readonly List<GameObject> buildableItems = new List<GameObject>();

    private GameObject currentPanel;
    private GameObject buildablePanel;
    private GameObject descriptionPanel;
    private TextMeshProUGUI currentNameText;
    private TextMeshProUGUI buildableNameText;
    private TextMeshProUGUI descriptionNameText;
    private TextMeshProUGUI descriptionText;
    private TextMeshProUGUI descriptionStartYearText;
    private Image descriptionImage;
    private RectTransform currentContainer;
    private RectTransform buildableContainer;
    private RectTransform currentTemplate;
    private RectTransform buildableTemplate;
    private RectTransform currentContent;
    private RectTransform buildableContent;
    private ScrollRect currentScrollRect;
    private ScrollRect buildableScrollRect;
    private StructureActionManager structureActionManager;
    private StructureNarratorVoiceRunner narratorVoiceRunner;
    private string selectedRegionDisplayName;
    private Transform selectedRegionTransform;

    private void Awake()
    {
        BindSceneObjects();
        LoadStructDefinitions();
        narratorVoiceRunner = GetComponent<StructureNarratorVoiceRunner>();
        if (narratorVoiceRunner == null)
        {
            narratorVoiceRunner = gameObject.AddComponent<StructureNarratorVoiceRunner>();
        }
        SetPanelActive(currentPanel, false);
        SetPanelActive(buildablePanel, false);
        SetPanelActive(descriptionPanel, false);
    }

    public void ShowRegion(string regionDisplayName, Transform regionTransform)
    {
        BindSceneObjects();

        selectedRegionDisplayName = regionDisplayName;
        selectedRegionTransform = regionTransform;

        SetPanelActive(currentPanel, false);
        SetPanelActive(buildablePanel, false);
        SetPanelActive(descriptionPanel, false);
        ClearItems(currentItems);
        ClearItems(buildableItems);
    }

    public void ShowCurrentStructures()
    {
        if (selectedRegionTransform == null)
        {
            return;
        }

        BindSceneObjects();
        LoadStructDefinitions();
        SetPanelActive(currentPanel, true);
        SetText(currentNameText, selectedRegionDisplayName + " (현재 건물목록)");
        PopulateStructures(selectedRegionTransform, true, true, currentTemplate, currentContent, currentContainer, currentScrollRect, currentItems);
    }

    public void ShowBuildableStructures()
    {
        if (selectedRegionTransform == null || string.IsNullOrEmpty(selectedRegionDisplayName))
        {
            return;
        }

        BindSceneObjects();
        LoadStructDefinitions();
        SetPanelActive(buildablePanel, true);
        SetText(buildableNameText, selectedRegionDisplayName + " (건축 가능 건물)");
        PopulateStructures(selectedRegionTransform, false, false, buildableTemplate, buildableContent, buildableContainer, buildableScrollRect, buildableItems);
    }
    public void RefreshVisiblePanels()
    {
        bool refreshCurrent = currentPanel != null && currentPanel.activeSelf;
        bool refreshBuildable = buildablePanel != null && buildablePanel.activeSelf;

        if (refreshCurrent)
        {
            ShowCurrentStructures();
        }

        if (refreshBuildable)
        {
            ShowBuildableStructures();
        }
    }

    private void BindSceneObjects()
    {
        Transform uiRoot = transform;
        if (gameObject.name != "UI")
        {
            GameObject uiObject = GameObject.Find("UI");
            if (uiObject != null)
            {
                uiRoot = uiObject.transform;
            }
        }

        structureActionManager = uiRoot.GetComponent<StructureActionManager>();

        Transform currentPanelTransform = uiRoot.Find("CurStruc");
        Transform buildablePanelTransform = uiRoot.Find("CanBuildStruc");
        Transform terrainPanelTransform = uiRoot.Find("TerrainPanel");
        Transform descriptionPanelTransform = uiRoot.Find("DescStruc");

        if (currentPanelTransform != null)
        {
            currentPanel = currentPanelTransform.gameObject;
            currentNameText = FindText(currentPanelTransform, "Name");
            currentContainer = currentPanelTransform.Find("StruContainer") as RectTransform;
            if (currentContainer != null)
            {
                currentTemplate = currentContainer.Find("StrTemplate") as RectTransform;
                if (currentTemplate != null && currentTemplate.GetComponent<Button>() == null)
                {
                    currentTemplate.gameObject.AddComponent<Button>();
                }
                PrepareScrollContent(currentContainer, ref currentContent, ref currentScrollRect);
            }

            BindCloseButton(currentPanelTransform);
        }

        if (buildablePanelTransform != null)
        {
            buildablePanel = buildablePanelTransform.gameObject;
            buildableNameText = FindText(buildablePanelTransform, "Name");
            buildableContainer = buildablePanelTransform.Find("StruContainer") as RectTransform;
            if (buildableContainer != null)
            {
                buildableTemplate = buildableContainer.Find("StrTemplate") as RectTransform;
                PrepareScrollContent(buildableContainer, ref buildableContent, ref buildableScrollRect);
            }

            BindCloseButton(buildablePanelTransform);
        }

        if (descriptionPanelTransform != null)
        {
            descriptionPanel = descriptionPanelTransform.gameObject;
            descriptionNameText = FindText(descriptionPanelTransform, "StucName");
            descriptionText = FindText(descriptionPanelTransform, "Desc");
            descriptionStartYearText = FindText(descriptionPanelTransform, "StartYear");
            Transform imageTransform = descriptionPanelTransform.Find("Image");
            if (imageTransform != null)
            {
                descriptionImage = imageTransform.GetComponent<Image>();
            }
            BindDescriptionCloseButton(descriptionPanelTransform);
        }

        if (terrainPanelTransform != null)
        {
            BindTerrainButton(terrainPanelTransform, "Summary", ShowCurrentStructures);
            BindTerrainButton(terrainPanelTransform, "Build", ShowBuildableStructures);
        }
    }

    private void PrepareScrollContent(RectTransform container, ref RectTransform content, ref ScrollRect scrollRect)
    {
        if (container == null)
        {
            return;
        }

        content = container.Find("Content") as RectTransform;
        if (content == null)
        {
            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            content = contentObject.GetComponent<RectTransform>();
            content.SetParent(container, false);
            content.anchorMin = new Vector2(0.5f, 0.5f);
            content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = container.sizeDelta;
        }

        if (container.GetComponent<Mask>() == null)
        {
            Mask mask = container.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
        }

        scrollRect = container.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = container.gameObject.AddComponent<ScrollRect>();
        }

        scrollRect.content = content;
        scrollRect.viewport = container;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    private void BindCloseButton(Transform panelTransform)
    {
        Transform closeTransform = panelTransform.Find("CloseBtn");
        if (closeTransform == null)
        {
            return;
        }

        Button closeButton = closeTransform.GetComponent<Button>();
        if (closeButton == null)
        {
            return;
        }

        closeButton.onClick.RemoveListener(CloseCurrentPanel);
        closeButton.onClick.RemoveListener(CloseBuildablePanel);

        if (panelTransform.gameObject == currentPanel)
        {
            closeButton.onClick.AddListener(CloseCurrentPanel);
        }
        else if (panelTransform.gameObject == buildablePanel)
        {
            closeButton.onClick.AddListener(CloseBuildablePanel);
        }
    }

    private void BindDescriptionCloseButton(Transform panelTransform)
    {
        Transform closeTransform = panelTransform.Find("CloseBtn");
        if (closeTransform == null)
        {
            return;
        }

        Button closeButton = closeTransform.GetComponent<Button>();
        if (closeButton == null)
        {
            return;
        }

        closeButton.onClick.RemoveListener(CloseDescriptionPanel);
        closeButton.onClick.AddListener(CloseDescriptionPanel);
    }

    private void BindTerrainButton(Transform terrainPanelTransform, string childName, UnityEngine.Events.UnityAction action)
    {
        Transform buttonTransform = terrainPanelTransform.Find(childName);
        if (buttonTransform == null)
        {
            return;
        }

        Button button = buttonTransform.GetComponent<Button>();
        if (button == null)
        {
            button = buttonTransform.gameObject.AddComponent<Button>();
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
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

    private void PopulateStructures(Transform regionTransform, bool showActive, bool enableDescription, RectTransform template, RectTransform content, RectTransform container, ScrollRect scrollRect, List<GameObject> items)
    {
        ClearItems(items);

        if (regionTransform == null || template == null || content == null)
        {
            return;
        }

        int itemIndex = 0;
        foreach (Transform child in regionTransform)
        {
            AddStructureItemIfTarget(child, showActive, enableDescription, template, content, items, ref itemIndex);

            if (child.name == IgnoredStructName)
            {
                foreach (Transform commonChild in child)
                {
                    AddStructureItemIfTarget(commonChild, showActive, enableDescription, template, content, items, ref itemIndex);
                }
            }
        }

        UpdateContentSize(itemIndex, template, content, container, scrollRect);
        RepositionItems(items, template, content);
    }

    private void AddStructureItemIfTarget(Transform target, bool showActive, bool enableDescription, RectTransform template, RectTransform content, List<GameObject> items, ref int itemIndex)
    {
        if (!IsStructureTarget(target, showActive))
        {
            return;
        }

        StructDefinitionData definition;
        if (!structDefinitions.TryGetValue(target.name, out definition))
        {
            definition = StructDefinitionData.CreateFallback(target.name);
            Debug.LogWarning(target.name + " was found in the scene but not in StructDefinition.csv.");
        }

        CreateItem(target, definition, enableDescription, template, content, items, itemIndex);
        itemIndex += 1;
    }

    private bool IsStructureTarget(Transform target, bool showActive)
    {
        if (target == null || target.name == IgnoredStructName)
        {
            return false;
        }

        if (!structDefinitions.ContainsKey(target.name))
        {
            return false;
        }

        if (!showActive && structureActionManager != null && (structureActionManager.IsConstructionPending(target.gameObject) || structureActionManager.IsDemolitionPending(target.gameObject)))
        {
            return false;
        }

        return target.gameObject.activeSelf == showActive;
    }

    private string ResolveDisplayName(StructDefinitionData definition)
    {
        string displayName = string.IsNullOrEmpty(definition.DisplayName) ? definition.Name : definition.DisplayName;
        if (displayName.Contains(DistrictNameToken + DistrictOfficeSuffix))
        {
            string region = selectedRegionDisplayName ?? string.Empty;
            if (string.IsNullOrEmpty(region))
            {
                return displayName.Replace(DistrictNameToken, string.Empty);
            }

            return region.EndsWith("\uAD6C", System.StringComparison.Ordinal) ? region + "\uCCAD" : region + DistrictOfficeSuffix;
        }

        if (displayName.Contains(DistrictNameToken))
        {
            return displayName.Replace(DistrictNameToken, selectedRegionDisplayName ?? string.Empty);
        }

        return displayName;
    }

    private void CreateItem(Transform structureTransform, StructDefinitionData definition, bool enableDescription, RectTransform template, RectTransform content, List<GameObject> items, int itemIndex)
    {
        GameObject itemObject = Instantiate(template.gameObject, content);
        itemObject.name = template.name + "_" + itemIndex;
        itemObject.SetActive(true);

        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            itemRect.anchoredPosition = new Vector2(template.anchoredPosition.x, template.anchoredPosition.y - itemSpacing * itemIndex);
        }

        string displayName = ResolveDisplayName(definition);

        SetText(FindText(itemObject.transform, "StruName"), displayName);
        SetText(FindText(itemObject.transform, "People"), FormatPopulationCapacityText(structureTransform, definition));
        SetText(FindText(itemObject.transform, "Money"), definition.Money.ToString());
        SetText(FindText(itemObject.transform, "Convenience"), definition.Convenience.ToString());
        SetText(FindText(itemObject.transform, "Science"), definition.Science.ToString());

        if (enableDescription)
        {
            BindItemButton(itemObject, definition);
            BindStructureActionButtons(itemObject, structureTransform == null ? null : structureTransform.gameObject, definition, displayName);
        }
        else
        {
            BindBuildButton(itemObject, structureTransform == null ? null : structureTransform.gameObject, definition, displayName);
        }

        items.Add(itemObject);
    }

    private string FormatPopulationCapacityText(Transform structureTransform, StructDefinitionData definition)
    {
        int capacity = CalculateStructurePopulationCapacity(structureTransform, definition);
        if (capacity <= 0)
        {
            return "-";
        }

        return "+" + capacity.ToString();
    }

    private int CalculateStructurePopulationCapacity(Transform structureTransform, StructDefinitionData definition)
    {
        if (definition == null || !IsHouseStructureName(definition.Name))
        {
            return 0;
        }

        int capacity = Mathf.Max(0, definition.PeopleIncrease) * HouseCapacityMultiplier;
        if (structureTransform == null)
        {
            return capacity;
        }

        StructureInvestmentState investmentState = structureTransform.GetComponent<StructureInvestmentState>();
        if (investmentState != null)
        {
            capacity = Mathf.CeilToInt(capacity * investmentState.RefreshCurrentStatMultiplier());
        }

        return capacity;
    }

    private bool IsHouseStructureName(string structureName)
    {
        return structureName == "House1" ||
               structureName == "House2" ||
               structureName == "House3" ||
               structureName == "House4";
    }

    private void BindItemButton(GameObject itemObject, StructDefinitionData definition)
    {
        Button button = itemObject.GetComponent<Button>();
        if (button == null)
        {
            button = itemObject.AddComponent<Button>();
        }

        StructDefinitionData selectedDefinition = definition;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ShowStructureDescription(selectedDefinition));
    }

    private void BindStructureActionButtons(GameObject itemObject, GameObject targetObject, StructDefinitionData definition, string displayName)
    {
        BindStructureActionButton(itemObject, "InvestBtn", targetObject, definition, displayName, StructureActionButtonBinding.ActionKind.Invest);
        BindStructureActionButton(itemObject, "RepairBtn", targetObject, definition, displayName, StructureActionButtonBinding.ActionKind.Repair);
        BindStructureActionButton(itemObject, "DestructBtn", targetObject, definition, displayName, StructureActionButtonBinding.ActionKind.Destroy);
    }

    private void BindStructureActionButton(GameObject itemObject, string buttonName, GameObject targetObject, StructDefinitionData definition, string displayName, StructureActionButtonBinding.ActionKind actionKind)
    {
        if (itemObject == null)
        {
            return;
        }

        Transform buttonTransform = itemObject.transform.Find(buttonName);
        if (buttonTransform == null)
        {
            return;
        }

        Button button = buttonTransform.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        StructureActionButtonBinding binding = buttonTransform.GetComponent<StructureActionButtonBinding>();
        if (binding == null)
        {
            binding = buttonTransform.gameObject.AddComponent<StructureActionButtonBinding>();
        }

        BindSceneObjects();
        binding.Configure(structureActionManager, this, targetObject, definition, displayName, actionKind);
        button.interactable = actionKind != StructureActionButtonBinding.ActionKind.Invest || structureActionManager == null || structureActionManager.CanInvestInStructure(targetObject);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(binding.InvokeAction);
    }

    private void BindBuildButton(GameObject itemObject, GameObject targetObject, StructDefinitionData definition, string displayName)
    {
        if (itemObject == null)
        {
            return;
        }

        Transform buildButtonTransform = itemObject.transform.Find("BuildBtn");
        if (buildButtonTransform == null)
        {
            return;
        }

        Button button = buildButtonTransform.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        GameObject selectedTarget = targetObject;
        StructDefinitionData selectedDefinition = definition;
        string selectedDisplayName = displayName;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            BindSceneObjects();
            if (structureActionManager != null)
            {
                structureActionManager.OpenBuildPanel(selectedTarget, selectedDefinition, selectedDisplayName, this);
            }
        });
    }

    private void ShowStructureDescription(StructDefinitionData definition)
    {
        BindSceneObjects();

        SetPanelActive(currentPanel, false);
        SetPanelActive(descriptionPanel, true);
        SetText(descriptionNameText, ResolveDisplayName(definition));
        SetText(descriptionText, definition.Description);
        SetText(descriptionStartYearText, definition.StartYear);

        if (descriptionImage == null)
        {
            if (narratorVoiceRunner != null) narratorVoiceRunner.PlayStructureNarrator(definition.Name);
            return;
        }

        Sprite sprite = LoadSprite(definition.ImagePath);
        descriptionImage.sprite = sprite;
        descriptionImage.enabled = sprite != null;

        if (narratorVoiceRunner != null)
        {
            narratorVoiceRunner.PlayStructureNarrator(definition.Name);
        }
    }

    private Sprite LoadSprite(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        if (loadedSprites.TryGetValue(assetPath, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite sprite = null;
#if UNITY_EDITOR
        sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#endif
        if (sprite == null)
        {
            string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
            if (File.Exists(fullPath))
            {
                byte[] imageBytes = File.ReadAllBytes(fullPath);
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(imageBytes))
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }
        }

        loadedSprites[assetPath] = sprite;
        return sprite;
    }

    private void UpdateContentSize(int itemCount, RectTransform template, RectTransform content, RectTransform container, ScrollRect scrollRect)
    {
        if (content == null || container == null || template == null)
        {
            return;
        }

        float height = container.sizeDelta.y;
        if (itemCount > 0)
        {
            height = Mathf.Max(height, template.rect.height + itemSpacing * (itemCount - 1) + 40f);
        }

        content.sizeDelta = new Vector2(container.sizeDelta.x, height);
        content.anchoredPosition = Vector2.zero;

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void RepositionItems(List<GameObject> items, RectTransform template, RectTransform content)
    {
        if (items == null || template == null || content == null)
        {
            return;
        }

        float itemHeight = template.rect.height;
        float startY = content.sizeDelta.y * 0.5f - itemHeight * 0.5f - ContentTopPadding;
        for (int i = 0; i < items.Count; i += 1)
        {
            if (items[i] == null)
            {
                continue;
            }

            RectTransform itemRect = items[i].GetComponent<RectTransform>();
            if (itemRect != null)
            {
                itemRect.anchoredPosition = new Vector2(template.anchoredPosition.x, startY - itemSpacing * i);
            }
        }
    }

    private void ClearItems(List<GameObject> items)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i] != null)
            {
                Destroy(items[i]);
            }
        }

        items.Clear();
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

    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    private void CloseCurrentPanel()
    {
        SetPanelActive(currentPanel, false);
    }

    private void CloseBuildablePanel()
    {
        SetPanelActive(buildablePanel, false);
    }

    private void CloseDescriptionPanel()
    {
        if (narratorVoiceRunner != null) narratorVoiceRunner.StopNarrator();
        SetPanelActive(descriptionPanel, false);
        SetPanelActive(currentPanel, true);
    }

    private void OnDisable()
    {
        if (narratorVoiceRunner != null) narratorVoiceRunner.StopNarrator();
    }
}
