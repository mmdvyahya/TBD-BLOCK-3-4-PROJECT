using UnityEngine;

public class MicrophoneInput : MonoBehaviour
{
    [Header("Mic Settings")]
    [SerializeField] private float loudnessThreshold = 0.02f;
    [SerializeField] private int sampleWindow = 64;

    public bool WasBlowDetectedThisFrame { get; private set; }

    private AudioClip microphoneClip;
    private string microphoneDevice;

    private void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            microphoneClip = Microphone.Start(microphoneDevice, true, 10, 44100);
        }
        else
        {
            Debug.LogWarning("No microphone detected.");
        }
    }

    private void Update()
    {
        WasBlowDetectedThisFrame = false;

        if (microphoneClip == null)
            return;

        float loudness = GetLoudness();

        if (loudness > loudnessThreshold)
        {
            WasBlowDetectedThisFrame = true;
        }
    }

    private float GetLoudness()
    {
        int micPosition = Microphone.GetPosition(microphoneDevice) - sampleWindow;

        if (micPosition < 0)
            return 0;

        float[] samples = new float[sampleWindow];
        microphoneClip.GetData(samples, micPosition);

        float total = 0;

        for (int i = 0; i < sampleWindow; i++)
        {
            total += Mathf.Abs(samples[i]);
        }

        return total / sampleWindow;
    }
}