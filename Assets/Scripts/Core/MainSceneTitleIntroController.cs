using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainSceneTitleIntroController : MonoBehaviour
{
    [SerializeField] private Transform titlePanel;
    [SerializeField] private CanvasGroup sumCanvasGroup;
    [SerializeField] private CanvasGroup gameStartButtonCanvasGroup;
    [SerializeField] private Button gameStartButton;
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioSource bgmAudioSource;

    [SerializeField] private float sumDelay = 4f;
    [SerializeField] private float sumFadeDuration = 1f;
    [SerializeField] private float gameStartButtonDelay = 16f;
    [SerializeField] private float gameStartButtonFadeDuration = 1f;

    private Coroutine sumFadeCoroutine;
    private Coroutine gameStartButtonFadeCoroutine;

    private void Start()
    {
        BindSceneObjects();
        InitializeFadeTarget(sumCanvasGroup, false);
        InitializeFadeTarget(gameStartButtonCanvasGroup, true);
        PlayBgm();

        sumFadeCoroutine = StartCoroutine(FadeInAfterDelay(sumCanvasGroup, sumDelay, sumFadeDuration, false));
        gameStartButtonFadeCoroutine = StartCoroutine(FadeInAfterDelay(gameStartButtonCanvasGroup, gameStartButtonDelay, gameStartButtonFadeDuration, true));
    }

    private void OnDisable()
    {
        StopFadeCoroutine(ref sumFadeCoroutine);
        StopFadeCoroutine(ref gameStartButtonFadeCoroutine);
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

        if (titlePanel == null && uiRoot != null)
        {
            titlePanel = uiRoot.Find("TitlePanel");
        }

        if (titlePanel == null)
        {
            return;
        }

        if (sumCanvasGroup == null)
        {
            sumCanvasGroup = EnsureCanvasGroup(titlePanel.Find("Sum"));
        }

        Transform gameStartButtonTransform = titlePanel.Find("GameStartBtn");
        if (gameStartButtonCanvasGroup == null)
        {
            gameStartButtonCanvasGroup = EnsureCanvasGroup(gameStartButtonTransform);
        }

        if (gameStartButton == null && gameStartButtonTransform != null)
        {
            gameStartButton = gameStartButtonTransform.GetComponent<Button>();
        }

        if (bgmAudioSource == null)
        {
            bgmAudioSource = GetComponent<AudioSource>();
            if (bgmAudioSource == null)
            {
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private CanvasGroup EnsureCanvasGroup(Transform target)
    {
        if (target == null)
        {
            return null;
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private void InitializeFadeTarget(CanvasGroup canvasGroup, bool lockInteraction)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = !lockInteraction;
        canvasGroup.blocksRaycasts = !lockInteraction;
    }

    private void PlayBgm()
    {
        if (bgmAudioSource == null || bgmClip == null)
        {
            return;
        }

        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.loop = true;
        bgmAudioSource.playOnAwake = false;
        if (!bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Play();
        }
    }

    private IEnumerator FadeInAfterDelay(CanvasGroup canvasGroup, float delay, float duration, bool unlockInteraction)
    {
        yield return new WaitForSeconds(delay);

        if (canvasGroup == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (unlockInteraction)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            if (gameStartButton != null)
            {
                gameStartButton.interactable = true;
            }
        }
    }

    private void StopFadeCoroutine(ref Coroutine fadeCoroutine)
    {
        if (fadeCoroutine == null)
        {
            return;
        }

        StopCoroutine(fadeCoroutine);
        fadeCoroutine = null;
    }
}
