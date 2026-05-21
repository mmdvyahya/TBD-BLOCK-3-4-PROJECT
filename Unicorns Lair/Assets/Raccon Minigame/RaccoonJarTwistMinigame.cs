using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class RaccoonJarTwistMinigame : MonoBehaviour
{
    [Header("Main Objects")]
    [SerializeField] private Transform jarLid;
    [SerializeField] private GameObject treatsObject;
    [SerializeField] private Transform raccoonObject;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject successTextObject;

    [Header("Game Settings")]
    [SerializeField] private int twistsNeeded = 3;
    [SerializeField] private float finishDelay = 1.8f;
    [SerializeField] private string returnSceneName = "WILDLANDS World";

    [Header("Rotation Detection")]
    [SerializeField] private bool useGyroscope = true;
    [SerializeField] private float twistThreshold = 1.35f;
    [SerializeField] private float twistCooldown = 0.35f;

    [Header("PC Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;
    [SerializeField] private Key leftTwistKey = Key.A;
    [SerializeField] private Key rightTwistKey = Key.D;

    [Header("Visual Feedback")]
    [SerializeField] private float lidRotationPerTwist = 35f;
    [SerializeField] private float lidPopHeight = 0.75f;
    [SerializeField] private float animationSpeed = 8f;

    private int currentTwists;
    private bool leftDetected;
    private bool rightDetected;
    private bool isFinished;
    private float lastTwistTime = -999f;

    private Quaternion lidStartRotation;
    private Vector3 lidStartPosition;
    private Vector3 raccoonStartScale;

    private void Awake()
    {
        if (jarLid != null)
        {
            lidStartRotation = jarLid.localRotation;
            lidStartPosition = jarLid.localPosition;
        }

        if (raccoonObject != null)
        {
            raccoonStartScale = raccoonObject.localScale;
        }

        if (treatsObject != null)
        {
            treatsObject.SetActive(false);
        }

        if (successTextObject != null)
        {
            successTextObject.SetActive(false);
        }

        UpdateProgressUI();
    }

    private void OnEnable()
    {
        EnableSensors();
    }

    private void OnDisable()
    {
        DisableSensors();
    }

    private void Update()
    {
        if (isFinished)
            return;

        DetectRotationInput();
        DetectKeyboardDebugInput();
    }

    private void EnableSensors()
    {
        if (!useGyroscope)
            return;

        if (UnityEngine.InputSystem.Gyroscope.current != null)
        {
            InputSystem.EnableDevice(UnityEngine.InputSystem.Gyroscope.current);
        }

        if (AttitudeSensor.current != null)
        {
            InputSystem.EnableDevice(AttitudeSensor.current);
        }
    }

    private void DisableSensors()
    {
        if (UnityEngine.InputSystem.Gyroscope.current != null)
        {
            InputSystem.DisableDevice(UnityEngine.InputSystem.Gyroscope.current);
        }

        if (AttitudeSensor.current != null)
        {
            InputSystem.DisableDevice(AttitudeSensor.current);
        }
    }

    private void DetectRotationInput()
    {
        if (!useGyroscope)
            return;

        if (UnityEngine.InputSystem.Gyroscope.current == null)
            return;

        Vector3 angularVelocity = UnityEngine.InputSystem.Gyroscope.current.angularVelocity.ReadValue();

        float zRotation = angularVelocity.z;

        if (Time.time - lastTwistTime < twistCooldown)
            return;

        if (zRotation > twistThreshold)
        {
            rightDetected = true;
        }

        if (zRotation < -twistThreshold)
        {
            leftDetected = true;
        }

        if (leftDetected && rightDetected)
        {
            RegisterSuccessfulTwist();
        }
    }

    private void DetectKeyboardDebugInput()
    {
        if (!allowKeyboardDebug)
            return;

        if (Keyboard.current == null)
            return;

        if (Time.time - lastTwistTime < twistCooldown)
            return;

        if (Keyboard.current[leftTwistKey].wasPressedThisFrame)
        {
            leftDetected = true;
        }

        if (Keyboard.current[rightTwistKey].wasPressedThisFrame)
        {
            rightDetected = true;
        }

        if (leftDetected && rightDetected)
        {
            RegisterSuccessfulTwist();
        }
    }

    private void RegisterSuccessfulTwist()
    {
        lastTwistTime = Time.time;

        leftDetected = false;
        rightDetected = false;

        currentTwists++;
        currentTwists = Mathf.Clamp(currentTwists, 0, twistsNeeded);

        UpdateProgressUI();

        StopAllCoroutines();
        StartCoroutine(AnimateTwistFeedback());

        if (currentTwists >= twistsNeeded)
        {
            StartCoroutine(FinishMinigame());
        }
    }

    private void UpdateProgressUI()
    {
        if (progressText == null)
            return;

        string progress = "";

        for (int i = 0; i < twistsNeeded; i++)
        {
            progress += i < currentTwists ? "●" : "○";

            if (i < twistsNeeded - 1)
                progress += " ";
        }

        progressText.text = progress;
    }

    private IEnumerator AnimateTwistFeedback()
    {
        if (jarLid == null)
            yield break;

        Quaternion startRot = jarLid.localRotation;
        Quaternion targetRot = lidStartRotation * Quaternion.Euler(0f, currentTwists * lidRotationPerTwist, 0f);

        Vector3 startScale = jarLid.localScale;
        Vector3 biggerScale = startScale * 1.08f;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed;

            jarLid.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            jarLid.localScale = Vector3.Lerp(startScale, biggerScale, Mathf.Sin(t * Mathf.PI));

            yield return null;
        }

        jarLid.localRotation = targetRot;
        jarLid.localScale = startScale;

        if (raccoonObject != null)
        {
            StartCoroutine(AnimateRaccoonBounce());
        }
    }

    private IEnumerator AnimateRaccoonBounce()
    {
        Vector3 startScale = raccoonStartScale;
        Vector3 bounceScale = raccoonStartScale * 1.12f;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 10f;
            raccoonObject.localScale = Vector3.Lerp(startScale, bounceScale, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        raccoonObject.localScale = startScale;
    }

    private IEnumerator FinishMinigame()
    {
        isFinished = true;

        if (hintText != null)
        {
            hintText.text = "Great job!";
        }

        if (successTextObject != null)
        {
            successTextObject.SetActive(true);
        }

        if (treatsObject != null)
        {
            treatsObject.SetActive(true);
        }

        yield return StartCoroutine(AnimateJarOpen());

        yield return new WaitForSeconds(finishDelay);

        SceneManager.LoadScene(returnSceneName);
    }

    private IEnumerator AnimateJarOpen()
    {
        if (jarLid == null)
            yield break;

        Vector3 startPos = jarLid.localPosition;
        Vector3 targetPos = lidStartPosition + new Vector3(0f, lidPopHeight, 0.25f);

        Quaternion startRot = jarLid.localRotation;
        Quaternion targetRot = startRot * Quaternion.Euler(25f, 0f, 35f);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 2.5f;

            jarLid.localPosition = Vector3.Lerp(startPos, targetPos, t);
            jarLid.localRotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        jarLid.localPosition = targetPos;
        jarLid.localRotation = targetRot;
    }
}