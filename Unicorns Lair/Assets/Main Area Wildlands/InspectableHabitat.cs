using UnityEngine;

public class InspectableHabitat : MonoBehaviour
{
    [Header("Localisation Keys")]
    [SerializeField] private string animalNameKey;
    [SerializeField] private string habitatDescriptionKey;
    [SerializeField] private string educationalFactKey;

    [Header("Inspection Center")]
    [SerializeField] private Transform inspectionCenter;

    public string AnimalNameKey => animalNameKey;
    public string HabitatDescriptionKey => habitatDescriptionKey;
    public string EducationalFactKey => educationalFactKey;

    public Vector3 GetInspectionCenter()
    {
        if (inspectionCenter != null)
            return inspectionCenter.position;

        return transform.position;
    }
}