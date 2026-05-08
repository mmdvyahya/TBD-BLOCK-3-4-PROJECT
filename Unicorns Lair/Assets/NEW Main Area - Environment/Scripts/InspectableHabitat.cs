using UnityEngine;

public class InspectableHabitat : MonoBehaviour
{
    [Header("Localization Keys")]
    [SerializeField] private string animalNameKey;
    [SerializeField] private string habitatDescriptionKey;
    [SerializeField] private string educationalFactKey;

    [Header("Camera View")]
    [Tooltip("Empty Transform positioned where the camera should be (and rotated how it should look) when this habitat is tapped. The inspection orbit will start from here.")]
    [SerializeField] private Transform habitatCamView;

    [Header("Inspection Orbit Center")]
    [Tooltip("Empty Transform at the center the inspection orbit should rotate around. Falls back to this object's position if empty.")]
    [SerializeField] private Transform inspectionCenter;

    [Header("Minigame")]
    [Tooltip("Name of the scene to load when the Minigame button is pressed. Leave empty if no minigame.")]
    [SerializeField] private string minigameScene;

    public string AnimalNameKey => animalNameKey;
    public string HabitatDescriptionKey => habitatDescriptionKey;
    public string EducationalFactKey => educationalFactKey;
    public string MinigameScene => minigameScene;
    public bool HasMinigame => !string.IsNullOrEmpty(minigameScene);
    public bool HasCamView => habitatCamView != null;
    public Vector3 CamViewPosition => habitatCamView != null ? habitatCamView.position : transform.position;
    public Quaternion CamViewRotation => habitatCamView != null ? habitatCamView.rotation : transform.rotation;

    public Vector3 GetInspectionCenter()
    {
        if (inspectionCenter != null) return inspectionCenter.position;
        return transform.position;
    }
}