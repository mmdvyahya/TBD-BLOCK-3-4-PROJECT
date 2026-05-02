using System.Collections;
using UnityEngine;
using TMPro;

public class ParrotFeedingManager : MonoBehaviour
{
    private enum ParrotFeedingState
    {
        Playing,
        Checking,
        Complete
    }

    [Header("References")]
    [SerializeField] private ParrotTiltPourInput tiltInput;
    [SerializeField] private FeedingTray feedingTray;

    [Header("Scene Objects")]
    [SerializeField] private Transform seedSack;
    [SerializeField] private Transform seedSpawnPoint;
    [SerializeField] private GameObject seedPrefab;
    [SerializeField] private Transform parrotVisual;

    [Header("UI")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Sack Movement")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float moveRange = 3.2f;

    [Header("Seeds")]
    [SerializeField] private int maxSeedsToDrop = 25;
    [SerializeField] private float seedSpawnInterval = 0.08f;
    [SerializeField] private float seedSpread = 0.45f;

    [Header("Timing")]
    [SerializeField] private float maxDuration = 20f;
    [SerializeField] private float finalSeedCheckDelay = 2f;

    private ParrotFeedingState currentState;
    private Vector3 sackStartPosition;
    private float direction = 1f;
    private float seedSpawnTimer;
    private float gameTimer;
    private bool hasPouredAtLeastOnce;
    private int seedsDropped;

    private void Start()
    {
        if (tiltInput == null)
            tiltInput = FindFirstObjectByType<ParrotTiltPourInput>();

        if (feedingTray == null)
            feedingTray = FindFirstObjectByType<FeedingTray>();

        if (seedSack != null)
            sackStartPosition = seedSack.position;

        StartMinigame();
    }

    private void Update()
    {
        if (currentState != ParrotFeedingState.Playing)
            return;

        gameTimer += Time.deltaTime;

        bool isPouring = tiltInput != null && tiltInput.IsPouring;

        if (isPouring && seedsDropped < maxSeedsToDrop)
        {
            hasPouredAtLeastOnce = true;
            SetInstruction("Pour!");
            SpawnSeedsOverTime();
        }
        else
        {
            if (seedsDropped >= maxSeedsToDrop)
                SetInstruction("No seeds left!");
            else
                SetInstruction("Tilt to pour!");

            MoveSack();
        }

        AnimateParrot();

        if (feedingTray != null && feedingTray.IsFull)
        {
            CompleteMinigame();
            return;
        }

        if (seedsDropped >= maxSeedsToDrop && feedingTray != null && !feedingTray.IsFull)
        {
            StartCoroutine(CheckFinalSeedsAfterDelay());
            return;
        }

        if (gameTimer >= maxDuration)
        {
            if (!hasPouredAtLeastOnce)
                StartCoroutine(HandleFailure("Try again! Tilt the tablet!"));
            else
                StartCoroutine(CheckFinalSeedsAfterDelay());

            return;
        }
    }

    public void StartMinigame()
    {
        currentState = ParrotFeedingState.Playing;

        gameTimer = 0f;
        seedSpawnTimer = 0f;
        hasPouredAtLeastOnce = false;
        seedsDropped = 0;

        if (feedingTray != null)
            feedingTray.ResetTray();

        if (seedSack != null)
            seedSack.position = sackStartPosition;

        SetInstruction("Tilt to pour!");
        SetFeedback("");
    }

    private void MoveSack()
    {
        if (seedSack == null)
            return;

        seedSack.position += Vector3.right * direction * moveSpeed * Time.deltaTime;

        float distanceFromStart = seedSack.position.x - sackStartPosition.x;

        if (Mathf.Abs(distanceFromStart) >= moveRange)
        {
            direction *= -1f;

            Vector3 pos = seedSack.position;
            pos.x = sackStartPosition.x + Mathf.Sign(distanceFromStart) * moveRange;
            seedSack.position = pos;
        }
    }

    private void SpawnSeedsOverTime()
    {
        seedSpawnTimer -= Time.deltaTime;

        if (seedSpawnTimer > 0f)
            return;

        seedSpawnTimer = seedSpawnInterval;
        SpawnSeed();
    }

    private void SpawnSeed()
    {
        if (seedPrefab == null || seedSpawnPoint == null)
            return;

        if (seedsDropped >= maxSeedsToDrop)
            return;

        Vector3 spawnPos = seedSpawnPoint.position;
        spawnPos.x += Random.Range(-seedSpread, seedSpread);
        spawnPos.z += Random.Range(-seedSpread * 0.35f, seedSpread * 0.35f);

        Instantiate(seedPrefab, spawnPos, Quaternion.identity);
        seedsDropped++;
    }

    private void AnimateParrot()
    {
        if (parrotVisual == null || feedingTray == null)
            return;

        float excitement = feedingTray.FillPercent;
        float bounce = Mathf.Sin(Time.time * 8f) * 0.06f * excitement;

        Vector3 pos = parrotVisual.localPosition;
        pos.y = bounce;
        parrotVisual.localPosition = pos;
    }

    private void CompleteMinigame()
    {
        if (currentState == ParrotFeedingState.Complete)
            return;

        currentState = ParrotFeedingState.Complete;

        SetInstruction("");
        SetFeedback("Parrot fed!");

        StartCoroutine(ParrotHappySequence());

        Debug.Log("[ParrotFeeding] Minigame complete. Reward system connects here.");
    }

    private IEnumerator CheckFinalSeedsAfterDelay()
    {
        if (currentState == ParrotFeedingState.Complete || currentState == ParrotFeedingState.Checking)
            yield break;

        currentState = ParrotFeedingState.Checking;

        SetInstruction("Checking seeds...");

        yield return new WaitForSeconds(finalSeedCheckDelay);

        if (feedingTray != null && feedingTray.IsFull)
        {
            CompleteMinigame();
        }
        else
        {
            SetInstruction("");
            SetFeedback("Try again! Aim for the tray!");

            yield return new WaitForSeconds(2f);

            StartMinigame();
        }
    }

    private IEnumerator HandleFailure(string message)
    {
        if (currentState == ParrotFeedingState.Complete || currentState == ParrotFeedingState.Checking)
            yield break;

        currentState = ParrotFeedingState.Complete;

        SetInstruction("");
        SetFeedback(message);

        yield return new WaitForSeconds(2f);

        StartMinigame();
    }

    private IEnumerator ParrotHappySequence()
    {
        if (parrotVisual == null)
            yield break;

        Vector3 baseScale = parrotVisual.localScale;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t * 25f) * 0.08f;
            parrotVisual.localScale = baseScale * s;
            yield return null;
        }

        parrotVisual.localScale = baseScale;
    }

    private void SetInstruction(string text)
    {
        if (instructionText != null)
            instructionText.text = text;
    }

    private void SetFeedback(string text)
    {
        if (feedbackText != null)
            feedbackText.text = text;
    }
}