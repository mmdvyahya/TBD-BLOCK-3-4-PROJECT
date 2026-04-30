using UnityEngine;

public class HabitatTapHandler : MonoBehaviour
{
    private InspectableHabitat inspectableHabitat;

    private void Awake()
    {
        inspectableHabitat = GetComponent<InspectableHabitat>();
    }

    private void OnMouseDown()
    {
        Debug.Log("Clicked habitat: " + gameObject.name);
        if (MainAreaManagerExists() == false) return;
        MainAreaManagerInstance().NotifyHabitatTapped(inspectableHabitat);
    }

    private bool MainAreaManagerExists()
    {
        return FindFirstObjectByType<MainAreaManager>() != null;
    }

    private MainAreaManager MainAreaManagerInstance()
    {
        return FindFirstObjectByType<MainAreaManager>();
    }
}