using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

// Controls slider for adjusting volume of a specific mixer group
public class VolumeSlider : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    [SerializeField] string mixerParameter; // Must match the exposed parameter name in the AudioMixer
    [SerializeField] TMP_Text label;
 
    Slider slider;
 
    void Awake()
    {
        slider = GetComponent<Slider>();
    }
 
    void Start()
    {
        float saved = PlayerPrefs.GetFloat(mixerParameter, 100f);
        slider.value = saved;
 
        UpdateMixer(saved);
        UpdateLabel(saved);
 
        slider.onValueChanged.AddListener(OnValueChanged);
    }
 
    void OnValueChanged(float value) // Called when the slider value changes
    {
        PlayerPrefs.SetFloat(mixerParameter, value);
        UpdateMixer(value);
        UpdateLabel(value);
    }
 
    void UpdateMixer(float value)
    {
        float db = value > 0.001f ? Mathf.Log10(value / 100f) * 20f : -80f; // Changes linear volume to logarathimic
        mixer.SetFloat(mixerParameter, db);
    }
 
    void UpdateLabel(float value)
    {
        if (label != null) label.text = Mathf.RoundToInt(value).ToString();
    }
}