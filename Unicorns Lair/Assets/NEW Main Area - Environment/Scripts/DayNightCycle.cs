using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Main References")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private Camera mainCamera;

    [Header("Cycle Settings")]
    [SerializeField] private bool cycleEnabled = true;
    [SerializeField] private float fullDayDuration = 120f;
    [SerializeField] [Range(0f, 1f)] private float startTimeOfDay = 0.25f;

    [Header("Light Intensity")]
    [SerializeField] private float dayIntensity = 1.2f;
    [SerializeField] private float sunsetIntensity = 0.75f;
    [SerializeField] private float nightIntensity = 0.18f;

    [Header("Light Colors")]
    [SerializeField] private Color dayLightColor = new Color(1f, 0.95f, 0.85f);
    [SerializeField] private Color sunsetLightColor = new Color(1f, 0.55f, 0.25f);
    [SerializeField] private Color nightLightColor = new Color(0.25f, 0.35f, 0.8f);

    private float timeOfDay;

    private void Start()
    {
        if (directionalLight == null)
            directionalLight = FindFirstObjectByType<Light>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        timeOfDay = startTimeOfDay;

        UpdateLighting();
    }

    private void Update()
    {
        if (!cycleEnabled)
            return;

        timeOfDay += Time.deltaTime / fullDayDuration;

        if (timeOfDay > 1f)
            timeOfDay = 0f;

        UpdateLighting();
    }

    private void UpdateLighting()
    {
        if (directionalLight == null)
            return;

        float intensity;
        Color lightColor;

        // DAY
        if (timeOfDay >= 0.25f && timeOfDay < 0.60f)
        {
            float t = Mathf.InverseLerp(0.25f, 0.60f, timeOfDay);

            intensity = Mathf.Lerp(sunsetIntensity, dayIntensity, t);
            lightColor = Color.Lerp(sunsetLightColor, dayLightColor, t);
        }
        // SUNSET
        else if (timeOfDay >= 0.60f && timeOfDay < 0.78f)
        {
            float t = Mathf.InverseLerp(0.60f, 0.78f, timeOfDay);

            intensity = Mathf.Lerp(dayIntensity, sunsetIntensity, t);
            lightColor = Color.Lerp(dayLightColor, sunsetLightColor, t);
        }
        // NIGHT (late evening)
        else if (timeOfDay >= 0.78f)
        {
            float t = Mathf.InverseLerp(0.78f, 1f, timeOfDay);

            intensity = Mathf.Lerp(sunsetIntensity, nightIntensity, t);
            lightColor = Color.Lerp(sunsetLightColor, nightLightColor, t);
        }
        // EARLY MORNING (sunrise transition)
        else
        {
            float t = Mathf.InverseLerp(0f, 0.25f, timeOfDay);

            intensity = Mathf.Lerp(nightIntensity, sunsetIntensity, t);
            lightColor = Color.Lerp(nightLightColor, sunsetLightColor, t);
        }

        directionalLight.intensity = intensity;
        directionalLight.color = lightColor;
    }
}