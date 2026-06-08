using UnityEngine;
using UnityEngine.Audio;

public class VictorySoundPlayer : MonoBehaviour
{
    public static VictorySoundPlayer Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private float volume = 0.8f;

    [Header("Mixer")]
    [SerializeField] private AudioMixerGroup mixerGroup;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        audioSource.outputAudioMixerGroup = mixerGroup;
    }

    public void PlayVictorySound()
    {
        if (victorySound == null)
            return;

        audioSource.PlayOneShot(victorySound, volume);
    }
}