using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwipe : MonoBehaviour
{
    [Header("Swipe Settings")]
    [SerializeField] private float swipeSensitivity = 0.018f;
    [SerializeField] private float smoothing = 8f;
    [SerializeField] private float swipeRangeBack = 12f;
    [SerializeField] private float swipeRangeFront = 12f;

    [Header("Feel")]
    [SerializeField] private float momentumDecay = 0.88f;

    private Vector3 _targetPos;
    private float _startZ;
    private float _minZ;
    private float _maxZ;
    private float _velocity;
    private float _prevInputX;
    private bool _dragging;

    void Start()
    {
        _startZ = transform.position.z;
        _minZ = _startZ - swipeRangeBack;
        _maxZ = _startZ + swipeRangeFront;
        _targetPos = transform.position;
    }

    void Update()
    {
        HandleInput();
        ApplyMovement();
    }

    void HandleInput()
    {
        bool touchActive = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
        bool mouseActive = Mouse.current != null && Mouse.current.leftButton.isPressed;

        if (touchActive)
        {
            float inputX = Touchscreen.current.primaryTouch.position.ReadValue().x;
            if (!_dragging)
            {
                _prevInputX = inputX;
                _dragging = true;
                _velocity = 0f;
            }
            else
            {
                float delta = (inputX - _prevInputX) * swipeSensitivity;
                _velocity = delta / Mathf.Max(Time.deltaTime, 0.001f);
                _targetPos.z = Mathf.Clamp(_targetPos.z - delta * Screen.width * swipeSensitivity, _minZ, _maxZ);
                _prevInputX = inputX;
            }
        }
        else if (mouseActive)
        {
            if (!_dragging)
            {
                _prevInputX = Mouse.current.position.ReadValue().x;
                _dragging = true;
                _velocity = 0f;
            }
            else
            {
                float delta = Mouse.current.delta.ReadValue().x;
                _velocity = delta;
                _targetPos.z = Mathf.Clamp(_targetPos.z - delta * swipeSensitivity * 60f, _minZ, _maxZ);
            }
        }
        else
        {
            _dragging = false;
            _velocity *= momentumDecay;
            if (Mathf.Abs(_velocity) > 0.01f)
                _targetPos.z = Mathf.Clamp(_targetPos.z - _velocity * swipeSensitivity * 40f * Time.deltaTime, _minZ, _maxZ);
        }

        _targetPos.x = transform.position.x;
        _targetPos.y = transform.position.y;
    }

    void ApplyMovement()
    {
        transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime * smoothing);
    }
}