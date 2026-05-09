using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class MicrophoneInput : MonoBehaviour
{
    [Header("Mic Settings")]
    [SerializeField] private float loudnessThreshold = 0.02f;
    [SerializeField] private int sampleWindow = 64;

    public bool WasBlowDetectedThisFrame { get; private set; }
    public float CurrentLoudness { get; private set; }

    private AudioClip _microphoneClip;
    private string _microphoneDevice;

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
#endif
        StartMic();
    }

    void StartMic()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[MicrophoneInput] No microphone detected.");
            return;
        }
        _microphoneDevice = Microphone.devices[0];
        _microphoneClip = Microphone.Start(_microphoneDevice, true, 10, 44100);
    }

    private void Update()
    {
        WasBlowDetectedThisFrame = false;
        if (_microphoneClip == null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone)) StartMic();
#endif
            return;
        }
        CurrentLoudness = GetLoudness();
        if (CurrentLoudness > loudnessThreshold) WasBlowDetectedThisFrame = true;
    }

    private float GetLoudness()
    {
        int micPosition = Microphone.GetPosition(_microphoneDevice) - sampleWindow;
        if (micPosition < 0) return 0f;
        float[] samples = new float[sampleWindow];
        _microphoneClip.GetData(samples, micPosition);
        float total = 0f;
        for (int i = 0; i < sampleWindow; i++) total += Mathf.Abs(samples[i]);
        return total / sampleWindow;
    }
}