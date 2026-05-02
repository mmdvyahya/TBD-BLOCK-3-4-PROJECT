using UnityEngine;
using UnityEngine.InputSystem;

public class ParrotTiltPourInput : MonoBehaviour
{
    [Header("Tilt Settings")]
    [SerializeField] private float pourTiltThreshold = 0.35f;

    [Header("Editor Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;
    [SerializeField] private Key debugPourKey = Key.Space;

    public bool IsPouring { get; private set; }

    private void Start()
    {
        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);
    }

    private void Update()
    {
        IsPouring = false;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (allowKeyboardDebug && Keyboard.current != null && Keyboard.current[debugPourKey].isPressed)
        {
            IsPouring = true;
            return;
        }
#else
        if (Accelerometer.current == null)
            return;

        Vector3 acceleration = Accelerometer.current.acceleration.ReadValue();

        // Forward/down tilt. If reversed on tablet, change < to >
        IsPouring = acceleration.y < -pourTiltThreshold;
#endif
    }
}