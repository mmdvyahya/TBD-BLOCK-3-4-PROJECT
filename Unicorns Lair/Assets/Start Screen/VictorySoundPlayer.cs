using UnityEngine;

public class VictorySoundPlayer : MonoBehaviour
{
    public static VictorySoundPlayer Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private float volume = 0.8f;

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
    }

    public void PlayVictorySound()
    {
        if (victorySound == null)
            return;

        audioSource.PlayOneShot(victorySound, volume);
    }
}