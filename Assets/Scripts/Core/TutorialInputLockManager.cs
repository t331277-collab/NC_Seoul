using UnityEngine;
using UnityEngine.UI;

public class TutorialInputLockManager : MonoBehaviour
{
    private bool isLocked = false;
    private GameObject blockerObj;
    private TutorialInputBlocker blocker;
    private TutorialHighlightUI highlightUI;

    private void Awake()
    {
        CreateBlocker();
        CreateHighlight();
    }

    private void CreateBlocker()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            blockerObj = new GameObject("TutorialInputBlocker");
            blockerObj.transform.SetParent(canvas.transform, false);
            blockerObj.transform.SetAsLastSibling(); 
            
            RectTransform rect = blockerObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            blocker = blockerObj.AddComponent<TutorialInputBlocker>();
            blocker.color = new Color(0, 0, 0, 0); // Transparent blocker
            blocker.raycastTarget = true;
            
            blockerObj.SetActive(false);
        }
    }

    private void CreateHighlight()
    {
        GameObject go = new GameObject("TutorialHighlight");
        highlightUI = go.AddComponent<TutorialHighlightUI>();
        highlightUI.HideHighlight();
    }

    public void LockInput()
    {
        isLocked = true;
        if (blocker != null)
        {
            blocker.AllowedTargetRect = null;
            blockerObj.transform.SetAsLastSibling();
            blockerObj.SetActive(true);
        }
        if (highlightUI != null) highlightUI.HideHighlight();
    }

    public void LockWithException(RectTransform target)
    {
        isLocked = true;
        if (blocker != null)
        {
            blocker.AllowedTargetRect = target;
            blockerObj.transform.SetAsLastSibling();
            blockerObj.SetActive(true);
        }
        
        if (highlightUI != null && target != null)
        {
            highlightUI.ShowHighlight(target);
        }
        else if (highlightUI != null)
        {
            highlightUI.HideHighlight();
        }
    }

    public void UnlockInput()
    {
        isLocked = false;
        if (blocker != null)
        {
            blocker.AllowedTargetRect = null;
            blockerObj.SetActive(false);
        }
        if (highlightUI != null) highlightUI.HideHighlight();
    }
}
