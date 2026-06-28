using System.Collections;
using UnityEngine;

public class InGameBgmManager : MonoBehaviour
{
    private static InGameBgmManager instance;

    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip[] playlist;
    [SerializeField] private float normalVolume = 1f;
    [SerializeField] private float duckedVolume = 0.2f;
    [SerializeField] private float restoreDuration = 1f;

    private int currentTrackIndex;
    private int activeDuckRequests;
    private Coroutine playlistRoutine;
    private Coroutine volumeRoutine;

    public static InGameBgmManager Instance
    {
        get { return instance; }
    }

    private void Awake()
    {
        instance = this;
        EnsureAudioSource();
    }

    private void Start()
    {
        Play();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static void BeginVoicePlayback()
    {
        if (instance != null)
        {
            instance.BeginDuck();
        }
    }

    public static void EndVoicePlayback()
    {
        if (instance != null)
        {
            instance.EndDuck();
        }
    }

    public void Play()
    {
        EnsureAudioSource();
        if (bgmAudioSource == null || playlist == null || playlist.Length == 0)
        {
            return;
        }

        if (playlistRoutine == null)
        {
            playlistRoutine = StartCoroutine(PlayPlaylistRoutine());
        }
    }

    private IEnumerator PlayPlaylistRoutine()
    {
        while (true)
        {
            AudioClip clip = GetNextPlayableClip();
            if (clip == null)
            {
                playlistRoutine = null;
                yield break;
            }

            bgmAudioSource.clip = clip;
            bgmAudioSource.loop = false;
            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.volume = activeDuckRequests > 0 ? duckedVolume : normalVolume;
            bgmAudioSource.Play();

            while (bgmAudioSource != null && bgmAudioSource.isPlaying)
            {
                yield return null;
            }

            yield return null;
        }
    }

    private AudioClip GetNextPlayableClip()
    {
        if (playlist == null || playlist.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < playlist.Length; i++)
        {
            int index = currentTrackIndex;
            currentTrackIndex = (currentTrackIndex + 1) % playlist.Length;
            if (playlist[index] != null)
            {
                return playlist[index];
            }
        }

        return null;
    }

    private void BeginDuck()
    {
        activeDuckRequests++;
        SetVolumeImmediate(duckedVolume);
    }

    private void EndDuck()
    {
        activeDuckRequests = Mathf.Max(0, activeDuckRequests - 1);
        if (activeDuckRequests == 0)
        {
            FadeVolume(normalVolume, restoreDuration);
        }
    }

    private void SetVolumeImmediate(float targetVolume)
    {
        EnsureAudioSource();
        if (bgmAudioSource == null)
        {
            return;
        }

        if (volumeRoutine != null)
        {
            StopCoroutine(volumeRoutine);
            volumeRoutine = null;
        }

        bgmAudioSource.volume = targetVolume;
    }

    private void FadeVolume(float targetVolume, float duration)
    {
        EnsureAudioSource();
        if (bgmAudioSource == null)
        {
            return;
        }

        if (volumeRoutine != null)
        {
            StopCoroutine(volumeRoutine);
        }

        volumeRoutine = StartCoroutine(FadeVolumeRoutine(targetVolume, duration));
    }

    private IEnumerator FadeVolumeRoutine(float targetVolume, float duration)
    {
        float startVolume = bgmAudioSource.volume;
        if (duration <= 0f)
        {
            bgmAudioSource.volume = targetVolume;
            volumeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bgmAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        bgmAudioSource.volume = targetVolume;
        volumeRoutine = null;
    }

    private void EnsureAudioSource()
    {
        if (bgmAudioSource == null)
        {
            bgmAudioSource = GetComponent<AudioSource>();
        }

        if (bgmAudioSource == null)
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
        }
    }
}
