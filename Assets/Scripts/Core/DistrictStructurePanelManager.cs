using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DistrictStructurePanelManager : MonoBehaviour
{
    private const string StructPrefix = "Stru";

    [SerializeField] private string structDefinitionRelativePath = "Data/StructDefinition.csv";
    [SerializeField] private float itemSpacing = 235f;

    private readonly Dictionary<string, StructDefinition> structDefinitions = new Dictionary<string, StructDefinition>();
    private readonly List<GameObject> currentItems = new List<GameObject>();

    private GameObject currentPanel;
    private GameObject buildablePanel;
    private TextMeshProUGUI currentNameText;
    private TextMeshProUGUI buildableNameText;
    private RectTransform currentContainer;
    private RectTransform currentTemplate;
    private RectTransform currentContent;
    private ScrollRect currentScrollRect;
    private string selectedRegionDisplayName;
    private Transform selectedRegionTransform;

    private void Awake()
    {
        BindSceneObjects();
        LoadStructDefinitions();
        SetPanelActive(currentPanel, false);
        SetPanelActive(buildablePanel, false);
    }

    public void ShowRegion(string regionDisplayName, Transform regionTransform)
    {
        BindSceneObjects();

        selectedRegionDisplayName = regionDisplayName;
        selectedRegionTransform = regionTransform;

        SetPanelActive(currentPanel, false);
        SetPanelActive(buildablePanel, false);
        ClearCurrentItems();
    }

    public void ShowCurrentStructures()
    {
        if (selectedRegionTransform == null)
        {
            return;
        }

        BindSceneObjects();
        SetPanelActive(currentPanel, true);
        SetText(currentNameText, selectedRegionDisplayName + " (현재 건물목록)");
        PopulateCurrentStructures(selectedRegionTransform);
    }

    public void ShowBuildableStructures()
    {
        if (string.IsNullOrEmpty(selectedRegionDisplayName))
        {
            return;
        }

        BindSceneObjects();
        SetPanelActive(buildablePanel, true);
        SetText(buildableNameText, selectedRegionDisplayName + " (건축 가능 건물)");
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

        if (currentPanelTransform != null)
        {
            currentPanel = currentPanelTransform.gameObject;
            currentNameText = FindText(currentPanelTransform, "Name");
            currentContainer = currentPanelTransform.Find("StruContainer") as RectTransform;
            if (currentContainer != null)
            {
                currentTemplate = currentContainer.Find("StrTemplate") as RectTransform;
                PrepareScrollContent();
            }

            BindCloseButton(currentPanelTransform);
        }

        if (buildablePanelTransform != null)
        {
            buildablePanel = buildablePanelTransform.gameObject;
            buildableNameText = FindText(buildablePanelTransform, "Name");
            BindCloseButton(buildablePanelTransform);
        }

        if (terrainPanelTransform != null)
        {
            BindTerrainButton(terrainPanelTransform, "Summary", ShowCurrentStructures);
            BindTerrainButton(terrainPanelTransform, "Build", ShowBuildableStructures);
        }
    }

    private void PrepareScrollContent()
    {
        if (currentContainer == null)
        {
            return;
        }

        currentContent = currentContainer.Find("Content") as RectTransform;
        if (currentContent == null)
        {
            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            currentContent = contentObject.GetComponent<RectTransform>();
            currentContent.SetParent(currentContainer, false);
            currentContent.anchorMin = new Vector2(0.5f, 0.5f);
            currentContent.anchorMax = new Vector2(0.5f, 0.5f);
            currentContent.pivot = new Vector2(0.5f, 0.5f);
            currentContent.anchoredPosition = Vector2.zero;
            currentContent.sizeDelta = currentContainer.sizeDelta;
        }

        if (currentContainer.GetComponent<Mask>() == null)
        {
            Mask mask = currentContainer.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
        }

        currentScrollRect = currentContainer.GetComponent<ScrollRect>();
        if (currentScrollRect == null)
        {
            currentScrollRect = currentContainer.gameObject.AddComponent<ScrollRect>();
        }

        currentScrollRect.content = currentContent;
        currentScrollRect.viewport = currentContainer;
        currentScrollRect.horizontal = false;
        currentScrollRect.vertical = true;
        currentScrollRect.movementType = ScrollRect.MovementType.Clamped;
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

            string[] columns = lines[i].Split(',');
            if (columns.Length < 7)
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

        if (!int.TryParse(columns[2].Trim(), out int money) ||
            !int.TryParse(columns[3].Trim(), out int people) ||
            !int.TryParse(columns[4].Trim(), out int science) ||
            !int.TryParse(columns[6].Trim(), out int convenience))
        {
            return false;
        }

        definition = new StructDefinition(structName, people, money, convenience, science);
        return true;
    }

    private void PopulateCurrentStructures(Transform regionTransform)
    {
        ClearCurrentItems();

        if (regionTransform == null || currentTemplate == null || currentContent == null)
        {
            return;
        }

        int itemIndex = 0;
        foreach (Transform child in regionTransform)
        {
            if (!child.name.StartsWith(StructPrefix, System.StringComparison.Ordinal))
            {
                continue;
            }

            StructDefinition definition;
            if (!structDefinitions.TryGetValue(child.name, out definition))
            {
                definition = new StructDefinition(child.name, 0, 0, 0, 0);
                Debug.LogWarning($"{child.name} was found in the scene but not in StructDefinition.csv.");
            }

            CreateCurrentItem(definition, itemIndex);
            itemIndex += 1;
        }

        UpdateContentSize(itemIndex);
    }

    private void CreateCurrentItem(StructDefinition definition, int itemIndex)
    {
        GameObject itemObject = Instantiate(currentTemplate.gameObject, currentContent);
        itemObject.name = currentTemplate.name + "_" + itemIndex;
        itemObject.SetActive(true);

        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            itemRect.anchoredPosition = new Vector2(currentTemplate.anchoredPosition.x, currentTemplate.anchoredPosition.y - itemSpacing * itemIndex);
        }

        SetText(FindText(itemObject.transform, "StruName"), definition.Name);
        SetText(FindText(itemObject.transform, "People"), definition.People.ToString());
        SetText(FindText(itemObject.transform, "Money"), definition.Money.ToString());
        SetText(FindText(itemObject.transform, "Convenience"), definition.Convenience.ToString());
        SetText(FindText(itemObject.transform, "Science"), definition.Science.ToString());

        currentItems.Add(itemObject);
    }

    private void UpdateContentSize(int itemCount)
    {
        if (currentContent == null || currentContainer == null)
        {
            return;
        }

        float height = currentContainer.sizeDelta.y;
        if (itemCount > 0)
        {
            height = Mathf.Max(height, currentTemplate.rect.height + itemSpacing * (itemCount - 1) + 40f);
        }

        currentContent.sizeDelta = new Vector2(currentContainer.sizeDelta.x, height);
        currentContent.anchoredPosition = Vector2.zero;

        if (currentScrollRect != null)
        {
            currentScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void ClearCurrentItems()
    {
        for (int i = currentItems.Count - 1; i >= 0; i--)
        {
            if (currentItems[i] != null)
            {
                Destroy(currentItems[i]);
            }
        }

        currentItems.Clear();
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

    private struct StructDefinition
    {
        public readonly string Name;
        public readonly int People;
        public readonly int Money;
        public readonly int Convenience;
        public readonly int Science;

        public StructDefinition(string name, int people, int money, int convenience, int science)
        {
            Name = name;
            People = people;
            Money = money;
            Convenience = convenience;
            Science = science;
        }
    }
}
