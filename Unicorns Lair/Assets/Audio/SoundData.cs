using UnityEngine;
using UnityEngine.Audio;

// A ScriptableObject that holds all data for an audio event
[CreateAssetMenu(fileName = "NewSoundData", menuName = "Audio/Sound Data")]
public class SoundData : ScriptableObject
{
    public enum ClipPlayMode { Random, Sequential, RandomNoRepeat }

    [Header("Clips")]
    [Tooltip("Add multiple clips for random variation on each play.")]
    public AudioClip[] clips;
    public ClipPlayMode playMode = ClipPlayMode.Random;

    int index = 0;
    int lastIndex = -1;

    // Reset the sequence so it starts fresh each time the asset is enabled
    void OnEnable()
    {
        index = 0;
        lastIndex = -1;
    }

    [Header("Mixer routing")]
    [Tooltip("Which AudioMixerGroup this sound routes through")]
    public AudioMixerGroup mixerGroup;

    [Header("Volume")]
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Random volume variation on each play.")]
    [Range(0f, 0.5f)] public float volumeVariance = 0f;

    [Header("Pitch")]
    [Range(-3f, 3f)] public float pitch = 1f;
    [Tooltip("Random pitch variation on each play.")]
    [Range(0f, 1f)] public float pitchVariance = 0f;

    [Header("Behaviour")]
    public bool loop = false;
    [Tooltip("If true, a new play call won't restart the sound if it's already playing. Using WAV files is recommended for this feature.")]
    public bool preventDuplicates = false;

    [Header("Spatial")]
    [Tooltip("0 = 2D, 1 = 3D. A value between 0 and 1 will blend between the two.")]
    [Range(0f, 1f)] public float spatialBlend = 0f;

    [Header("Spatial 3D settings")]
    [Range(1f, 500f)] public float minDistance = 1f;
    [Range(1f, 500f)] public float maxDistance = 50f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    // Returns a random clip from the array (or null if empty)
    public AudioClip GetClip()
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];

        switch (playMode)
        {
            case ClipPlayMode.Sequential: // Plays clips in order
                var clip = clips[index];
                index = (index + 1) % clips.Length;
                return clip;

            case ClipPlayMode.RandomNoRepeat: // Plays clips randomly but won't repeat the same clip twice in a row
                int next;
                do { next = Random.Range(0, clips.Length); }
                while (next == lastIndex && clips.Length > 1);
                lastIndex = next;
                return clips[next];

            default: // Random
                return clips[Random.Range(0, clips.Length)];
        }
    }

    // Returns volume with optional random variance applied
    public float GetVolume() => Mathf.Clamp01(volume + Random.Range(-volumeVariance, volumeVariance));

    // Returns pitch with optional random variance applied
    public float GetPitch() => pitch + Random.Range(-pitchVariance, pitchVariance);
}