using System.Collections;
using TMPro;
using UnityEngine;

public class ToastPopupManager : MonoBehaviour
{
    private const string MoneyShortageMessage = "돈이 모자랍니다!";
    private const string ScienceShortageMessage = "기술력이 모자랍니다!";
    private const string ScienceAndMoneyShortageMessage = "기술력, 돈이 모자릅니다!";

    [SerializeField] private string toastPath = "ToastPopUp";
    [SerializeField] private string textPath = "TXT";
    [SerializeField] private float dropDuration = 0.35f;
    [SerializeField] private float visibleDuration = 2f;
    [SerializeField] private float exitDuration = 0.15f;
    [SerializeField] private float hiddenOffset = 120f;

    private GameObject toastObject;
    private RectTransform toastRect;
    private TextMeshProUGUI toastText;
    private Vector2 visiblePosition;
    private Vector2 hiddenPosition;
    private Coroutine toastCoroutine;

    private void Awake()
    {
        BindSceneObjects();
        HideImmediately();
    }

    public void ShowMoneyShortage()
    {
        Show(MoneyShortageMessage);
    }

    public void ShowScienceShortage()
    {
        Show(ScienceShortageMessage);
    }

    public void ShowScienceAndMoneyShortage()
    {
        Show(ScienceAndMoneyShortageMessage);
    }

    public void Show(string message)
    {
        BindSceneObjects();
        if (toastObject == null || toastRect == null)
        {
            return;
        }

        if (toastText != null)
        {
            toastText.text = message;
        }

        if (toastCoroutine != null)
        {
            StopCoroutine(toastCoroutine);
        }

        toastCoroutine = StartCoroutine(ShowToastRoutine());
    }

    private IEnumerator ShowToastRoutine()
    {
        toastObject.SetActive(true);
        toastRect.anchoredPosition = hiddenPosition;

        yield return MoveToast(hiddenPosition, visiblePosition, dropDuration);
        yield return new WaitForSeconds(visibleDuration);
        yield return MoveToast(visiblePosition, hiddenPosition, exitDuration);

        toastObject.SetActive(false);
        toastCoroutine = null;
    }

    private IEnumerator MoveToast(Vector2 from, Vector2 to, float duration)
    {
        if (duration <= 0f)
        {
            toastRect.anchoredPosition = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            toastRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        toastRect.anchoredPosition = to;
    }

    private void HideImmediately()
    {
        if (toastObject == null || toastRect == null)
        {
            return;
        }

        toastRect.anchoredPosition = hiddenPosition;
        toastObject.SetActive(false);
    }

    private void BindSceneObjects()
    {
        Transform toastTransform = transform.Find(toastPath);
        if (toastTransform == null)
        {
            toastObject = null;
            toastRect = null;
            toastText = null;
            return;
        }

        toastObject = toastTransform.gameObject;
        toastRect = toastTransform.GetComponent<RectTransform>();
        if (toastRect != null && visiblePosition == Vector2.zero)
        {
            visiblePosition = toastRect.anchoredPosition;
            hiddenPosition = visiblePosition + Vector2.up * (toastRect.rect.height + hiddenOffset);
        }

        Transform textTransform = toastTransform.Find(textPath);
        toastText = textTransform == null ? null : textTransform.GetComponent<TextMeshProUGUI>();
    }
}
