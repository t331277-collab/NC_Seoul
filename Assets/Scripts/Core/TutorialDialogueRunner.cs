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
    public string flowJsonPath = "Data/TutorialDialogueFlow.json";
    
    private TutorialDialogueFlow flow;
    private Dictionary<string, TutorialDialogueNode> nodeDict = new Dictionary<string, TutorialDialogueNode>();
    private TutorialDialogueNode currentNode;
    
    private TutorialVoiceSynthesisManager voiceManager;
    private TutorialInputLockManager inputManager;

    private bool isWaitingForVoice = false;
    private bool isWaitingForAction = false;

    private void Awake()
    {
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

        if (currentNode.type == "waitForAction")
        {
            isWaitingForVoice = false;
            isWaitingForAction = true;
            
            Debug.Log($"[행동 대기중]: {currentNode.instruction}");
            BindActionGate(currentNode.allowedTargetPath);
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
            if (nextNode.type != "waitForAction")
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
}
