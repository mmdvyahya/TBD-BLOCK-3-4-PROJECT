using UnityEngine;
using UnityEngine.Audio;

// A component that applies saved volume settings to the audio mixer. Make it persistant at starting scene
public class VolumeSettings : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;

    [Header("Mixer parameters")]
    [SerializeField] string masterParameter = "Master";
    [SerializeField] string musicParameter = "Music";
    [SerializeField] string sfxParameter = "Sfx";
    [SerializeField] string voiceParameter = "Voice";
    [SerializeField] string ambienceParameter = "Ambience";

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ApplyAll();
    }

    void ApplyAll()
    {
        Apply(masterParameter);
        Apply(musicParameter);
        Apply(sfxParameter);
        Apply(voiceParameter);
        Apply(ambienceParameter);
    }

    void Apply(string parameter)
    {
        float value = PlayerPrefs.GetFloat(parameter, 100f);
        float db = value > 0.001f ? Mathf.Log10(value / 100f) * 20f : -80f;
        mixer.SetFloat(parameter, db);
    }
}