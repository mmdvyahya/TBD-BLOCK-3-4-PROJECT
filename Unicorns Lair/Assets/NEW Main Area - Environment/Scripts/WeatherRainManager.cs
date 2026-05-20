using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeatherRainManager : MonoBehaviour
{
    [Header("Rain")]
    [SerializeField] private GameObject rainParticles;
    [SerializeField] private float timeBetweenRain = 40f;
    [SerializeField] private float rainDuration = 10f;

    [Header("Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;
    [SerializeField] private Key toggleRainKey = Key.P;
    private bool isRaining;

    private void Start()
    {
        if (rainParticles != null)
            rainParticles.SetActive(false);

        StartCoroutine(AutoRainLoop());
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

        Debug.Log(isRaining
            ? "[Weather] Rain started"
            : "[Weather] Rain stopped");
    }
}