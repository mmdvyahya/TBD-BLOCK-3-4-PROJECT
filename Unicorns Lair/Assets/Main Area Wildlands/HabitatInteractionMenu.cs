using UnityEngine;
using UnityEngine.UI;

public class HabitatInteractionMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HabitatInspectionManager inspectionManager;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button inspectButton;

    private InspectableHabitat selectedHabitat;

    private void Start()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);

        if (inspectButton != null)
            inspectButton.onClick.AddListener(PressInspect);
    }

    public void OpenMenu(InspectableHabitat habitat)
    {
        selectedHabitat = habitat;

        if (menuPanel != null)
            menuPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        selectedHabitat = null;

        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    private void PressInspect()
    {
        if (selectedHabitat == null)
            return;

        FindFirstObjectByType<MainAreaManager>()?.NotifyInspectHabitat(selectedHabitat);
    }
}