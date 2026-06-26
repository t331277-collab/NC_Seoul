using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DistrictStructurePanelManager : MonoBehaviour
{
    private const string StructPrefix = "Stru";
    private const string IgnoredStructName = "Stru_CommonSense";

    [SerializeField] private string structDefinitionRelativePath = "Data/StructDefinition.csv";
    [SerializeField] private float itemSpacing = 235f;

    private readonly Dictionary<string, StructDefinition> structDefinitions = new Dictionary<string, StructDefinition>();
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
    private string selectedRegionDisplayName;
    private Transform selectedRegionTransform;

    private void Awake()
    {
        BindSceneObjects();
        LoadStructDefinitions();
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

        string csvPath = Path.Combine(Application.dataPath, structDefinitionRelativePath);
        if (!File.Exists(csvPath))
        {
            Debug.LogWarning($"StructDefinition.csv was not found at {csvPath}.");
            return;
        }

        string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            string[] columns = ParseCsvLine(lines[i]);
            if (columns.Length < 11)
            {
                Debug.LogWarning($"StructDefinition.csv line {i + 1} has fewer than 11 columns.");
                continue;
            }

            if (!TryParseDefinition(columns, out StructDefinition definition))
            {
                Debug.LogWarning($"StructDefinition.csv line {i + 1} could not be parsed.");
                continue;
            }

            structDefinitions[definition.Name] = definition;
        }
    }

    private string[] ParseCsvLine(string line)
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

    private string NormalizeCsvText(string value)
    {
        return string.IsNullOrEmpty(value) ? value : value.Replace("\\n", "\n");
    }

    private bool TryParseDefinition(string[] columns, out StructDefinition definition)
    {
        definition = default;

        string structName = columns[0].Trim();
        string displayName = columns[1].Trim();
        string imagePath = columns[8].Trim();
        string description = NormalizeCsvText(columns[9].Trim());
        string startYear = NormalizeCsvText(columns[10].Trim());
        if (string.IsNullOrEmpty(structName))
        {
            return false;
        }

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = structName;
        }

        if (string.IsNullOrEmpty(description))
        {
            description = "설명글 추가 예정";
        }

        if (string.IsNullOrEmpty(startYear))
        {
            startYear = "임시";
        }

        if (!int.TryParse(columns[3].Trim(), out int money) ||
            !int.TryParse(columns[4].Trim(), out int people) ||
            !int.TryParse(columns[5].Trim(), out int science) ||
            !int.TryParse(columns[7].Trim(), out int convenience))
        {
            return false;
        }

        definition = new StructDefinition(structName, displayName, people, money, convenience, science, imagePath, description, startYear);
        return true;
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
            if (!IsStructureTarget(child, showActive))
            {
                continue;
            }

            StructDefinition definition;
            if (!structDefinitions.TryGetValue(child.name, out definition))
            {
                definition = new StructDefinition(child.name, child.name, 0, 0, 0, 0, string.Empty, "설명글 추가 예정", "임시");
                Debug.LogWarning($"{child.name} was found in the scene but not in StructDefinition.csv.");
            }

            CreateItem(definition, enableDescription, template, content, items, itemIndex);
            itemIndex += 1;
        }

        UpdateContentSize(itemIndex, template, content, container, scrollRect);
    }

    private bool IsStructureTarget(Transform target, bool showActive)
    {
        if (target == null || target.name == IgnoredStructName)
        {
            return false;
        }

        if (!target.name.StartsWith(StructPrefix, System.StringComparison.Ordinal))
        {
            return false;
        }

        return target.gameObject.activeInHierarchy == showActive;
    }

    private void CreateItem(StructDefinition definition, bool enableDescription, RectTransform template, RectTransform content, List<GameObject> items, int itemIndex)
    {
        GameObject itemObject = Instantiate(template.gameObject, content);
        itemObject.name = template.name + "_" + itemIndex;
        itemObject.SetActive(true);

        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            itemRect.anchoredPosition = new Vector2(template.anchoredPosition.x, template.anchoredPosition.y - itemSpacing * itemIndex);
        }

        SetText(FindText(itemObject.transform, "StruName"), definition.DisplayName);
        SetText(FindText(itemObject.transform, "People"), definition.People.ToString());
        SetText(FindText(itemObject.transform, "Money"), definition.Money.ToString());
        SetText(FindText(itemObject.transform, "Convenience"), definition.Convenience.ToString());
        SetText(FindText(itemObject.transform, "Science"), definition.Science.ToString());

        if (enableDescription)
        {
            BindItemButton(itemObject, definition);
        }

        items.Add(itemObject);
    }

    private void BindItemButton(GameObject itemObject, StructDefinition definition)
    {
        Button button = itemObject.GetComponent<Button>();
        if (button == null)
        {
            button = itemObject.AddComponent<Button>();
        }

        StructDefinition selectedDefinition = definition;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ShowStructureDescription(selectedDefinition));
    }

    private void ShowStructureDescription(StructDefinition definition)
    {
        BindSceneObjects();

        SetPanelActive(currentPanel, false);
        SetPanelActive(descriptionPanel, true);
        SetText(descriptionNameText, definition.DisplayName);
        SetText(descriptionText, definition.Description);
        SetText(descriptionStartYearText, definition.StartYear);

        if (descriptionImage == null)
        {
            return;
        }

        Sprite sprite = LoadSprite(definition.ImagePath);
        descriptionImage.sprite = sprite;
        descriptionImage.enabled = sprite != null;
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
        SetPanelActive(descriptionPanel, false);
        SetPanelActive(currentPanel, true);
    }

    private struct StructDefinition
    {
        public readonly string Name;
        public readonly string DisplayName;
        public readonly int People;
        public readonly int Money;
        public readonly int Convenience;
        public readonly int Science;
        public readonly string ImagePath;
        public readonly string Description;
        public readonly string StartYear;

        public StructDefinition(string name, string displayName, int people, int money, int convenience, int science, string imagePath, string description, string startYear)
        {
            Name = name;
            DisplayName = displayName;
            People = people;
            Money = money;
            Convenience = convenience;
            Science = science;
            ImagePath = imagePath;
            Description = description;
            StartYear = startYear;
        }
    }
}
