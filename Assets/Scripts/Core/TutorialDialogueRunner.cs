using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

[Serializable]
public class TutorialDialogueNode
{
    public string id;
    public string text;
    public bool lockInput;
    public string onCompleteSetFlag;
    public string next;
    
    public string type;
    public string instruction;
    public string allowedTargetPath;
    public string onActionSetFlag;
}

[Serializable]
public class TutorialDialogueFlow
{
    public string startNodeId;
    public List<TutorialDialogueNode> nodes;
}

public class TutorialDialogueRunner : MonoBehaviour
{
    public static TutorialDialogueRunner Instance { get; private set; }
    public string flowJsonPath = "Data/TutorialDialogueFlow.json";
    
    private TutorialDialogueFlow flow;
    private Dictionary<string, TutorialDialogueNode> nodeDict = new Dictionary<string, TutorialDialogueNode>();
    private TutorialDialogueNode currentNode;
    
    private TutorialVoiceSynthesisManager voiceManager;
    private TutorialInputLockManager inputManager;

    private bool isWaitingForVoice = false;
    private bool isWaitingForAction = false;
    private GameObject tutorialPromptPanel;

    private void Awake()
    {
        Instance = this;
        voiceManager = gameObject.AddComponent<TutorialVoiceSynthesisManager>();
        inputManager = gameObject.AddComponent<TutorialInputLockManager>();
        LoadFlow();
    }

    private void Start()
    {
        StartTutorial();
    }

