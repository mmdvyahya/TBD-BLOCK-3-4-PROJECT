using System.Collections;
using UnityEngine;

// Central manager for playing music with support for crossfading between them. Make it persistant at starting scene
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; } // Used for accessing the manager from anywhere without needing a reference

    [Header("Audio Sources")]
    [SerializeField] AudioSource sourceA;
    [SerializeField] AudioSource sourceB;

    AudioSource activeSource;
    AudioSource inactiveSource;
    Coroutine crossfadeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        activeSource = sourceA;
        inactiveSource = sourceB;
    }

    // Play music with optional crossfade. If the same track is already playing, it will not restart.
    public void Play(SoundData data, float fadeDuration)
    {
        if (data == null) return;
        if (activeSource.isPlaying && activeSource.clip == data.GetClip()) return;

        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);

        if (fadeDuration <= 0f)
            PlayInstant(data);
        else
            crossfadeCoroutine = StartCoroutine(Crossfade(data, fadeDuration));
    }

    // Stop music with optional fade out
    public void Stop(float fadeDuration)
    {
        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);

        if (fadeDuration <= 0f)
        {
            activeSource.Stop();
            activeSource.volume = 0f;
        }
        else
            crossfadeCoroutine = StartCoroutine(FadeOut(activeSource, fadeDuration));
    }

    // Immediately play a new track without fading
    void PlayInstant(SoundData data)
    {
        activeSource.Stop();

        activeSource.clip = data.GetClip();
        activeSource.outputAudioMixerGroup = data.mixerGroup;
        activeSource.volume = data.GetVolume();
        activeSource.loop = data.loop;
        activeSource.Play();
    }

    // Crossfade to a new track
    IEnumerator Crossfade(SoundData data, float duration)
    {
        inactiveSource.clip = data.GetClip();
        inactiveSource.outputAudioMixerGroup = data.mixerGroup;
        inactiveSource.volume = 0f;
        inactiveSource.loop = data.loop;
        inactiveSource.Play();

        float t = 0f;
        float startVolume = activeSource.volume;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;

            activeSource.volume = Mathf.Lerp(startVolume, 0f, progress);
            inactiveSource.volume = Mathf.Lerp(0f, data.GetVolume(), progress);

            yield return null;
        }

        activeSource.Stop();
        activeSource.volume = 0f;

        (activeSource, inactiveSource) = (inactiveSource, activeSource);

        crossfadeCoroutine = null;
    }

    // Fade out the current track
    IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }

        source.Stop();
        source.volume = 0f;
        crossfadeCoroutine = null;
    }
}