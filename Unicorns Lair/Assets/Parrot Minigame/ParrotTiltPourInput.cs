using UnityEngine;
using UnityEngine.InputSystem;

public class ParrotTiltPourInput : MonoBehaviour
{
    private enum PourDirection
    {
        ForwardBack,
        LeftRight
    }

    [Header("Tilt Settings")]
    [SerializeField] private PourDirection pourDirection = PourDirection.LeftRight;
    [SerializeField] private float pourTiltThreshold = 0.25f;
    [SerializeField] private bool requirePositiveDirection = true;

    [Header("Calibration")]
    [SerializeField] private bool calibrateOnStart = true;
    [SerializeField] private Key recalibrateKey = Key.C;

    [Header("Editor Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;
    [SerializeField] private Key debugPourKey = Key.Space;

    public bool IsPouring { get; private set; }

    private Vector3 calibrationAcceleration;
    private bool calibrated;

    private void Start()
    {
        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);

        if (calibrateOnStart)
            Calibrate();
    }

    private void Update()
    {
        IsPouring = false;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Keyboard.current != null && Keyboard.current[recalibrateKey].wasPressedThisFrame)
            Calibrate();

        if (allowKeyboardDebug && Keyboard.current != null && Keyboard.current[debugPourKey].isPressed)
        {
            IsPouring = true;
            return;
        }
#else
        if (Accelerometer.current == null)
            return;

        if (!calibrated)
            Calibrate();

        Vector3 acceleration = Accelerometer.current.acceleration.ReadValue();
        Vector3 delta = acceleration - calibrationAcceleration;

        float tiltValue = pourDirection == PourDirection.LeftRight
            ? delta.x
            : delta.y;

        if (requirePositiveDirection)
            IsPouring = tiltValue > pourTiltThreshold;
        else
            IsPouring = tiltValue < -pourTiltThreshold;
#endif
    }

    public void Calibrate()
    {
        if (Accelerometer.current == null)
        {
            calibrated = false;
            return;
        }

        calibrationAcceleration = Accelerometer.current.acceleration.ReadValue();
        calibrated = true;

        Debug.Log("[ParrotTiltPourInput] Calibrated: " + calibrationAcceleration);
    }
}