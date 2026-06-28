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
    private RectTransform templateRect;
    private Button inforButton;
    private Button closeButton;
    private Coroutine imageMoveCoroutine;

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
            Transform templateTransform = contentTransform == null ? null : contentTransform.Find("Template");
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
        SetActive(inforPanel, true);
    }

    private void CloseInfoPanel()
    {
        SetActive(inforPanel, false);
    }

    private void HideIntroText()
    {
        SetActive(inforTextObject, false);
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

        GameObject itemObject = Instantiate(templateRect.gameObject, contentRect);
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
        if (contentRect == null || templateRect == null)
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
        Vector2 sizeDelta = contentRect.sizeDelta;
        sizeDelta.y = Mathf.Max(sizeDelta.y, requiredHeight);
        contentRect.sizeDelta = sizeDelta;
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
}
