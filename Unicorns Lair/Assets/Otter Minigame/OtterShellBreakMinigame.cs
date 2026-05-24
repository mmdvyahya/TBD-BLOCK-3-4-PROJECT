using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class OtterShellBreakMinigame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text progressText;

    [Header("Scene Objects")]
    [SerializeField] private GameObject otterModel;

    [Tooltip("0 = closed, 1 = small crack, 2 = medium crack, 3 = big crack, 4 = broken")]
    [SerializeField] private GameObject[] shellStages;

    [Header("Shake Settings")]
    [SerializeField] private int shakesNeeded = 5;
    [SerializeField] private float forwardShakeThreshold = 1.25f;
    [SerializeField] private float shakeCooldown = 0.35f;
    [SerializeField] private ShakeAxis forwardAxis = ShakeAxis.Z;
    [SerializeField] private bool invertAxis = false;

    [Header("Animation")]
    [SerializeField] private Animator otterAnimator;
    [SerializeField] private string anticipationTrigger = "Anticipate";
    [SerializeField] private string happyTrigger = "Happy";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip crackSound;
    [SerializeField] private AudioClip completeSound;

    [Header("PC Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;
    [SerializeField] private Key debugShakeKey = Key.Space;

    private int currentShakes;
    private float lastShakeTime = -999f;
    private bool isRunning;
    private bool isCompleted;

    private Vector3 lastAcceleration;
    private bool hasLastAcceleration;

    public enum ShakeAxis
    {
        X,
        Y,
        Z
    }

    private void Start()
    {
        StartMinigame();
    }

    private void OnEnable()
    {
        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);
    }

    private void OnDisable()
    {
        if (Accelerometer.current != null)
            InputSystem.DisableDevice(Accelerometer.current);
    }

    private void Update()
    {
        if (!isRunning || isCompleted)
            return;

        DetectForwardShake();
        DetectDebugInput();
    }

    private void StartMinigame()
    {
        currentShakes = 0;
        lastShakeTime = -999f;
        isRunning = true;
        isCompleted = false;
        hasLastAcceleration = false;

        if (otterModel != null)
            otterModel.SetActive(true);

        ShowShellStage(0);

        if (titleText != null)
            titleText.text = "Otter Shell Break";

        if (hintText != null)
            hintText.text = "Shake Forward";

        UpdateProgressText();
    }

    private void DetectForwardShake()
    {
        if (Accelerometer.current == null)
            return;

        Vector3 acceleration = Accelerometer.current.acceleration.ReadValue();

        if (!hasLastAcceleration)
        {
            lastAcceleration = acceleration;
            hasLastAcceleration = true;
            return;
        }

        Vector3 delta = acceleration - lastAcceleration;
        lastAcceleration = acceleration;

        float forwardValue = GetAxisValue(delta);

        if (invertAxis)
            forwardValue *= -1f;

        if (forwardValue >= forwardShakeThreshold && Time.time >= lastShakeTime + shakeCooldown)
        {
            RegisterShake();
        }
    }

    private void DetectDebugInput()
    {
        if (!allowKeyboardDebug)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current[debugShakeKey].wasPressedThisFrame)
        {
            RegisterShake();
        }
    }

    private void RegisterShake()
    {
        if (isCompleted)
            return;

        lastShakeTime = Time.time;
        currentShakes++;

        PlaySound(crackSound);
        PlayOtterAnticipation();
        UpdateShellVisual();
        UpdateProgressText();

        if (currentShakes >= shakesNeeded)
        {
            CompleteMinigame();
        }
    }

    private void UpdateShellVisual()
    {
        if (shellStages == null || shellStages.Length == 0)
            return;

        float progress = Mathf.Clamp01((float)currentShakes / shakesNeeded);

        int lastStageIndex = shellStages.Length - 1;
        int stageIndex = Mathf.FloorToInt(progress * lastStageIndex);

        stageIndex = Mathf.Clamp(stageIndex, 0, lastStageIndex);

        ShowShellStage(stageIndex);
    }

    private void ShowShellStage(int indexToShow)
    {
        if (shellStages == null)
            return;

        for (int i = 0; i < shellStages.Length; i++)
        {
            if (shellStages[i] != null)
                shellStages[i].SetActive(i == indexToShow);
        }
    }

    private void CompleteMinigame()
    {
        isCompleted = true;
        isRunning = false;

        ShowShellStage(shellStages.Length - 1);

        if (hintText != null)
            hintText.text = "Shell opened!";

        PlaySound(completeSound);
        PlayOtterHappy();

        StartCoroutine(FinishRoutine());
    }

    private IEnumerator FinishRoutine()
    {
        yield return new WaitForSeconds(2f);

        Debug.Log("Otter Shell Break finished. Scene can now return to the main zoo flow.");
    }

    private void UpdateProgressText()
    {
        if (progressText != null)
            progressText.text = currentShakes + " / " + shakesNeeded;
    }

    private float GetAxisValue(Vector3 value)
    {
        switch (forwardAxis)
        {
            case ShakeAxis.X:
                return value.x;
            case ShakeAxis.Y:
                return value.y;
            case ShakeAxis.Z:
                return value.z;
            default:
                return value.z;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void PlayOtterAnticipation()
    {
        if (otterAnimator != null && !string.IsNullOrEmpty(anticipationTrigger))
            otterAnimator.SetTrigger(anticipationTrigger);
    }

    private void PlayOtterHappy()
    {
        if (otterAnimator != null && !string.IsNullOrEmpty(happyTrigger))
            otterAnimator.SetTrigger(happyTrigger);
    }
}