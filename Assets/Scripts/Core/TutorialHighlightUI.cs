using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialHighlightUI : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image outlineImage;

    private void Awake()
    {
        outlineImage = gameObject.AddComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        outlineImage.color = new Color(1f, 1f, 0f, 0.5f);
        outlineImage.raycastTarget = false; // Never block input
    }

    public void ShowHighlight(RectTransform target)
    {
        gameObject.SetActive(true);
        transform.SetParent(target, false);
        
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        StartCoroutine(BlinkRoutine());
    }

    public void HideHighlight()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
        transform.SetParent(null, false);
    }

    private IEnumerator BlinkRoutine()
    {
        while(true)
        {
            outlineImage.color = new Color(1f, 0.8f, 0f, 0.6f);
            yield return new WaitForSeconds(0.4f);
            outlineImage.color = new Color(1f, 0.8f, 0f, 0.1f);
            yield return new WaitForSeconds(0.4f);
        }
    }
}
