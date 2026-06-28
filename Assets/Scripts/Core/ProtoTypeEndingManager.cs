using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProtoTypeEndingManager : MonoBehaviour
{
    private const string MainSceneName = "MainScene";

    [SerializeField] private string endingPanelPath = "ProtoTypeEndingPanel";
    [SerializeField] private string mainMenuButtonName = "MainMenuBtn";

    private GameObject endingPanel;
    private GameObject inputBlocker;
    private Button mainMenuButton;

    private void Awake()
    {
        BindSceneObjects();
        SetActive(inputBlocker, false);
        SetActive(endingPanel, false);
    }

    private void OnEnable()
    {
        BindSceneObjects();
        BindButtons();
    }

    private void OnDisable()
    {
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(LoadMainScene);
        }
    }

    public void ShowEnding()
    {
        BindSceneObjects();
        SetActive(inputBlocker, true);
        SetActive(endingPanel, true);

        if (inputBlocker != null)
        {
            inputBlocker.transform.SetAsLastSibling();
        }

        if (endingPanel != null)
        {
            endingPanel.transform.SetAsLastSibling();
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

        Transform panelTransform = uiRoot.Find(endingPanelPath);
        if (panelTransform != null)
        {
            endingPanel = panelTransform.gameObject;
            Transform buttonTransform = panelTransform.Find(mainMenuButtonName);
            mainMenuButton = buttonTransform == null ? null : buttonTransform.GetComponent<Button>();
        }

        EnsureInputBlocker(uiRoot);
    }

    private void EnsureInputBlocker(Transform uiRoot)
    {
        if (uiRoot == null)
        {
            return;
        }

        Transform blockerTransform = uiRoot.Find("ProtoTypeEndingInputBlocker");
        if (blockerTransform == null)
        {
            GameObject blockerObject = new GameObject("ProtoTypeEndingInputBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            blockerTransform = blockerObject.transform;
            blockerTransform.SetParent(uiRoot, false);

            RectTransform blockerRect = blockerObject.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            Image blockerImage = blockerObject.GetComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, 0f);
            blockerImage.raycastTarget = true;
        }

        inputBlocker = blockerTransform.gameObject;
    }

    private void BindButtons()
    {
        if (mainMenuButton == null)
        {
            return;
        }

        mainMenuButton.onClick.RemoveListener(LoadMainScene);
        mainMenuButton.onClick.AddListener(LoadMainScene);
    }

    private void LoadMainScene()
    {
        SceneManager.LoadScene(MainSceneName);
    }

    private void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}
