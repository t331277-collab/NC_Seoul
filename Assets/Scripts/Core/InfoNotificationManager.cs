using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoNotificationManager : MonoBehaviour
{
    private const float AlertMoveDuration = 0.5f;
    private const float NotificationItemSpacing = 186.8f;
    private static readonly Vector2 AlertImageTargetPosition = new Vector2(38f, -3.2f);

    private readonly List<GameObject> notificationItems = new List<GameObject>();

    private Transform seoulRoot;
    private UIManager uiManager;
    private GameObject inforRoot;
    private GameObject inforPanel;
    private GameObject inforTextObject;
    private RectTransform inforImageRect;
    private RectTransform contentRect;
    private RectTransform scrollObjectRect;
    private RectTransform itemParentRect;
    private RectTransform templateRect;
    private Button inforButton;
    private Button closeButton;
    private ScrollRect scrollRect;
    private UISfxManager uiSfxManager;
    private Coroutine imageMoveCoroutine;
    private Vector2 inforImageDefaultPosition;
    private bool hasInforImageDefaultPosition;
    private bool inforTextDefaultActive = true;
    private bool hasInforTextDefaultActive;
    private Vector2 contentDefaultSize;
    private bool hasContentDefaultSize;

    private void Awake()
    {
        BindSceneObjects();
        SetActive(inforPanel, false);
        SetActive(templateRect == null ? null : templateRect.gameObject, false);
    }

    private void OnEnable()
    {
        BindSceneObjects();
        BindInforButton();
    }

    private void OnDisable()
    {
        if (inforButton != null)
        {
            inforButton.onClick.RemoveListener(OpenInfoPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseInfoPanel);
        }
    }

    public void AddNotification(string status, string description, string regionName, Transform regionTransform)
    {
        BindSceneObjects();
        HideIntroText();
        MoveInfoImageToAlertPosition();
        CreateNotificationItem(status, description, regionName, regionTransform);
    }

    public void ClearNotificationsForNewYear()
    {
        BindSceneObjects();
        ClearNotificationItems();
        ResetInfoButtonAppearance(false);
        SetActive(inforPanel, false);
    }

    private void BindSceneObjects()
    {
        if (seoulRoot == null)
        {
            GameObject seoulObject = GameObject.Find("Seoul");
            if (seoulObject != null)
            {
                seoulRoot = seoulObject.transform;
            }
        }

        if (uiManager == null)
        {
            uiManager = GetComponent<UIManager>();
            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }
        }

        if (uiSfxManager == null)
        {
            uiSfxManager = GetComponent<UISfxManager>();
            if (uiSfxManager == null)
            {
                uiSfxManager = FindObjectOfType<UISfxManager>();
            }
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

        Transform inforTransform = uiRoot.Find("Infor");
        if (inforTransform != null)
        {
            inforRoot = inforTransform.gameObject;
            inforTextObject = FindDirectChild(inforTransform, "TXT");
            Transform imageTransform = inforTransform.Find("Image");
            inforImageRect = imageTransform == null ? null : imageTransform.GetComponent<RectTransform>();
            CaptureInfoButtonDefaults();
            inforButton = inforRoot.GetComponent<Button>();
            if (inforButton == null)
            {
                inforButton = inforRoot.AddComponent<Button>();
            }
        }

        Transform panelTransform = uiRoot.Find("InforPanel");
        if (panelTransform != null)
        {
            inforPanel = panelTransform.gameObject;
            Transform contentTransform = panelTransform.Find("Content");
            contentRect = contentTransform == null ? null : contentTransform.GetComponent<RectTransform>();
            ConfigureScrollObjects(contentTransform);
            Transform templateTransform = FindTemplateTransform(contentTransform);
            templateRect = templateTransform == null ? null : templateTransform.GetComponent<RectTransform>();
            Transform closeTransform = panelTransform.Find("Close");
            closeButton = closeTransform == null ? null : closeTransform.GetComponent<Button>();
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseInfoPanel);
                closeButton.onClick.AddListener(CloseInfoPanel);
            }
        }
    }

    private void CaptureInfoButtonDefaults()
    {
        if (inforImageRect != null && !hasInforImageDefaultPosition)
        {
            inforImageDefaultPosition = inforImageRect.anchoredPosition;
            hasInforImageDefaultPosition = true;
        }

        if (inforTextObject != null && !hasInforTextDefaultActive)
        {
            inforTextDefaultActive = inforTextObject.activeSelf;
            hasInforTextDefaultActive = true;
        }
    }

    private void ConfigureScrollObjects(Transform contentTransform)
    {
        if (contentTransform == null || contentRect == null)
        {
            return;
        }

        if (!hasContentDefaultSize)
        {
            contentDefaultSize = contentRect.sizeDelta;
            hasContentDefaultSize = true;
        }

        Transform scrollTransform = contentTransform.Find("Scroll Object");
        if (scrollTransform == null)
        {
            scrollTransform = contentTransform.Find("ScrollObject");
        }

        if (scrollTransform == null)
        {
            GameObject scrollObject = new GameObject("Scroll Object", typeof(RectTransform));
            scrollTransform = scrollObject.transform;
            scrollTransform.SetParent(contentTransform, false);
            RectTransform createdRect = scrollObject.GetComponent<RectTransform>();
            createdRect.anchorMin = new Vector2(0f, 1f);
            createdRect.anchorMax = new Vector2(1f, 1f);
            createdRect.pivot = new Vector2(0.5f, 1f);
            createdRect.anchoredPosition = Vector2.zero;
            createdRect.sizeDelta = new Vector2(0f, Mathf.Max(contentRect.rect.height, contentDefaultSize.y));
        }

        scrollObjectRect = scrollTransform.GetComponent<RectTransform>();
        itemParentRect = scrollObjectRect == null ? contentRect : scrollObjectRect;

        Transform templateTransform = scrollTransform.Find("Template");
        if (templateTransform == null)
        {
            templateTransform = contentTransform.Find("Template");
            if (templateTransform != null && itemParentRect != null && templateTransform.parent != itemParentRect)
            {
                RectTransform templateAsRect = templateTransform.GetComponent<RectTransform>();
                Vector2 anchoredPosition = templateAsRect == null ? Vector2.zero : templateAsRect.anchoredPosition;
                templateTransform.SetParent(itemParentRect, false);
                if (templateAsRect != null)
                {
                    templateAsRect.anchoredPosition = anchoredPosition;
                }
            }
        }

        RectMask2D mask = contentTransform.GetComponent<RectMask2D>();
        if (mask == null)
        {
            contentTransform.gameObject.AddComponent<RectMask2D>();
        }

        scrollRect = contentTransform.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = contentTransform.gameObject.AddComponent<ScrollRect>();
        }

        scrollRect.content = itemParentRect;
        scrollRect.viewport = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 40f;
    }

    private Transform FindTemplateTransform(Transform contentTransform)
    {
        if (itemParentRect != null)
        {
            Transform templateTransform = itemParentRect.Find("Template");
            if (templateTransform != null)
            {
                return templateTransform;
            }
        }

        return contentTransform == null ? null : contentTransform.Find("Template");
    }

    private GameObject FindDirectChild(Transform parent, string childName)
    {
        Transform child = parent == null ? null : parent.Find(childName);
        return child == null ? null : child.gameObject;
    }

    private void BindInforButton()
    {
        if (inforButton == null)
        {
            return;
        }

        inforButton.onClick.RemoveListener(OpenInfoPanel);
        inforButton.onClick.AddListener(OpenInfoPanel);
    }

    private void OpenInfoPanel()
    {
        ResetInfoButtonAppearance(true);
        SetActive(inforPanel, true);
        PlayPanelOpenSfx();
    }

    private void CloseInfoPanel()
    {
        SetActive(inforPanel, false);
    }

    private void HideIntroText()
    {
        SetActive(inforTextObject, false);
    }

    private void ResetInfoButtonAppearance(bool animate)
    {
        SetActive(inforTextObject, hasInforTextDefaultActive ? inforTextDefaultActive : true);
        MoveInfoImageToDefaultPosition(animate);
    }

    private void MoveInfoImageToAlertPosition()
    {
        if (inforImageRect == null)
        {
            return;
        }

        if (imageMoveCoroutine != null)
        {
            StopCoroutine(imageMoveCoroutine);
        }

        imageMoveCoroutine = StartCoroutine(MoveImageRoutine(inforImageRect.anchoredPosition, AlertImageTargetPosition));
    }

    private void MoveInfoImageToDefaultPosition(bool animate)
    {
        if (inforImageRect == null || !hasInforImageDefaultPosition)
        {
            return;
        }

        if (imageMoveCoroutine != null)
        {
            StopCoroutine(imageMoveCoroutine);
            imageMoveCoroutine = null;
        }

        if (!animate)
        {
            inforImageRect.anchoredPosition = inforImageDefaultPosition;
            return;
        }

        imageMoveCoroutine = StartCoroutine(MoveImageRoutine(inforImageRect.anchoredPosition, inforImageDefaultPosition));
    }

    private IEnumerator MoveImageRoutine(Vector2 from, Vector2 to)
    {
        float elapsed = 0f;
        while (elapsed < AlertMoveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / AlertMoveDuration);
            inforImageRect.anchoredPosition = Vector2.Lerp(from, to, progress);
            yield return null;
        }

        inforImageRect.anchoredPosition = to;
        imageMoveCoroutine = null;
    }

    private void CreateNotificationItem(string status, string description, string regionName, Transform regionTransform)
    {
        if (contentRect == null || templateRect == null)
        {
            return;
        }

        RectTransform parentRect = itemParentRect == null ? contentRect : itemParentRect;
        GameObject itemObject = Instantiate(templateRect.gameObject, parentRect);
        itemObject.name = templateRect.name + "_" + notificationItems.Count;
        itemObject.SetActive(true);

        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            itemRect.anchoredPosition = new Vector2(templateRect.anchoredPosition.x, templateRect.anchoredPosition.y - NotificationItemSpacing * notificationItems.Count);
        }

        SetText(FindText(itemObject.transform, "Statue"), status);
        SetText(FindText(itemObject.transform, "Desc"), description);
        BindItemClick(itemObject, regionName, regionTransform);
        notificationItems.Add(itemObject);
        ResizeContentForNotifications();
    }

    private void ResizeContentForNotifications()
    {
        RectTransform parentRect = itemParentRect == null ? contentRect : itemParentRect;
        if (contentRect == null || parentRect == null || templateRect == null)
        {
            return;
        }

        int itemCount = notificationItems.Count;
        if (itemCount <= 0)
        {
            return;
        }

        float firstY = templateRect.anchoredPosition.y;
        float lastY = firstY - NotificationItemSpacing * (itemCount - 1);
        float requiredHeight = Mathf.Abs(lastY) + templateRect.rect.height + NotificationItemSpacing;
        Vector2 sizeDelta = parentRect.sizeDelta;
        sizeDelta.y = Mathf.Max(contentRect.rect.height, requiredHeight);
        parentRect.sizeDelta = sizeDelta;

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void ClearNotificationItems()
    {
        for (int i = notificationItems.Count - 1; i >= 0; i -= 1)
        {
            if (notificationItems[i] != null)
            {
                Destroy(notificationItems[i]);
            }
        }

        notificationItems.Clear();

        RectTransform parentRect = itemParentRect == null ? contentRect : itemParentRect;
        if (parentRect != null && contentRect != null)
        {
            Vector2 sizeDelta = parentRect.sizeDelta;
            sizeDelta.y = Mathf.Max(contentRect.rect.height, hasContentDefaultSize ? contentDefaultSize.y : contentRect.sizeDelta.y);
            parentRect.sizeDelta = sizeDelta;
            parentRect.anchoredPosition = Vector2.zero;
        }

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        SetActive(templateRect == null ? null : templateRect.gameObject, false);
    }

    private void BindItemClick(GameObject itemObject, string regionName, Transform regionTransform)
    {
        Button rootButton = itemObject.GetComponent<Button>();
        if (rootButton == null)
        {
            rootButton = itemObject.AddComponent<Button>();
        }

        Button[] buttons = itemObject.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i += 1)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OpenRegion(regionName, regionTransform));
        }
    }

    private void OpenRegion(string regionName, Transform regionTransform)
    {
        if (uiManager == null)
        {
            BindSceneObjects();
        }

        if (regionTransform == null)
        {
            regionTransform = FindRegionTransform(regionName);
        }

        if (uiManager != null && regionTransform != null)
        {
            uiManager.ShowTerrainPanel(regionTransform.name, regionTransform);
        }
    }

    private Transform FindRegionTransform(string regionName)
    {
        if (seoulRoot == null || string.IsNullOrEmpty(regionName))
        {
            return null;
        }

        return seoulRoot.Find(regionName);
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

    private void PlayPanelOpenSfx()
    {
        if (uiSfxManager == null)
        {
            uiSfxManager = UISfxManager.Instance;
        }

        if (uiSfxManager != null)
        {
            uiSfxManager.PlayPanelOpen();
        }
    }
}
