using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeatherRainManager : MonoBehaviour
{
    [Header("Rain")]
    [SerializeField] private GameObject rainParticles;
    [SerializeField] private float timeBetweenRain = 40f;
    [SerializeField] private float rainDuration = 10f;

    [Header("Rain Impacts")]
    [SerializeField] private RainImpactSpawner rainImpactSpawner;

    [Header("Wet Look")]
    [SerializeField] private bool enableWetLook = true;
    [SerializeField] private Renderer[] wetRenderers;

    [SerializeField] private float wetSmoothness = 1f;

    [SerializeField]
    private Color wetTint = new Color(0.35f, 0.42f, 0.48f, 1f);

    [Header("Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;
    [SerializeField] private Key toggleRainKey = Key.P;

    private bool isRaining;
    private Coroutine rainRoutine;

    private readonly List<Material> runtimeMaterials = new List<Material>();
    private readonly List<Color> originalColors = new List<Color>();
    private readonly List<float> originalSmoothness = new List<float>();

    private void Start()
    {
        if (rainParticles != null)
            rainParticles.SetActive(false);

        if (rainImpactSpawner != null)
            rainImpactSpawner.SetActiveRainImpacts(false);

        CacheMaterials();

        rainRoutine = StartCoroutine(AutoRainLoop());
    }

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (allowKeyboardDebug &&
            Keyboard.current != null &&
            Keyboard.current[toggleRainKey].wasPressedThisFrame)
        {
            ToggleRainManual();
        }
#endif
    }

    private IEnumerator AutoRainLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenRain);

            if (!isRaining)
            {
                SetRain(true);

                yield return new WaitForSeconds(rainDuration);

                SetRain(false);
            }
        }
    }

    private void ToggleRainManual()
    {
        SetRain(!isRaining);
    }

    private void SetRain(bool value)
    {
        isRaining = value;

        if (rainParticles != null)
            rainParticles.SetActive(isRaining);

        if (rainImpactSpawner != null)
            rainImpactSpawner.SetActiveRainImpacts(isRaining);

        ApplyWetLook(isRaining);

        Debug.Log(isRaining
            ? "[Weather] Rain started"
            : "[Weather] Rain stopped");
    }

    private void CacheMaterials()
    {
        runtimeMaterials.Clear();
        originalColors.Clear();
        originalSmoothness.Clear();

        if (wetRenderers == null || wetRenderers.Length == 0)
            wetRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        foreach (Renderer r in wetRenderers)
        {
            if (r == null)
                continue;

            Material mat = r.material;

            runtimeMaterials.Add(mat);

            Color originalColor = Color.white;

            if (mat.HasProperty("_BaseColor"))
                originalColor = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color"))
                originalColor = mat.GetColor("_Color");

            originalColors.Add(originalColor);

            float smoothness = 0f;

            if (mat.HasProperty("_Smoothness"))
                smoothness = mat.GetFloat("_Smoothness");

            originalSmoothness.Add(smoothness);
        }
    }

    private void ApplyWetLook(bool wet)
    {
        if (!enableWetLook)
            return;

        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            Material mat = runtimeMaterials[i];

            if (mat == null)
                continue;

            Color targetColor = wet
                ? Color.Lerp(originalColors[i], wetTint, 0.45f)
                : originalColors[i];

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", targetColor);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", targetColor);

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat(
                    "_Smoothness",
                    wet ? wetSmoothness : originalSmoothness[i]
                );
            }

            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat(
                    "_Metallic",
                    wet ? 0.15f : 0f
                );
            }
        }
    }
}