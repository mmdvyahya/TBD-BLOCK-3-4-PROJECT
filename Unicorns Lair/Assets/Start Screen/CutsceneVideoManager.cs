using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class CutsceneVideoManager : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Scene Loading")]
    [SerializeField] private string nextSceneName = "Main Area";

    [Header("Input")]
    [SerializeField] private bool allowSkip = true;

    private bool isLoadingNextScene;

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("CutsceneVideoManager: No VideoPlayer found.");
            LoadNextScene();
            return;
        }

        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Play();
    }

    private void Update()
    {
        if (!allowSkip || isLoadingNextScene)
            return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            LoadNextScene();
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            LoadNextScene();
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            LoadNextScene();
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (isLoadingNextScene)
            return;

        isLoadingNextScene = true;
        SceneManager.LoadScene(nextSceneName);
    }
}