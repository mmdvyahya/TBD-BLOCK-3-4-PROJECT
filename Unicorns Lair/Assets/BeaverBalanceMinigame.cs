using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class BeaverBalanceMinigame : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text debugTiltText;

    [Header("Stick")]
    [SerializeField] private RectTransform stickPivot;
    [SerializeField] private RectTransform stickVisual;
    [SerializeField] private Image stickImage;

    [Header("Stick Settings")]
    [SerializeField] private float maxStickAngle = 35f;
    [SerializeField] private float stickMoveSpeed = 120f;
    [SerializeField] private float startingAngle = 12f;

    [Header("Balance Rules")]
    [SerializeField] private float balanceZoneAngle = 6f;
    [SerializeField] private float stableTimeRequired = 5f;

    [Header("Desktop Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;
    [SerializeField] private float keyboardTiltSpeed = 1.5f;

    [Header("Tablet Input")]
    [SerializeField] private bool useAccelerometer = true;
    [SerializeField] private float accelerometerMultiplier = 35f;
    [SerializeField] private float accelerometerDeadZone = 0.05f;

    private float currentStickAngle;
    private float stableTimeLeft;
    private bool isRunning;

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

    public void OpenMinigame()
    {
        Debug.Log("=== BeaverBalance OpenMinigame ===");

        if (panel == null)
        {
            Debug.LogError("BeaverBalance: panel is NULL");
            return;
        }

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();

        if (stickPivot != null)
            stickPivot.gameObject.SetActive(true);

        if (stickVisual != null)
            stickVisual.gameObject.SetActive(true);

        if (titleText != null)
            titleText.text = "Beaver Balance";

#if UNITY_ANDROID && !UNITY_EDITOR
        if (instructionText != null)
            instructionText.text = "Tilt the tablet left and right to balance the stick";
#else
        if (instructionText != null)
            instructionText.text = "Press A / D to simulate tablet tilt";
#endif

        stableTimeLeft = stableTimeRequired;
        currentStickAngle = startingAngle;
        isRunning = true;

        ForceStickVisible();
        UpdateStickVisual();
        UpdateUI();

        Debug.Log("Panel activeSelf: " + panel.activeSelf);
        Debug.Log("Panel activeInHierarchy: " + panel.activeInHierarchy);

        if (stickPivot != null)
            Debug.Log("StickPivot activeInHierarchy: " + stickPivot.gameObject.activeInHierarchy);
        else
            Debug.LogError("StickPivot is NULL");

        if (stickVisual != null)
            Debug.Log("StickVisual activeInHierarchy: " + stickVisual.gameObject.activeInHierarchy);
        else
            Debug.LogError("StickVisual is NULL");

        Debug.Log("=== BeaverBalance opened successfully ===");
    }

    private void ForceStickVisible()
    {
        if (stickPivot != null)
        {
            stickPivot.anchorMin = new Vector2(0.5f, 0.5f);
            stickPivot.anchorMax = new Vector2(0.5f, 0.5f);
            stickPivot.pivot = new Vector2(0.5f, 0.5f);
            stickPivot.anchoredPosition = Vector2.zero;
            stickPivot.sizeDelta = new Vector2(20f, 20f);
            stickPivot.localScale = Vector3.one;
        }

        if (stickVisual != null)
        {
            stickVisual.anchorMin = new Vector2(0.5f, 0.5f);
            stickVisual.anchorMax = new Vector2(0.5f, 0.5f);
            stickVisual.pivot = new Vector2(0.5f, 0.5f);
            stickVisual.anchoredPosition = Vector2.zero;
            stickVisual.sizeDelta = new Vector2(300f, 20f);
            stickVisual.localScale = Vector3.one;
            stickVisual.localRotation = Quaternion.identity;
        }

        if (stickImage != null)
        {
            stickImage.color = new Color(0.45f, 0.27f, 0.1f, 1f);
            stickImage.raycastTarget = false;
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        float inputTilt = 0f;

        if (allowKeyboardDebug && Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed)
                inputTilt -= keyboardTiltSpeed;

            if (Keyboard.current.dKey.isPressed)
                inputTilt += keyboardTiltSpeed;
        }

        if (useAccelerometer && Accelerometer.current != null)
        {
            Vector3 accel = Accelerometer.current.acceleration.ReadValue();
            float accelX = accel.x;

            if (Mathf.Abs(accelX) > accelerometerDeadZone)
            {
                inputTilt += accelX * accelerometerMultiplier * Time.deltaTime;
            }

            if (debugTiltText != null)
                debugTiltText.text = "Tilt X: " + accelX.ToString("F2");
        }

        // лёгкая нестабильность
        currentStickAngle += 10f * Time.deltaTime;

        // управление
        currentStickAngle += inputTilt * stickMoveSpeed * Time.deltaTime;

        currentStickAngle = Mathf.Clamp(currentStickAngle, -maxStickAngle, maxStickAngle);

        UpdateStickVisual();
        UpdateBalanceState();
    }

    private void UpdateStickVisual()
    {
        if (stickPivot != null)
        {
            stickPivot.localRotation = Quaternion.Euler(0f, 0f, currentStickAngle);
        }
    }

    private void UpdateBalanceState()
    {
        bool isStable = Mathf.Abs(currentStickAngle) <= balanceZoneAngle;

        if (isStable)
        {
            stableTimeLeft -= Time.deltaTime;
        }

        stableTimeLeft = Mathf.Max(0f, stableTimeLeft);
        UpdateUI();

        if (stableTimeLeft <= 0f)
        {
            CompleteMinigame();
        }
    }

    private void UpdateUI()
    {
        if (countdownText == null) return;

        bool isStable = Mathf.Abs(currentStickAngle) <= balanceZoneAngle;

        if (isStable)
            countdownText.text = "Stable! " + stableTimeLeft.ToString("F1");
        else
            countdownText.text = "Hold steady: " + stableTimeLeft.ToString("F1");
    }

    private void CompleteMinigame()
    {
        isRunning = false;

        if (instructionText != null)
            instructionText.text = "Success! The beaver balanced the stick.";

        Debug.Log("Beaver Balance minigame complete");

        Invoke(nameof(FinishAndClose), 1.2f);
    }

    private void FinishAndClose()
    {
        if (panel != null)
            panel.SetActive(false);

        if (PrototypeGameManager.Instance != null)
            PrototypeGameManager.Instance.NotifyMinigameComplete();
    }
}