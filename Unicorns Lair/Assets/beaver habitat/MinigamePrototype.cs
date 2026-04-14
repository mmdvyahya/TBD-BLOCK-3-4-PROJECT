using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class MinigamePrototype : MonoBehaviour
{
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private int targetFeeds = 3;

    private int currentFeeds = 0;

    public void OpenMinigame()
    {
        Debug.Log("Minigame opened");
        currentFeeds = 0;
        RefreshUI();
    }

    public void PressFeed()
    {
        Debug.Log("Feed button pressed");

        currentFeeds++;
        RefreshUI();

        if (currentFeeds >= targetFeeds)
        {
            Debug.Log("Minigame complete");
            PrototypeGameManager.Instance.NotifyMinigameComplete();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            Debug.Log("F debug feed");
            PressFeed();
        }
    }

    private void RefreshUI()
    {
        if (progressText != null)
        {
            progressText.text = "Fed: " + currentFeeds + " / " + targetFeeds;
        }
        else
        {
            Debug.LogError("Progress Text is not assigned in MinigamePrototype");
        }
    }
}