    private void LoadFlow()
    {
        string path = Path.Combine(Application.dataPath, flowJsonPath);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            flow = JsonUtility.FromJson<TutorialDialogueFlow>(json);
            if (flow != null && flow.nodes != null)
            {
                foreach (var node in flow.nodes)
                {
                    nodeDict[node.id] = node;
                }
            }
        }
        else
        {
            Debug.LogError($"TutorialDialogueFlow.json not found at: {path}");
        }
    }

    public void StartTutorial()
    {
        if (GameSessionData.SkipTutorial)
        {
            Debug.Log("[튜토리얼 스킵] 유저가 튜토리얼 스킵을 선택했습니다.");
            inputManager.UnlockInput();
            return;
        }

        TutorialFlagStore.ClearAll();
        if (flow != null && !string.IsNullOrEmpty(flow.startNodeId))
        {
            RunNode(flow.startNodeId);
        }
    }

    private void Update()
    {
        if (isWaitingForVoice && Input.GetKeyDown(KeyCode.Return))
        {
            SkipVoice();
        }
    }

    private void RunNode(string nodeId)
    {
        if (!nodeDict.TryGetValue(nodeId, out currentNode))
        {
            inputManager.UnlockInput();
            Debug.Log("[튜토리얼 종료] 더 이상 진행할 노드가 없습니다.");
            return;
        }

        if (currentNode.type == "showSkipPrompt")
        {
            isWaitingForVoice = false;
            isWaitingForAction = true;
            inputManager.LockInput();
            ShowTutorialPrompt();
            return;
        }

        if (currentNode.type == "waitForAction")
        {
            isWaitingForVoice = false;
            isWaitingForAction = true;
            
            Debug.Log($"[행동 대기중]: {currentNode.instruction}");
            BindActionGate(currentNode.allowedTargetPath);
            return;
        }

        if (currentNode.type == "waitForDemolition")
        {
            isWaitingForVoice = false;
            isWaitingForAction = true;
            inputManager.UnlockInput();
            Debug.Log($"[철거 대기중]: {currentNode.instruction}");
            return;
        }

        isWaitingForAction = false;
        
        if (currentNode.lockInput)
            inputManager.LockInput();
        else
            inputManager.UnlockInput();

        string finalSentence = currentNode.text.Replace("{playerName}", GameSessionData.PlayerName);
        isWaitingForVoice = true;
        
        string nextSentence = "";
        string nextNodeId = "";
        if (!string.IsNullOrEmpty(currentNode.next) && nodeDict.TryGetValue(currentNode.next, out TutorialDialogueNode nextNode))
        {
            if (nextNode.type != "waitForAction" && nextNode.type != "showSkipPrompt" && !string.IsNullOrEmpty(nextNode.text))
            {
                nextNodeId = nextNode.id;
                nextSentence = nextNode.text.Replace("{playerName}", GameSessionData.PlayerName);
            }
        }

        voiceManager.PlayDialogue(finalSentence, currentNode.id, nextSentence, nextNodeId, OnVoiceComplete);
    }

    private void OnVoiceComplete()
    {
        isWaitingForVoice = false;
        if (!string.IsNullOrEmpty(currentNode.onCompleteSetFlag))
        {
            TutorialFlagStore.SetFlag(currentNode.onCompleteSetFlag);
        }
        
        if (!string.IsNullOrEmpty(currentNode.next))
        {
            RunNode(currentNode.next);
        }
        else
        {
            inputManager.UnlockInput();
            Debug.Log("[튜토리얼 종료]");
        }
    }

    private void SkipVoice()
    {
        voiceManager.StopDialogue();
        OnVoiceComplete();
    }

    private void BindActionGate(string targetPath)
    {
        GameObject target = GameObject.Find(targetPath);
        if (target != null)
        {
            RectTransform targetRect = target.GetComponent<RectTransform>();
            inputManager.LockWithException(targetRect);

            Button btn = target.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(OnActionGateTriggered);
            }
            else
            {
                Debug.LogWarning($"[ActionGate] Target {targetPath} does not have a Button component!");
            }
        }
        else
        {
            Debug.LogWarning($"[ActionGate] Target {targetPath} not found in scene!");
            // Auto skip if target doesn't exist to prevent hard lock
            OnActionGateTriggered();
        }
    }

    private void OnActionGateTriggered()
    {
        if (!isWaitingForAction) return;
        
        GameObject target = GameObject.Find(currentNode.allowedTargetPath);
        if (target != null)
        {
            Button btn = target.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveListener(OnActionGateTriggered);
        }

        isWaitingForAction = false;
        inputManager.LockInput();
        
        if (!string.IsNullOrEmpty(currentNode.onActionSetFlag))
        {
            TutorialFlagStore.SetFlag(currentNode.onActionSetFlag);
        }

        if (!string.IsNullOrEmpty(currentNode.next))
        {
            RunNode(currentNode.next);
        }
        else
        {
            inputManager.UnlockInput();
            Debug.Log("[튜토리얼 종료]");
        }
    }

    public void NotifyStructureDemolished(string structureId)
    {
        if (isWaitingForAction && currentNode != null && currentNode.type == "waitForDemolition" && currentNode.allowedTargetPath == structureId)
        {
            isWaitingForAction = false;
            inputManager.LockInput();
            if (!string.IsNullOrEmpty(currentNode.onActionSetFlag))
            {
                TutorialFlagStore.SetFlag(currentNode.onActionSetFlag);
            }
            if (!string.IsNullOrEmpty(currentNode.next))
            {
                RunNode(currentNode.next);
            }
        }
    }

    private void ShowTutorialPrompt()
    {
        if (tutorialPromptPanel == null)
        {
            CreateTutorialPromptPanel();
        }
        tutorialPromptPanel.SetActive(true);
    }

    private void CreateTutorialPromptPanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        tutorialPromptPanel = new GameObject("TutorialPromptPanel");
        tutorialPromptPanel.transform.SetParent(canvas.transform, false);
        tutorialPromptPanel.transform.SetAsLastSibling();
        
        RectTransform rect = tutorialPromptPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        
        Image bg = tutorialPromptPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f);
        bg.raycastTarget = true; // Block raycasts while prompt is active

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
        yesTmp.text = "진행 (추천)";
        yesTmp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        yesTmp.color = Color.black;
        yesTmp.fontSize = 24;
        yesTmp.alignment = TextAnchor.MiddleCenter;
        yesTmp.rectTransform.sizeDelta = yesRect.sizeDelta;

        btnYes.onClick.AddListener(() => {
            GameSessionData.SkipTutorial = false;
            tutorialPromptPanel.SetActive(false);
            isWaitingForAction = false;
            if (!string.IsNullOrEmpty(currentNode.next))
            {
                RunNode(currentNode.next);
            }
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
            tutorialPromptPanel.SetActive(false);
            inputManager.UnlockInput();
            Debug.Log("[튜토리얼 스킵] 유저가 튜토리얼 스킵을 선택했습니다.");
        });
    }
}
