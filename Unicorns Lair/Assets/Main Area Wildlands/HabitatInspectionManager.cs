using UnityEngine;
using UnityEngine.InputSystem;

public class HabitatInspectionManager : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraDistance = 600f;
    [SerializeField] private float cameraHeight = 200f;
    [SerializeField] private float cameraMoveSpeed = 4f;
    [SerializeField] private float lookAtHeight = 100f;

    [Header("Rotation Settings")]
    [SerializeField] private float horizontalRotationAmount = 45f;
    [SerializeField] private float verticalRotationAmount = 18f;
    [SerializeField] private float rotationSmoothness = 5f;

    private InspectableHabitat _currentHabitat;
    private CameraSwipe _cameraSwipe;
    private bool _isInspecting;
    private float _currentHorizontalAngle;
    private float _currentVerticalAngle;

    private void Start()
    {
        LanguageManager.Ensure();

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) _cameraSwipe = mainCamera.GetComponent<CameraSwipe>();

        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);
    }

    private void Update()
    {
        if (!_isInspecting || _currentHabitat == null || mainCamera == null) return;
        UpdateCameraInspection();
    }

    public void OpenMinigame(InspectableHabitat habitat)
    {
        if (habitat == null || mainCamera == null) return;

        _currentHabitat = habitat;
        _isInspecting = true;
        _currentHorizontalAngle = 0f;
        _currentVerticalAngle = 0f;
    }

    public void CloseInspectionMode()
    {
        _isInspecting = false;
        _currentHabitat = null;
    }

    private void UpdateCameraInspection()
    {
        Vector2 input = GetInspectionInput();

        _currentHorizontalAngle = Mathf.Lerp(_currentHorizontalAngle, input.x * horizontalRotationAmount, Time.deltaTime * rotationSmoothness);
        _currentVerticalAngle = Mathf.Lerp(_currentVerticalAngle, -input.y * verticalRotationAmount, Time.deltaTime * rotationSmoothness);

        Vector3 targetCenter = _currentHabitat.GetInspectionCenter() + Vector3.up * lookAtHeight;
        Quaternion orbitRotation = Quaternion.Euler(_currentVerticalAngle, _currentHorizontalAngle, 0f);
        Vector3 offset = orbitRotation * new Vector3(0f, cameraHeight, -cameraDistance);
        Vector3 desiredPosition = targetCenter + offset;

        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desiredPosition, Time.deltaTime * cameraMoveSpeed);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation,
            Quaternion.LookRotation(targetCenter - mainCamera.transform.position), Time.deltaTime * cameraMoveSpeed);
    }

    private Vector2 GetInspectionInput()
    {
        Vector2 input = Vector2.zero;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        }
#else
        if (Accelerometer.current != null)
        {
            Vector3 acc = Accelerometer.current.acceleration.ReadValue();
            input.x = acc.x;
            input.y = acc.y;
        }
#endif

        return new Vector2(Mathf.Clamp(input.x, -1f, 1f), Mathf.Clamp(input.y, -1f, 1f));
    }
}