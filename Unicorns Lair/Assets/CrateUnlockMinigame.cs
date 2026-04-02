using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class CrateUnlockMinigame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text hintText;

    [Header("Crate Visuals")]
    [SerializeField] private Transform crateVisual;
    [SerializeField] private GameObject closedCrate;
    [SerializeField] private GameObject crackedCrate;
    [SerializeField] private GameObject animalReveal;

    [Header("Progress Settings")]
    [SerializeField] private int shakesNeeded = 5;

    [Header("PC Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;

    [Header("Android Shake Detection")]
    [SerializeField] private bool useAndroidAccelerometer = true;
    [SerializeField] private float shakeThreshold = 2.2f;
    [SerializeField] private float shakeCooldown = 0.35f;

    private int currentShakes;
    private bool isRunning;
    private float lastShakeTime = -999f;

    private void OnEnable()
    {
        if (Accelerometer.current != null && !Accelerometer.current.enabled)
        {
            InputSystem.EnableDevice(Accelerometer.current);
        }
    }

    private void Start()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        if (!isRunning) return;

        if (allowKeyboardDebug && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RegisterShake("Keyboard R");
        }

        if (useAndroidAccelerometer && Accelerometer.current != null)
        {
            Vector3 accel = Accelerometer.current.acceleration.ReadValue();
            float magnitude = accel.magnitude;

            if (magnitude > shakeThreshold && Time.unscaledTime - lastShakeTime > shakeCooldown)
            {
                lastShakeTime = Time.unscaledTime;
                RegisterShake("Accelerometer");
            }
        }
    }

    public void OpenCrateSequence()
    {
        currentShakes = 0;
        isRunning = true;
        lastShakeTime = -999f;

        if (panel != null)
            panel.SetActive(true);

        if (titleText != null)
            titleText.text = "Open the animal crate!";

        if (hintText != null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            hintText.text = "Shake the tablet!";
#else
            hintText.text = "Press R to simulate shaking";
#endif
        }

        if (closedCrate != null)
            closedCrate.SetActive(true);

        if (crackedCrate != null)
            crackedCrate.SetActive(false);

        if (animalReveal != null)
            animalReveal.SetActive(false);

        if (crateVisual != null)
            crateVisual.localRotation = Quaternion.identity;

        RefreshUI();
        Debug.Log("Crate sequence started");
    }

    public void RegisterShake(string source)
    {
        if (!isRunning) return;

        currentShakes++;
        Debug.Log($"Shake registered from {source}. Count: {currentShakes}");

        if (crateVisual != null)
        {
            crateVisual.localRotation = Quaternion.Euler(
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f)
            );
        }

        if (currentShakes >= 3 && crackedCrate != null)
        {
            crackedCrate.SetActive(true);

            if (closedCrate != null)
                closedCrate.SetActive(false);
        }

        RefreshUI();

        if (currentShakes >= shakesNeeded)
        {
            CompleteSequence();
        }
    }

    private void RefreshUI()
    {
        if (progressText != null)
            progressText.text = "Shakes: " + currentShakes + " / " + shakesNeeded;
    }

    private void CompleteSequence()
    {
        isRunning = false;

        if (crateVisual != null)
            crateVisual.localRotation = Quaternion.identity;

        if (closedCrate != null)
            closedCrate.SetActive(false);

        if (crackedCrate != null)
            crackedCrate.SetActive(false);

        if (animalReveal != null)
            animalReveal.SetActive(true);

#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif

        Invoke(nameof(FinishSequence), 1.2f);
    }

    private void FinishSequence()
    {
        if (panel != null)
            panel.SetActive(false);

        if (PrototypeGameManager.Instance != null)
            PrototypeGameManager.Instance.NotifyCrateUnlockComplete();
    }
}