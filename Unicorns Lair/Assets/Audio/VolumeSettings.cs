using UnityEngine;
using UnityEngine.Audio;

// A component that applies saved volume settings to the audio mixer. Make it persistant at starting scene
public class VolumeSettings : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;

    [Header("Mixer parameters")]
    [SerializeField] string masterParameter = "Master";   
    [SerializeField][Range(0,100)] float masterDefault = 100f;
    [SerializeField] string musicParameter = "Music";    
    [SerializeField][Range(0,100)] float musicDefault = 100f;
    [SerializeField] string sfxParameter = "Sfx";      
    [SerializeField][Range(0,100)] float sfxDefault = 100f;
    [SerializeField] string voiceParameter = "Voice";    
    [SerializeField][Range(0,100)] float voiceDefault = 100f;
    [SerializeField] string ambienceParameter = "Ambience"; 
    [SerializeField][Range(0,100)] float ambienceDefault = 100f;

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
        Apply(masterParameter, masterDefault);
        Apply(musicParameter, musicDefault);
        Apply(sfxParameter, sfxDefault);
        Apply(voiceParameter, voiceDefault);
        Apply(ambienceParameter, ambienceDefault);
    }

    void Apply(string parameter, float defaultValue)
    {
        float value = PlayerPrefs.GetFloat(parameter, defaultValue);
        float db = value > 0.001f ? Mathf.Log10(value / 100f) * 20f : -80f;
        mixer.SetFloat(parameter, db);
    }
}