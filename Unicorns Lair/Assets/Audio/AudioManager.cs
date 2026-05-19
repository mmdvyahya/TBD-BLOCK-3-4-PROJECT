using UnityEngine;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;

    [Header("Master")]
    [SerializeField] Slider masterSlider;
    [SerializeField] TMP_Text masterVolumeText;
    static float masterValue = 100;

    [Header("Music")]
    [SerializeField] Slider musicSlider;
    [SerializeField] TMP_Text musicVolumeTXT;
    static float musicValue = 100;

    [Header("Sfx")]
    [SerializeField] Slider sfxSlider;
    [SerializeField] TMP_Text sfxVolumeText;
    static float sfxValue = 100;

    [Header("Voice")]
    [SerializeField] Slider voiceSlider;
    [SerializeField] TMP_Text voiceVolumeText;
    static float voiceValue = 100;

    [Header("Ambient")]
    [SerializeField] Slider ambientSlider;
    [SerializeField] TMP_Text ambientVolumeText;
    static float ambientValue = 100;


    void Start()
    {
        // Used for UI slider
        masterSlider.onValueChanged.AddListener(MasterChangeVolume);
        musicSlider.onValueChanged.AddListener(MusicChangeVolume);
        sfxSlider.onValueChanged.AddListener(SfxChangeVolume);
        voiceSlider.onValueChanged.AddListener(VoiceChangeVolume);
        ambientSlider.onValueChanged.AddListener(AmbientChangeVolume);

        masterSlider.value = masterValue;
        musicSlider.value = musicValue;
        sfxSlider.value = sfxValue;
        voiceSlider.value = voiceValue;
        ambientSlider.value = ambientValue;

        // Used for UI text
        masterVolumeText.text = Mathf.RoundToInt(masterSlider.value).ToString();
        musicVolumeTXT.text = Mathf.RoundToInt(musicSlider.value).ToString();
        sfxVolumeText.text = Mathf.RoundToInt(sfxSlider.value).ToString();
        voiceVolumeText.text = Mathf.RoundToInt(voiceSlider.value).ToString();
        ambientVolumeText.text = Mathf.RoundToInt(ambientSlider.value).ToString();
    }

    // Functions called when the slider value changes
    void MasterChangeVolume(float value)
    {
        masterValue = value;
        masterVolumeText.text = Mathf.RoundToInt(value).ToString();
        mixer.SetFloat("MasterVolume", Mathf.Log10(masterValue / 100) * 20); // Changes lineair volume to logarathimic
    }
    void MusicChangeVolume(float value)
    {
        musicValue = value;
        musicVolumeTXT.text = Mathf.RoundToInt(value).ToString();
        mixer.SetFloat("MusicVolume", Mathf.Log10(musicValue / 100) * 20); 
    }

    void SfxChangeVolume(float value)
    {
        sfxValue = value;
        sfxVolumeText.text = Mathf.RoundToInt(value).ToString();
        mixer.SetFloat("SfxVolume", Mathf.Log10(sfxValue / 100) * 20);
    }

    void VoiceChangeVolume(float value)
    {
        voiceValue = value;
        voiceVolumeText.text = Mathf.RoundToInt(value).ToString();
        mixer.SetFloat("VoiceVolume", Mathf.Log10(voiceValue / 100) * 20);
    }

    void AmbientChangeVolume(float value)
    {
        ambientValue = value;
        ambientVolumeText.text = Mathf.RoundToInt(value).ToString();
        mixer.SetFloat("AmbientVolume", Mathf.Log10(ambientValue / 100) * 20);
    }
}