using UnityEngine;

public class DayNightCycleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light sunLight;
    [SerializeField] private Camera mainCamera;

    [Header("Cycle Settings")]
    [SerializeField] private float fullDayDuration = 180f;
    [SerializeField, Range(0f, 1f)] private float startTimeOfDay = 0.25f;

    [Header("Light Intensity")]
    [SerializeField] private float dayIntensity = 1.2f;
    [SerializeField] private float sunsetIntensity = 0.75f;
    [SerializeField] private float nightIntensity = 0.18f;

    [Header("Light Colors")]
    [SerializeField] private Color dayLightColor = new Color(1f, 0.96f, 0.86f);
    [SerializeField] private Color sunsetLightColor = new Color(1f, 0.55f, 0.25f);
    [SerializeField] private Color nightLightColor = new Color(0.25f, 0.35f, 0.75f);

    [Header("Camera Background")]
    [SerializeField] private Color daySkyColor = new Color(0.53f, 0.81f, 0.98f);
    [SerializeField] private Color sunsetSkyColor = new Color(1f, 0.45f, 0.25f);
    [SerializeField] private Color nightSkyColor = new Color(0.03f, 0.05f, 0.12f);

    private float timeOfDay;

    private void Start()
    {
        if (sunLight == null)
            sunLight = FindFirstObjectByType<Light>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        timeOfDay = startTimeOfDay;
        ApplyTimeOfDay();
    }

    private void Update()
    {
        if (fullDayDuration <= 0f)
            return;

        timeOfDay += Time.deltaTime / fullDayDuration;

        if (timeOfDay > 1f)
            timeOfDay -= 1f;

        ApplyTimeOfDay();
    }

    private void ApplyTimeOfDay()
    {
        if (sunLight == null)
            return;

        // Rotate sun through the day
        float sunAngle = timeOfDay * 360f - 90f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        Color targetLightColor;
        Color targetSkyColor;
        float targetIntensity;

        // 0.00 = midnight
        // 0.25 = sunrise
        // 0.50 = noon
        // 0.75 = sunset
        if (timeOfDay < 0.20f)
        {
            targetLightColor = nightLightColor;
            targetSkyColor = nightSkyColor;
            targetIntensity = nightIntensity;
        }
        else if (timeOfDay < 0.35f)
        {
            float t = Mathf.InverseLerp(0.20f, 0.35f, timeOfDay);
            targetLightColor = Color.Lerp(nightLightColor, sunsetLightColor, t);
            targetSkyColor = Color.Lerp(nightSkyColor, sunsetSkyColor, t);
            targetIntensity = Mathf.Lerp(nightIntensity, sunsetIntensity, t);
        }
        else if (timeOfDay < 0.65f)
        {
            float t = Mathf.InverseLerp(0.35f, 0.65f, timeOfDay);
            targetLightColor = Color.Lerp(sunsetLightColor, dayLightColor, t);
            targetSkyColor = Color.Lerp(sunsetSkyColor, daySkyColor, t);
            targetIntensity = Mathf.Lerp(sunsetIntensity, dayIntensity, t);
        }
        else if (timeOfDay < 0.82f)
        {
            float t = Mathf.InverseLerp(0.65f, 0.82f, timeOfDay);
            targetLightColor = Color.Lerp(dayLightColor, sunsetLightColor, t);
            targetSkyColor = Color.Lerp(daySkyColor, sunsetSkyColor, t);
            targetIntensity = Mathf.Lerp(dayIntensity, sunsetIntensity, t);
        }
        else
        {
            float t = Mathf.InverseLerp(0.82f, 1f, timeOfDay);
            targetLightColor = Color.Lerp(sunsetLightColor, nightLightColor, t);
            targetSkyColor = Color.Lerp(sunsetSkyColor, nightSkyColor, t);
            targetIntensity = Mathf.Lerp(sunsetIntensity, nightIntensity, t);
        }

        sunLight.color = targetLightColor;
        sunLight.intensity = targetIntensity;

        if (mainCamera != null)
            mainCamera.backgroundColor = targetSkyColor;
    }
}