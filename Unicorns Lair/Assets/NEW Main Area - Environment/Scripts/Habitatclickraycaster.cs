using UnityEngine;
using UnityEngine.InputSystem;

public class HabitatClickRaycaster : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private HabitatInteractionController controller;

    [Header("Tap Detection")]
    [Tooltip("Max time finger/mouse can be held down for it to still count as a tap.")]
    [SerializeField] private float maxTapDuration = 0.5f;
    [Tooltip("Max pixels finger/mouse can drift between press and release before it's treated as a drag instead of a tap.")]
    [SerializeField] private float maxTapDrift = 30f;

    private Vector2 _pressStartPos;
    private Vector2 _lastKnownPos;
    private float _pressStartTime;
    private bool _tracking;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (controller == null) controller = FindFirstObjectByType<HabitatInteractionController>();
    }

    private void Update()
    {
        if (mainCamera == null || controller == null) return;
        if (controller.IsBusy) { _tracking = false; return; }

        int activeTouches = CountActiveTouches();

        bool justPressed = false;
        bool justReleased = false;
        bool isPressed = false;
        Vector2 currentPos = _lastKnownPos;

        if (Touchscreen.current != null)
        {
            var primary = Touchscreen.current.primaryTouch;
            if (primary.press.wasPressedThisFrame) justPressed = true;
            if (primary.press.wasReleasedThisFrame) justReleased = true;
            if (primary.press.isPressed)
            {
                isPressed = true;
                currentPos = primary.position.ReadValue();
            }
        }

        if (Mouse.current != null && activeTouches == 0)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) justPressed = true;
            if (Mouse.current.leftButton.wasReleasedThisFrame) justReleased = true;
            if (Mouse.current.leftButton.isPressed)
            {
                isPressed = true;
                currentPos = Mouse.current.position.ReadValue();
            }
        }

        if (isPressed) _lastKnownPos = currentPos;

        if (activeTouches > 1) { _tracking = false; return; }

        if (justPressed)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                _tracking = false;
            }
            else
            {
                _pressStartPos = currentPos;
                _pressStartTime = Time.time;
                _tracking = true;
            }
        }

        if (_tracking)
        {
            if (Vector2.Distance(currentPos, _pressStartPos) > maxTapDrift)
            {
                _tracking = false;
                return;
            }
            if (Time.time - _pressStartTime > maxTapDuration)
            {
                _tracking = false;
                return;
            }
        }

        if (_tracking && justReleased)
        {
            _tracking = false;

            Ray ray = mainCamera.ScreenPointToRay(_lastKnownPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 9999f))
            {
                var habitat = hit.collider.GetComponentInParent<InspectableHabitat>()
                           ?? hit.collider.GetComponent<InspectableHabitat>();
                if (habitat != null)
                    controller.OpenHabitat(habitat);
            }
        }
    }

    int CountActiveTouches()
    {
        var ts = Touchscreen.current;
        if (ts == null) return 0;
        int count = 0;
        for (int i = 0; i < ts.touches.Count; i++)
            if (ts.touches[i].press.isPressed) count++;
        return count;
    }
}