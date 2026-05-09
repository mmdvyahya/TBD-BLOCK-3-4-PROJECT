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

    [Header("Camera Background")]
    [SerializeField] private Color daySkyColor = new Color(0.55f, 0.82f, 1f);
    [SerializeField] private Color sunsetSkyColor = new Color(1f, 0.45f, 0.25f);
    [SerializeField] private Color nightSkyColor = new Color(0.03f, 0.05f, 0.12f);

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
        Color skyColor;

        // DAY
        if (timeOfDay >= 0.25f && timeOfDay < 0.60f)
        {
            float t = Mathf.InverseLerp(0.25f, 0.60f, timeOfDay);

            intensity = Mathf.Lerp(sunsetIntensity, dayIntensity, t);
            lightColor = Color.Lerp(sunsetLightColor, dayLightColor, t);
            skyColor = Color.Lerp(sunsetSkyColor, daySkyColor, t);
        }
        // SUNSET
        else if (timeOfDay >= 0.60f && timeOfDay < 0.78f)
        {
            float t = Mathf.InverseLerp(0.60f, 0.78f, timeOfDay);

            intensity = Mathf.Lerp(dayIntensity, sunsetIntensity, t);
            lightColor = Color.Lerp(dayLightColor, sunsetLightColor, t);
            skyColor = Color.Lerp(daySkyColor, sunsetSkyColor, t);
        }
        // NIGHT
        else
        {
            float t;

            if (timeOfDay >= 0.78f)
                t = Mathf.InverseLerp(0.78f, 1f, timeOfDay);
            else
                t = Mathf.InverseLerp(0f, 0.25f, timeOfDay);

            intensity = Mathf.Lerp(sunsetIntensity, nightIntensity, t);
            lightColor = Color.Lerp(sunsetLightColor, nightLightColor, t);
            skyColor = Color.Lerp(sunsetSkyColor, nightSkyColor, t);
        }

        directionalLight.intensity = intensity;
        directionalLight.color = lightColor;

        if (mainCamera != null)
            mainCamera.backgroundColor = skyColor;
    }
}