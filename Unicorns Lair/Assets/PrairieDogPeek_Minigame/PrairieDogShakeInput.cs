using UnityEngine;
using UnityEngine.InputSystem;

public class PrairieDogShakeInput : MonoBehaviour
{
    [Header("Shake Detection")]
    [SerializeField] private float shakeThreshold = 2.2f;
    [SerializeField] private float shakeCooldown = 0.6f;

    [Header("Editor Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;
    [SerializeField] private Key debugShakeKey = Key.Space;

    private float lastShakeTime = -999f;
    private Vector3 lastAcceleration;

    public bool WasShakeDetectedThisFrame { get; private set; }

    private void Start()
    {
        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);
    }

    private void Update()
    {
        WasShakeDetectedThisFrame = false;

        if (Time.unscaledTime - lastShakeTime < shakeCooldown)
            return;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (allowKeyboardDebug && Keyboard.current != null && Keyboard.current[debugShakeKey].wasPressedThisFrame)
        {
            RegisterShake();
            return;
        }
#else
        if (Accelerometer.current == null)
            return;

        Vector3 acceleration = Accelerometer.current.acceleration.ReadValue();
        float delta = (acceleration - lastAcceleration).magnitude;
        lastAcceleration = acceleration;

        if (delta >= shakeThreshold)
        {
            RegisterShake();
        }
#endif
    }

    private void RegisterShake()
    {
        lastShakeTime = Time.unscaledTime;
        WasShakeDetectedThisFrame = true;
    }
}