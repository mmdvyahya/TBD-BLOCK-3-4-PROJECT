using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArcticCoolingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MicrophoneInput microphoneInput;
    [SerializeField] private Image coolingFill;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Renderer animalRenderer;

    [Header("Cooling")]
    [SerializeField] private float secondsOfSoundNeeded = 5f;
    [SerializeField] private bool drainWhenSilent = false;
    [SerializeField] private float drainSpeed = 0.25f;
    [SerializeField] private float colorChangeSpeed = 1.5f;

    private float coolingProgress;
    private bool completed;
    private Material animalMaterial;
    private Color originalAnimalColor;

    private void Start()
    {
        if (microphoneInput == null)
            microphoneInput = FindFirstObjectByType<MicrophoneInput>();

        if (animalRenderer != null)
        {
            animalMaterial = animalRenderer.material;
            originalAnimalColor = animalMaterial.color;
        }

        UpdateUI();
    }

    private void Update()
    {
        if (completed)
            return;

        bool soundDetected = microphoneInput != null && microphoneInput.WasBlowDetectedThisFrame;

        if (soundDetected)
        {
            coolingProgress += Time.deltaTime / secondsOfSoundNeeded;
        }
        else if (drainWhenSilent)
        {
            coolingProgress -= Time.deltaTime * drainSpeed;
        }

        coolingProgress = Mathf.Clamp01(coolingProgress);

        ApplyCoolingVisual();
        UpdateUI();

        if (coolingProgress >= 1f)
            CompleteMinigame();
    }

    private void ApplyCoolingVisual()
    {
        if (coolingFill != null)
            coolingFill.fillAmount = coolingProgress;

        if (animalMaterial != null)
        {
            Color targetColor = Color.Lerp(originalAnimalColor, Color.cyan, coolingProgress);
            animalMaterial.color = Color.Lerp(animalMaterial.color, targetColor, Time.deltaTime * colorChangeSpeed);
        }
    }

    private void UpdateUI()
    {
        if (instructionText == null)
            return;

        int percent = Mathf.RoundToInt(coolingProgress * 100f);

        if (coolingProgress < 1f)
            instructionText.text = "BLOW!\n" + percent + "%";
        else
            instructionText.text = "Animal cooled!";
    }

    private void CompleteMinigame()
    {
        completed = true;

        if (coolingFill != null)
            coolingFill.fillAmount = 1f;

        if (instructionText != null)
            instructionText.text = "Animal cooled!";

        Debug.Log("[ArcticCooling] Complete. Reward system connects here.");
    }
}