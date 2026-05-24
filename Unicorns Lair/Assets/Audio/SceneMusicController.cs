using UnityEngine;

// Play music for a scene
public class SceneMusicController : MonoBehaviour
{
    [SerializeField] SoundData soundData;
    [SerializeField] float fadeDuration = 1.5f;

    void Start()
    {
        if (soundData != null)
            MusicManager.Instance.Play(soundData, fadeDuration);
        else
            MusicManager.Instance.Stop(fadeDuration);
    }
}