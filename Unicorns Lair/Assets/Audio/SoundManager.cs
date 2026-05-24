using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Central manager for playing all sounds in the game. Make it persistant at starting scene
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Tooltip("How many AudioSources to pre-create in the pool. Useful for limiting the number of sounds that can play simultaneously.")]
    [SerializeField] int poolSize = 10;

    List<AudioSource> pool = new List<AudioSource>();

    HashSet<SoundData> playingSounds = new HashSet<SoundData>(); // Tracks which SoundData assets are currently playing to support preventDuplicates

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

    // Play a sound at 2D or at a world position for 3D sounds.
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

        // If not looping, release the source back to the pool after the clip ends
        if (!data.loop)
            StartCoroutine(ReleaseAfter(data, source, clip.length));
        else
            playingSounds.Add(data);
    }

    // Stop a currently looping sound.
    public void Stop(SoundData data)
    {
        foreach (var source in pool)
        {
            if (source.isPlaying && playingSounds.Contains(data))
            {
                source.Stop();
                playingSounds.Remove(data);
                return;
            }
        }
    }

    AudioSource GetAvailableSource()
    {
        foreach (var source in pool)
            if (!source.isPlaying) return source;

        Debug.LogWarning("SoundManager: No available AudioSources in the pool!");
        return null;
    }

    // Release a non-looping sound after it finishes playing
    IEnumerator ReleaseAfter(SoundData data, AudioSource source, float duration)
    {
        playingSounds.Add(data);
        yield return new WaitForSeconds(duration);
        playingSounds.Remove(data);
        source.Stop();
    }
}