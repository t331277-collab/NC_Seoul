using System.Collections;
using UnityEngine;

public class TutorialVoiceSynthesisManager : MonoBehaviour
{
    public delegate void SynthesisCompleteHandler();

    public void PlayDialogue(string text, SynthesisCompleteHandler onComplete)
    {
        StartCoroutine(SimulateDialoguePlayback(text, onComplete));
    }

    public void StopDialogue()
    {
        StopAllCoroutines();
    }

    private IEnumerator SimulateDialoguePlayback(string text, SynthesisCompleteHandler onComplete)
    {
        Debug.Log($"[음성 재생 대기중]: {text}");
        
        // Simulate reading time (min 1 sec, max 4 sec)
        float duration = Mathf.Clamp(text.Length * 0.1f, 1f, 4f);
        yield return new WaitForSeconds(duration);
        
        onComplete?.Invoke();
    }
}
