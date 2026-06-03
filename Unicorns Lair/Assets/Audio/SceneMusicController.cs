using UnityEngine;

// Play music for a scene
public class SceneMusicController : MonoBehaviour
{
    [SerializeField] SoundData soundData;
    [SerializeField] float fadeDuration = 1.5f;

    void Start()
    {
        if (MusicManager.Instance == null) return; // Prevents errors in case MusicManager is missing from the scene

        if (soundData != null)
            MusicManager.Instance.Play(soundData, fadeDuration);
        else
            MusicManager.Instance.Stop(fadeDuration);
    }
}