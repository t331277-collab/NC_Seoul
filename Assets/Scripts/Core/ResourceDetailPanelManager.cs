using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceDetailPanelManager : MonoBehaviour
{
    private static ResourceDetailPanelManager activeInstance;

    private readonly List<ResourcePanelBinding> bindings = new List<ResourcePanelBinding>();
    private ResourcePanelBinding activeBinding;

    private void Awake()
    {
        BindSceneObjects();
        CloseAllPanels();
    }

    private void OnEnable()
    {
        activeInstance = this;
        BindSceneObjects();
        AddListeners();
    }

    private void OnDisable()
    {
        RemoveListeners();
        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    private void Update()
    {
        if (activeBinding != null && activeBinding.DetailPanel != null && activeBinding.DetailPanel.activeSelf)
        {
            SyncTexts(activeBinding);
        }
    }

    public static bool TryCloseOpenPanel()
    {
        return activeInstance != null && activeInstance.CloseOpenPanel();
    }

    private void BindSceneObjects()
    {
        bindings.Clear();

        Transform uiRoot = transform;
        if (gameObject.name != "UI")
        {
            GameObject uiObject = GameObject.Find("UI");
            if (uiObject != null)
            {
                uiRoot = uiObject.transform;
            }
        }

        Transform panelContainer = uiRoot.Find("PanelContainer");
        if (panelContainer == null)
        {
            return;
        }

        AddBinding(uiRoot, panelContainer, "MoneyPanel", "MoneyPanel");
        AddBinding(uiRoot, panelContainer, "ConveniencePanel", "ConveniencePanel");
        AddBinding(uiRoot, panelContainer, "PeoplePanel", "PeoplePanel");
        AddBinding(uiRoot, panelContainer, "SciencePanel", "SciencePanel");
        AddBinding(uiRoot, panelContainer, "SciecnePanel", "SciecnePanel");
        AddBinding(uiRoot, panelContainer, "LovePanel", "LovePanel");
    }

    private void AddBinding(Transform uiRoot, Transform panelContainer, string sourceName, string detailName)
    {
        Transform sourcePanel = uiRoot.Find(sourceName);
        Transform detailPanel = panelContainer.Find(detailName);
        if (sourcePanel == null || detailPanel == null)
        {
            return;
        }

        ResourcePanelBinding binding = new ResourcePanelBinding
        {
            SourcePanel = sourcePanel.gameObject,
            DetailPanel = detailPanel.gameObject,
            SourceText = FindPanelText(sourcePanel),
            SourcePlusMinus = FindText(sourcePanel, "PlusMinus"),
            DetailText = FindPanelText(detailPanel),
            DetailPlusMinus = FindText(detailPanel, "PlusMinus"),
            SourceButton = sourcePanel.GetComponent<Button>()
        };

        bindings.Add(binding);
    }

private void AddListeners()
    {
        foreach (ResourcePanelBinding binding in bindings)
        {
            if (binding.SourceButton == null)
            {
                continue;
            }

            if (binding.OpenPanel != null)
            {
                binding.SourceButton.onClick.RemoveListener(binding.OpenPanel);
            }

            ResourcePanelBinding capturedBinding = binding;
            binding.OpenPanel = () => OpenDetailPanel(capturedBinding);
            binding.SourceButton.onClick.AddListener(binding.OpenPanel);
        }
    }

    private void RemoveListeners()
    {
        foreach (ResourcePanelBinding binding in bindings)
        {
            if (binding.SourceButton != null && binding.OpenPanel != null)
            {
                binding.SourceButton.onClick.RemoveListener(binding.OpenPanel);
            }
        }
    }

    private void OpenDetailPanel(ResourcePanelBinding binding)
    {
        CloseAllPanels();
        activeBinding = binding;
        SetActive(binding.DetailPanel, true);
        SyncTexts(binding);
    }

    private bool CloseOpenPanel()
    {
        if (activeBinding != null && activeBinding.DetailPanel != null && activeBinding.DetailPanel.activeSelf)
        {
            SetActive(activeBinding.DetailPanel, false);
            activeBinding = null;
            return true;
        }

        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].DetailPanel != null && bindings[i].DetailPanel.activeSelf)
            {
                SetActive(bindings[i].DetailPanel, false);
                activeBinding = null;
                return true;
            }
        }

        return false;
    }

    private void CloseAllPanels()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            SetActive(bindings[i].DetailPanel, false);
        }

        activeBinding = null;
    }

    private void SyncTexts(ResourcePanelBinding binding)
    {
        CopyText(binding.SourceText, binding.DetailText);
        CopyText(binding.SourcePlusMinus, binding.DetailPlusMinus);
    }

    private TextMeshProUGUI FindPanelText(Transform panel)
    {
        TextMeshProUGUI text = FindText(panel, "Text");
        if (text == null)
        {
            text = FindText(panel, "Text (TMP)");
        }

        return text;
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

    private void CopyText(TextMeshProUGUI source, TextMeshProUGUI target)
    {
        if (source != null && target != null)
        {
            target.text = source.text;
        }
    }

    private void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    private class ResourcePanelBinding
    {
        public GameObject SourcePanel;
        public GameObject DetailPanel;
        public TextMeshProUGUI SourceText;
        public TextMeshProUGUI SourcePlusMinus;
        public TextMeshProUGUI DetailText;
        public TextMeshProUGUI DetailPlusMinus;
        public Button SourceButton;
        public UnityEngine.Events.UnityAction OpenPanel;
    }
}
