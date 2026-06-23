using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    private const string InGameSceneName = "InGameScene";

    private GameObject titleGameStartButtonObject;
    private GameObject gameStartPanel;
    private GameObject gameStartButtonObject;
    private Button titleGameStartButton;
    private Button gameStartButton;
    private TMP_InputField nameInput;
    private GameObject tutorialPromptPanel;
    private void Awake()
    {
        BindSceneObjects();
        InitializeState();
    }

    private void OnEnable()
    {
        BindSceneObjects();
        AddListeners();
    }

    private void OnDisable()
    {
        RemoveListeners();
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

        Transform titleButtonTransform = uiRoot.Find("TitlePanel/GameStartBtn");
        if (titleButtonTransform != null)
        {
            titleGameStartButtonObject = titleButtonTransform.gameObject;
            titleGameStartButton = titleButtonTransform.GetComponent<Button>();
        }

        Transform gameStartPanelTransform = uiRoot.Find("GameStartPanel");
        if (gameStartPanelTransform != null)
        {
            gameStartPanel = gameStartPanelTransform.gameObject;

            Transform nameInputTransform = gameStartPanelTransform.Find("NameInput");
            if (nameInputTransform != null)
            {
                nameInput = nameInputTransform.GetComponent<TMP_InputField>();
            }

            Transform gameStartButtonTransform = gameStartPanelTransform.Find("GameStartBtn");
            if (gameStartButtonTransform != null)
            {
                gameStartButtonObject = gameStartButtonTransform.gameObject;
                gameStartButton = gameStartButtonTransform.GetComponent<Button>();
            }
        }
    }

    private void InitializeState()
    {
        SetActive(titleGameStartButtonObject, true);
        SetActive(gameStartPanel, false);
        SetActive(gameStartButtonObject, false);
    }

    private void AddListeners()
    {
        if (titleGameStartButton != null)
        {
            titleGameStartButton.onClick.RemoveListener(OpenGameStartPanel);
            titleGameStartButton.onClick.AddListener(OpenGameStartPanel);
        }

        if (nameInput != null)
        {
            nameInput.onValueChanged.RemoveListener(OnNameInputChanged);
            nameInput.onValueChanged.AddListener(OnNameInputChanged);
        }

        if (gameStartButton != null)
        {
            gameStartButton.onClick.RemoveListener(LoadInGameScene);
            gameStartButton.onClick.AddListener(LoadInGameScene);
        }
    }

    private void RemoveListeners()
    {
        if (titleGameStartButton != null)
        {
            titleGameStartButton.onClick.RemoveListener(OpenGameStartPanel);
        }

        if (nameInput != null)
        {
            nameInput.onValueChanged.RemoveListener(OnNameInputChanged);
        }

        if (gameStartButton != null)
        {
            gameStartButton.onClick.RemoveListener(LoadInGameScene);
        }
    }

    private void OpenGameStartPanel()
    {
        SetActive(titleGameStartButtonObject, false);
        SetActive(gameStartPanel, true);

        if (nameInput != null)
        {
            nameInput.text = string.Empty;
        }

        UpdateGameStartButtonState();
    }

    private void OnNameInputChanged(string value)
    {
        UpdateGameStartButtonState();
    }

    private void UpdateGameStartButtonState()
    {
        SetActive(gameStartButtonObject, HasNameInputText());
    }

    private bool HasNameInputText()
    {
        return nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text);
    }

    private void LoadInGameScene()
    {
        if (!HasNameInputText())
        {
            return;
        }

        GameSessionData.PlayerName = nameInput.text.Trim();
        
        SetActive(gameStartPanel, false);

        if (tutorialPromptPanel == null)
        {
            CreateTutorialPromptPanel();
        }
        SetActive(tutorialPromptPanel, true);
    }

    private void CreateTutorialPromptPanel()
    {
        tutorialPromptPanel = new GameObject("TutorialPromptPanel");
        tutorialPromptPanel.transform.SetParent(gameStartPanel.transform.parent, false);
        tutorialPromptPanel.transform.SetAsLastSibling();
        
        RectTransform rect = tutorialPromptPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        
        Image bg = tutorialPromptPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f);

        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(tutorialPromptPanel.transform, false);
        UnityEngine.UI.Text tmp = textObj.AddComponent<UnityEngine.UI.Text>();
        tmp.text = "튜토리얼을 진행하시겠습니까?";
        tmp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tmp.alignment = TextAnchor.MiddleCenter;
        tmp.fontSize = 40;
        tmp.color = Color.white;
        RectTransform textRect = tmp.rectTransform;
        textRect.anchoredPosition = new Vector2(0, 100);
        textRect.sizeDelta = new Vector2(800, 100);

        GameObject yesObj = new GameObject("BtnYes");
        yesObj.transform.SetParent(tutorialPromptPanel.transform, false);
        Image yesImg = yesObj.AddComponent<Image>();
        yesImg.color = Color.white;
        Button btnYes = yesObj.AddComponent<Button>();
        RectTransform yesRect = yesObj.GetComponent<RectTransform>();
        yesRect.anchoredPosition = new Vector2(-150, -50);
        yesRect.sizeDelta = new Vector2(200, 80);
        
        GameObject yesTextObj = new GameObject("Text");
        yesTextObj.transform.SetParent(yesObj.transform, false);
        UnityEngine.UI.Text yesTmp = yesTextObj.AddComponent<UnityEngine.UI.Text>();
        yesTmp.text = "진행";
        yesTmp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        yesTmp.color = Color.black;
        yesTmp.fontSize = 24;
        yesTmp.alignment = TextAnchor.MiddleCenter;
        yesTmp.rectTransform.sizeDelta = yesRect.sizeDelta;

        btnYes.onClick.AddListener(() => {
            GameSessionData.SkipTutorial = false;
            SceneManager.LoadScene(InGameSceneName);
        });

        GameObject noObj = new GameObject("BtnNo");
        noObj.transform.SetParent(tutorialPromptPanel.transform, false);
        Image noImg = noObj.AddComponent<Image>();
        noImg.color = Color.gray;
        Button btnNo = noObj.AddComponent<Button>();
        RectTransform noRect = noObj.GetComponent<RectTransform>();
        noRect.anchoredPosition = new Vector2(150, -50);
        noRect.sizeDelta = new Vector2(200, 80);

        GameObject noTextObj = new GameObject("Text");
        noTextObj.transform.SetParent(noObj.transform, false);
        UnityEngine.UI.Text noTmp = noTextObj.AddComponent<UnityEngine.UI.Text>();
        noTmp.text = "스킵";
        noTmp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        noTmp.color = Color.black;
        noTmp.fontSize = 24;
        noTmp.alignment = TextAnchor.MiddleCenter;
        noTmp.rectTransform.sizeDelta = noRect.sizeDelta;

        btnNo.onClick.AddListener(() => {
            GameSessionData.SkipTutorial = true;
            SceneManager.LoadScene(InGameSceneName);
        });
    }

    private void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}
