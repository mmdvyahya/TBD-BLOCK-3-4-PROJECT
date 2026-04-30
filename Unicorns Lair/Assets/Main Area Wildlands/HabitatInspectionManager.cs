using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HabitatInspectionManager : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraDistance = 7f;
    [SerializeField] private float cameraHeight = 3f;
    [SerializeField] private float cameraMoveSpeed = 5f;
    [SerializeField] private float lookAtHeight = 1.2f;

    [Header("UI")]
    [SerializeField] private GameObject dimBackground;
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private Text animalNameText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text factText;
    [SerializeField] private Button closeButton;

    [Header("Rotation Settings")]
    [SerializeField] private float horizontalRotationAmount = 45f;
    [SerializeField] private float verticalRotationAmount = 18f;
    [SerializeField] private float rotationSmoothness = 5f;

    private InspectableHabitat currentHabitat;
    private CameraSwipe cameraSwipe;

    private bool isInspecting;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;

    private float currentHorizontalAngle;
    private float currentVerticalAngle;

    private void Start()
    {
        LanguageManager.Ensure();
        EnsureEventSystem();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null)
            cameraSwipe = mainCamera.GetComponent<CameraSwipe>();

        if (dimBackground != null)
            dimBackground.SetActive(false);

        if (infoPanel != null)
            infoPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(() =>
            {
                FindFirstObjectByType<MainAreaManager>()?.NotifyExitInspection();
            });

        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);
    }

    private void Update()
    {
        if (!isInspecting || currentHabitat == null || mainCamera == null)
            return;

        UpdateCameraInspection();
    }

    public void OpenMinigame(InspectableHabitat habitat)
    {
        if (habitat == null || mainCamera == null)
            return;

        currentHabitat = habitat;
        isInspecting = true;

        originalCameraPosition = mainCamera.transform.position;
        originalCameraRotation = mainCamera.transform.rotation;

        currentHorizontalAngle = 0f;
        currentVerticalAngle = 0f;

        if (cameraSwipe != null)
            cameraSwipe.enabled = false;

        SetInspectionUI(habitat);

        if (dimBackground != null)
            dimBackground.SetActive(true);

        if (infoPanel != null)
            infoPanel.SetActive(true);
    }

    public void CloseInspectionMode()
    {
        isInspecting = false;
        currentHabitat = null;

        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPosition;
            mainCamera.transform.rotation = originalCameraRotation;
        }

        if (cameraSwipe != null)
            cameraSwipe.enabled = true;

        if (dimBackground != null)
            dimBackground.SetActive(false);

        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    private void UpdateCameraInspection()
    {
        Vector2 input = GetInspectionInput();

        float targetHorizontal = input.x * horizontalRotationAmount;
        float targetVertical = -input.y * verticalRotationAmount;

        currentHorizontalAngle = Mathf.Lerp(
            currentHorizontalAngle,
            targetHorizontal,
            Time.deltaTime * rotationSmoothness
        );

        currentVerticalAngle = Mathf.Lerp(
            currentVerticalAngle,
            targetVertical,
            Time.deltaTime * rotationSmoothness
        );

        Vector3 targetCenter = currentHabitat.GetInspectionCenter() + Vector3.up * lookAtHeight;

        Quaternion orbitRotation = Quaternion.Euler(
            currentVerticalAngle,
            currentHorizontalAngle,
            0f
        );

        Vector3 offset = orbitRotation * new Vector3(0f, cameraHeight, -cameraDistance);
        Vector3 desiredPosition = targetCenter + offset;

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            desiredPosition,
            Time.deltaTime * cameraMoveSpeed
        );

        Quaternion desiredRotation = Quaternion.LookRotation(targetCenter - mainCamera.transform.position);

        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            desiredRotation,
            Time.deltaTime * cameraMoveSpeed
        );
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
            Vector3 acceleration = Accelerometer.current.acceleration.ReadValue();
            input.x = acceleration.x;
            input.y = acceleration.y;
        }
#endif

        input.x = Mathf.Clamp(input.x, -1f, 1f);
        input.y = Mathf.Clamp(input.y, -1f, 1f);

        return input;
    }

    private void SetInspectionUI(InspectableHabitat habitat)
    {
        if (animalNameText != null)
            animalNameText.text = LanguageManager.Instance.Get(habitat.AnimalNameKey);

        if (descriptionText != null)
            descriptionText.text = LanguageManager.Instance.Get(habitat.HabitatDescriptionKey);

        if (factText != null)
            factText.text = LanguageManager.Instance.Get(habitat.EducationalFactKey);
    }

    private void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
            return;

        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }
}