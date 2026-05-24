using UnityEngine;

public class Example : MonoBehaviour
{
    [SerializeField] SoundData soundData;
    void Start()
    {
        SoundManager.Instance.Play(soundData);
    }
}
