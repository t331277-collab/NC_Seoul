using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialVoiceSynthesisManager : MonoBehaviour
{
    public delegate void SynthesisCompleteHandler();
    private VarcoVoiceClient voiceClient;
    private AudioSource audioSource;
    private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
    private bool isDuckingBgm;

    private void Awake()
    {
        voiceClient = gameObject.AddComponent<VarcoVoiceClient>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void PlayDialogue(string text, string nodeId, string nextText, string nextNodeId, SynthesisCompleteHandler onComplete)
    {
        StartCoroutine(PlayAndPrefetchRoutine(text, nodeId, nextText, nextNodeId, onComplete));
    }

    public void StopDialogue()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        EndBgmDuck();
        StopAllCoroutines();
    }

    private void OnDisable()
    {
        EndBgmDuck();
    }

    private void OnDestroy()
    {
        EndBgmDuck();
    }

    private IEnumerator PlayAndPrefetchRoutine(string text, string nodeId, string nextText, string nextNodeId, SynthesisCompleteHandler onComplete)
    {
        // 1. Trigger prefetch for the NEXT node immediately (in the background)
        if (!string.IsNullOrEmpty(nextText) && !string.IsNullOrEmpty(nextNodeId) && !clipCache.ContainsKey(nextNodeId))
        {
            voiceClient.Synthesize(nextText, nextNodeId, clip => 
            {
                if (clip != null) clipCache[nextNodeId] = clip;
            }, err => Debug.LogError($"Prefetch error for {nextNodeId}: {err}"));
        }

        // 2. Play current node
        AudioClip currentClip = null;
        if (clipCache.ContainsKey(nodeId))
        {
            currentClip = clipCache[nodeId];
        }
        else
        {
            // If we don't have it, we must fetch it now and wait
            bool isFetching = true;
            voiceClient.Synthesize(text, nodeId, clip => 
            {
                currentClip = clip;
                if (clip != null) clipCache[nodeId] = clip;
                isFetching = false;
            }, err => 
            {
                Debug.LogError($"Synthesis error for {nodeId}: {err}");
                isFetching = false;
            });

            while (isFetching)
            {
                yield return null;
            }
        }

        if (currentClip != null)
        {
            audioSource.clip = currentClip;
            BeginBgmDuck();
            audioSource.Play();
            while (audioSource.isPlaying)
            {
                yield return null;
            }

            EndBgmDuck();
        }
        else
        {
            // Fallback duration if synthesis fails
            float duration = Mathf.Clamp(text.Length * 0.1f, 1f, 4f);
            yield return new WaitForSeconds(duration);
        }

        onComplete?.Invoke();
    }

    private void BeginBgmDuck()
    {
        if (isDuckingBgm)
        {
            return;
        }

        isDuckingBgm = true;
        InGameBgmManager.BeginVoicePlayback();
    }

    private void EndBgmDuck()
    {
        if (!isDuckingBgm)
        {
            return;
        }

        isDuckingBgm = false;
        InGameBgmManager.EndVoicePlayback();
    }
}
