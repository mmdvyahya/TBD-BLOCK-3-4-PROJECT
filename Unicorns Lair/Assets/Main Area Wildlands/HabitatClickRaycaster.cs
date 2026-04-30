using UnityEngine;
using UnityEngine.InputSystem;

public class HabitatClickRaycaster : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        bool pressed = false;
        Vector2 screenPos = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressed = true;
            screenPos = Mouse.current.position.ReadValue();
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pressed = true;
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (!pressed || mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Debug.Log("Raycast hit: " + hit.collider.gameObject.name);

            InspectableHabitat habitat = hit.collider.GetComponentInParent<InspectableHabitat>();

            if (habitat != null)
            {
                FindFirstObjectByType<MainAreaManager>()?.NotifyHabitatTapped(habitat);
            }
        }
        else
        {
            Debug.Log("Raycast hit nothing");
        }
    }
}