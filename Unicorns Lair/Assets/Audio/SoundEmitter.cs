using UnityEngine;

// Attach to any GameObject to trigger sounds from the inspector or UnityEvents.
public class SoundEmitter : MonoBehaviour
{
    [SerializeField] SoundData soundData;
    [SerializeField] bool playOnStart = false;
    [SerializeField] bool use3DPosition = false;

    void Start()
    {
        if (playOnStart) Play();
    }

    void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        if (soundData == null || SoundManager.Instance == null) return; // Prevents errors in case SoundManager is missing from the scene

        if (use3DPosition)
            SoundManager.Instance.Play(soundData, transform.position);
        else
            SoundManager.Instance.Play(soundData);
    }

    public void Stop()
    {
        if (soundData == null || SoundManager.Instance == null) return; // Prevents errors in case SoundManager is missing from the scene
        SoundManager.Instance.Stop(soundData);
    }
    void OnDrawGizmosSelected()
    {
        if (soundData == null || soundData.spatialBlend == 0f) return;

        Color gizmoColorMin = new Color(0, 1, 0, 0.3f);
        Color gizmoColorMax = new Color(1, 0, 0, 0.3f);

        Gizmos.color = gizmoColorMin;
        Gizmos.DrawSphere(transform.position, soundData.minDistance);
        Gizmos.DrawWireSphere(transform.position, soundData.minDistance);

        Gizmos.color = gizmoColorMax;
        Gizmos.DrawSphere(transform.position, soundData.maxDistance);
        Gizmos.DrawWireSphere(transform.position, soundData.maxDistance);
    }
}