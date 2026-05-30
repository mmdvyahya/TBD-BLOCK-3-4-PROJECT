using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Central manager for playing all sounds in the game. Make it persistant at starting scene
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; } // Used for accessing the manager from anywhere without needing a reference

    [Tooltip("How many AudioSources to pre-create in the pool. Useful for limiting the number of sounds that can play simultaneously.")]
    [SerializeField] int poolSize = 10;

    List<AudioSource> pool = new List<AudioSource>();

    HashSet<SoundData> playingSounds = new HashSet<SoundData>(); // Tracks which SoundData assets are currently playing to support preventDuplicates
    Dictionary<SoundData, AudioSource> playingSources = new Dictionary<SoundData, AudioSource>(); // Maps each SoundData to the source currently playing it
    Dictionary<SoundData, Coroutine> releaseCoroutines = new Dictionary<SoundData, Coroutine>(); // Tracks release coroutines so they can be cancelled on Stop

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Pre-create pooled AudioSources
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"PooledSource ({i})");
            go.transform.parent = transform;
            pool.Add(go.AddComponent<AudioSource>());
        }
    }

    // Play a sound at 2D or at a world position for 3D sounds
    public void Play(SoundData data, Vector3? position = null)
    {
        if (data == null) return;

        // Skip if already playing and preventDuplicates is on
        if (data.preventDuplicates && playingSounds.Contains(data)) return;

        AudioClip clip = data.GetClip();
        if (clip == null) return;

        AudioSource source = GetAvailableSource();
        if (source == null) return;

        // Apply settings from SoundData
        source.clip = clip;
        source.outputAudioMixerGroup = data.mixerGroup;
        source.volume = data.GetVolume();
        source.pitch = data.GetPitch();
        source.loop = data.loop;
        source.spatialBlend = data.spatialBlend;
        source.minDistance = data.minDistance;
        source.maxDistance = data.maxDistance;
        source.rolloffMode = data.rolloffMode;

        if (position.HasValue)
            source.transform.position = position.Value;

        source.Play();
        playingSounds.Add(data);
        playingSources[data] = source;

        // If not looping, release the source back to the pool after the clip ends
        if (!data.loop)
        {
            var coroutine = StartCoroutine(ReleaseAfter(data, source, clip.length));
            releaseCoroutines[data] = coroutine;
        }
    }

    // Stop a currently playing sound immediately
    public void Stop(SoundData data)
    {
        if (playingSources.TryGetValue(data, out AudioSource source))
        {
            // Cancel the release coroutine so it can't interfere with future sounds
            if (releaseCoroutines.TryGetValue(data, out Coroutine coroutine))
            {
                StopCoroutine(coroutine);
                releaseCoroutines.Remove(data);
            }

            source.Stop();
            playingSources.Remove(data);
            playingSounds.Remove(data);
        }
    }

    // Fade out and stop a currently playing sound
    public void FadeOut(SoundData data, float duration = 0.1f)
    {
        if (playingSources.TryGetValue(data, out AudioSource source))
        {
            // Cancel the release coroutine so it can't interfere
            if (releaseCoroutines.TryGetValue(data, out Coroutine coroutine))
            {
                StopCoroutine(coroutine);
                releaseCoroutines.Remove(data);
            }

            playingSounds.Remove(data);
            playingSources.Remove(data);

            StartCoroutine(FadeOutCoroutine(source, duration));
        }
    }

    IEnumerator FadeOutCoroutine(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.Stop();
        source.volume = startVolume; // Reset for when the source is reused
    }

    AudioSource GetAvailableSource()
    {
        foreach (var source in pool)
            if (!source.isPlaying) return source;

        Debug.LogWarning("SoundManager: No available AudioSources in the pool!");
        return null;
    }

    // Release a non-looping sound after it finishes playing naturally
    IEnumerator ReleaseAfter(SoundData data, AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        playingSounds.Remove(data);
        playingSources.Remove(data);
        releaseCoroutines.Remove(data);
        source.Stop();
    }
}