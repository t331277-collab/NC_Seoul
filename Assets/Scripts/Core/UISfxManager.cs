using UnityEngine;

public class UISfxManager : MonoBehaviour
{
    private static UISfxManager instance;

    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip uiClickClip;
    [SerializeField] private AudioClip constructClip;
    [SerializeField] private AudioClip policyAcceptedClip;
    [SerializeField] private AudioClip acceptedClip;

    public static UISfxManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UISfxManager>();
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            return;
        }

        instance = this;
        EnsureAudioSource();
    }

    public void PlayPanelOpen()
    {
        PlayOneShot(uiClickClip);
    }

    public void PlayConstruct()
    {
        PlayOneShot(constructClip);
    }

    public void PlayPolicyAccepted()
    {
        PlayOneShot(policyAcceptedClip);
    }

    public void PlayAccepted()
    {
        PlayOneShot(acceptedClip);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        EnsureAudioSource();
        if (sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }

    private void EnsureAudioSource()
    {
        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();
        }

        if (sfxAudioSource == null)
        {
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }

        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.loop = false;
    }
}
