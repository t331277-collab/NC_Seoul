using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainSceneTitleIntroController : MonoBehaviour
{
    [SerializeField] private Transform titlePanel;
    [SerializeField] private CanvasGroup summaryCanvasGroup;
    [SerializeField] private CanvasGroup gameStartButtonCanvasGroup;
    [SerializeField] private Button gameStartButton;
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioSource bgmAudioSource;

    [SerializeField] private float summaryDelay = 4f;
    [SerializeField] private float summaryFadeDuration = 4f;
    [SerializeField] private float gameStartButtonDelay = 15f;
    [SerializeField] private float gameStartButtonFadeDuration = 1f;

    private void Start()
    {
        BindSceneObjects();
        InitializeUiState();
        PlayBgm();
        StartCoroutine(PlayIntroSequence());
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

        if (titlePanel != null)
        {
            if (summaryCanvasGroup == null)
            {
                Transform summary = titlePanel.Find("Summary");
                summaryCanvasGroup = EnsureCanvasGroup(summary);
            }

            if (gameStartButtonCanvasGroup == null || gameStartButton == null)
            {
                Transform buttonTransform = titlePanel.Find("GameStartBtn");
                if (gameStartButtonCanvasGroup == null)
                {
                    gameStartButtonCanvasGroup = EnsureCanvasGroup(buttonTransform);
                }

                if (gameStartButton == null && buttonTransform != null)
                {
                    gameStartButton = buttonTransform.GetComponent<Button>();
                }
            }
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

    private void InitializeUiState()
    {
        SetCanvasGroup(summaryCanvasGroup, 0f, true, false);
        SetCanvasGroup(gameStartButtonCanvasGroup, 0f, false, false);

        if (summaryCanvasGroup != null)
        {
            summaryCanvasGroup.gameObject.SetActive(true);
        }

        if (gameStartButtonCanvasGroup != null)
        {
            gameStartButtonCanvasGroup.gameObject.SetActive(true);
        }

        if (gameStartButton != null)
        {
            gameStartButton.interactable = false;
        }
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

    private IEnumerator PlayIntroSequence()
    {
        yield return new WaitForSeconds(summaryDelay);
        yield return FadeCanvasGroup(summaryCanvasGroup, 0f, 1f, summaryFadeDuration);
        SetCanvasGroup(summaryCanvasGroup, 1f, true, true);

        float buttonWait = Mathf.Max(0f, gameStartButtonDelay - summaryDelay - summaryFadeDuration);
        yield return new WaitForSeconds(buttonWait);
        yield return FadeCanvasGroup(gameStartButtonCanvasGroup, 0f, 1f, gameStartButtonFadeDuration);
        SetCanvasGroup(gameStartButtonCanvasGroup, 1f, true, true);

        if (gameStartButton != null)
        {
            gameStartButton.interactable = true;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float fromAlpha, float toAlpha, float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float elapsed = 0f;
        SetCanvasGroup(canvasGroup, fromAlpha, false, false);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            SetCanvasGroup(canvasGroup, Mathf.Lerp(fromAlpha, toAlpha, progress), false, false);
            yield return null;
        }

        SetCanvasGroup(canvasGroup, toAlpha, false, false);
    }

    private void SetCanvasGroup(CanvasGroup canvasGroup, float alpha, bool interactable, bool blocksRaycasts)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = alpha;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = blocksRaycasts;
    }
}
