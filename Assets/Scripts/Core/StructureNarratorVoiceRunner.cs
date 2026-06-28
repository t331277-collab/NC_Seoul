using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class StructureNarratorVoiceRunner : MonoBehaviour
{
    private Dictionary<string, List<string>> structureScriptMap = new Dictionary<string, List<string>>();
    private TutorialVoiceSynthesisManager voiceManager;
    private bool isCancelled = false;

    private void Awake()
    {
        voiceManager = GetComponent<TutorialVoiceSynthesisManager>();
        if (voiceManager == null)
        {
            voiceManager = gameObject.AddComponent<TutorialVoiceSynthesisManager>();
        }

        LoadNarratorScripts("Data/StructureNarratorScripts.txt");
    }

    private void LoadNarratorScripts(string relativePath)
    {
        structureScriptMap.Clear();
        string fullPath = Path.Combine(Application.dataPath, relativePath);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("[StructureNarratorVoiceRunner] Script file not found at: " + fullPath);
            return;
        }

        string[] lines = File.ReadAllLines(fullPath, Encoding.UTF8);
        string currentTag = null;
        bool collectingDialogue = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            // Check for tag e.g. [Stru_AmsaDong]
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentTag = line.Substring(1, line.Length - 2).Trim();
                collectingDialogue = false;
                if (!structureScriptMap.ContainsKey(currentTag))
                {
                    structureScriptMap[currentTag] = new List<string>();
                }
                continue;
            }

            if (currentTag == null)
            {
                continue;
            }

            if (line.Contains("나레이터 대사:"))
            {
                collectingDialogue = true;
                int idx = line.IndexOf("나레이터 대사:");
                line = line.Substring(idx + "나레이터 대사:".Length).Trim();
            }

            if (collectingDialogue && !string.IsNullOrEmpty(line))
            {
                // Clean quotes and formatting symbols
                line = line.Replace("“", "").Replace("”", "").Replace("\"", "").Replace("'", "").Replace("\\n", " ").Trim();
                if (line.StartsWith("“") || line.StartsWith("\"")) line = line.Substring(1).Trim();
                if (line.EndsWith("”") || line.EndsWith("\"")) line = line.Substring(0, line.Length - 1).Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    structureScriptMap[currentTag].Add(line);
                }
            }
        }

        Debug.Log($"[StructureNarratorVoiceRunner] Loaded scripts for {structureScriptMap.Count} structures.");
    }

    public void PlayStructureNarrator(string structureId)
    {
        StopNarrator();

        if (string.IsNullOrEmpty(structureId) || !structureScriptMap.TryGetValue(structureId, out List<string> sentences) || sentences.Count == 0)
        {
            return;
        }

        isCancelled = false;
        PlaySentenceIndex(structureId, sentences, 0);
    }

    private void PlaySentenceIndex(string structureId, List<string> sentences, int index)
    {
        if (isCancelled || index >= sentences.Count)
        {
            return;
        }

        string currentText = sentences[index];
        string currentNodeId = $"{structureId}_{index}";

        string nextText = (index + 1 < sentences.Count) ? sentences[index + 1] : null;
        string nextNodeId = (index + 1 < sentences.Count) ? $"{structureId}_{index + 1}" : null;

        voiceManager.PlayDialogue(currentText, currentNodeId, nextText, nextNodeId, () =>
        {
            if (!isCancelled)
            {
                PlaySentenceIndex(structureId, sentences, index + 1);
            }
        });
    }

    public void StopNarrator()
    {
        isCancelled = true;
        if (voiceManager != null)
        {
            voiceManager.StopDialogue();
        }
    }
}
