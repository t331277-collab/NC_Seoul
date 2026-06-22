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

        SceneManager.LoadScene(InGameSceneName);
    }

    private void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}
