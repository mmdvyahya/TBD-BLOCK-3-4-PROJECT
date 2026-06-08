using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Audio;

public class GlobalUIClickSound : MonoBehaviour
{
    public static GlobalUIClickSound Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float volume = 0.7f;

    [Header("Mixer")]
    [SerializeField] private AudioMixerGroup mixerGroup;

    private AudioSource audioSource;
    private GameObject lastPressedObject;

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

    private void Update()
    {
        if (EventSystem.current == null)
            return;

        GameObject currentPressed = EventSystem.current.currentSelectedGameObject;

        if (currentPressed == null)
            return;

        if (currentPressed == lastPressedObject)
            return;

        PlayClickSound();
        lastPressedObject = currentPressed;
    }

    private void LateUpdate()
    {
        if (EventSystem.current == null)
            return;

        if (EventSystem.current.currentSelectedGameObject == null)
            lastPressedObject = null;
    }

    private void PlayClickSound()
    {
        if (clickSound == null)
            return;

        audioSource.PlayOneShot(clickSound, volume);
    }
}