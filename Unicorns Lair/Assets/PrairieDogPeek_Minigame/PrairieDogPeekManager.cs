using System.Collections;
using UnityEngine;
using TMPro;

public class PrairieDogPeekManager : MonoBehaviour
{
    private enum PrairieDogPeekState
    {
        WaitingForShake,
        Revealing,
        WaitingForChoice,
        Feedback,
        Complete
    }

    [Header("References")]
    [SerializeField] private PrairieDogShakeInput shakeInput;
    [SerializeField] private PrairieDogHole[] holes;

    [Header("UI")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Timing")]
    [SerializeField] private float visibleDuration = 2f;
    [SerializeField] private float feedbackDuration = 1.2f;

    [Header("Debug")]
    [SerializeField] private bool autoFindHoles = true;

    private PrairieDogPeekState currentState;
    private int correctHoleIndex = -1;

    private void Start()
    {
        if (shakeInput == null)
            shakeInput = FindFirstObjectByType<PrairieDogShakeInput>();

        if (autoFindHoles && (holes == null || holes.Length == 0))
            holes = FindObjectsByType<PrairieDogHole>(FindObjectsSortMode.None);

        InitializeHoles();
        StartMinigame();
    }

    private void Update()
    {
        if (currentState != PrairieDogPeekState.WaitingForShake)
            return;

        if (shakeInput != null && shakeInput.WasShakeDetectedThisFrame)
        {
            StartCoroutine(RevealSequence());
        }
    }

    public void StartMinigame()
    {
        SetState(PrairieDogPeekState.WaitingForShake);
        SetInstruction("Shake the tablet!");
        SetFeedback("");

        foreach (var hole in holes)
        {
            if (hole != null)
                hole.HideDogInstant();
        }
    }

    private void InitializeHoles()
    {
        for (int i = 0; i < holes.Length; i++)
        {
            if (holes[i] != null)
                holes[i].Initialize(this, i);
        }
    }

    private IEnumerator RevealSequence()
    {
        SetState(PrairieDogPeekState.Revealing);
        SetInstruction("Watch carefully!");
        SetFeedback("");

        correctHoleIndex = Random.Range(0, holes.Length);

        PrairieDogHole correctHole = holes[correctHoleIndex];

        if (correctHole != null)
            yield return StartCoroutine(correctHole.PlayReveal(visibleDuration));

        SetState(PrairieDogPeekState.WaitingForChoice);
        SetInstruction("Tap the hole!");
    }

    public void NotifyHolePressed(PrairieDogHole hole)
    {
        if (currentState != PrairieDogPeekState.WaitingForChoice)
            return;

        if (hole == null)
            return;

        StartCoroutine(HandleChoice(hole));
    }

    private IEnumerator HandleChoice(PrairieDogHole selectedHole)
    {
        SetState(PrairieDogPeekState.Feedback);

        bool correct = selectedHole.HoleIndex == correctHoleIndex;

        if (correct)
        {
            SetFeedback("Correct!");
            yield return StartCoroutine(selectedHole.PlayCorrectFeedback());
        }
        else
        {
            SetFeedback("Try again! It was here.");
            yield return StartCoroutine(selectedHole.PlayWrongFeedback());

            PrairieDogHole correctHole = holes[correctHoleIndex];
            if (correctHole != null)
                yield return StartCoroutine(correctHole.PlayCorrectFeedback());
        }

        yield return new WaitForSeconds(feedbackDuration);

        CompleteMinigame();
    }

    private void CompleteMinigame()
    {
        SetState(PrairieDogPeekState.Complete);
        SetInstruction("");
        SetFeedback("Prairie Dog Peek complete!");

        Debug.Log("[PrairieDogPeek] Minigame complete. Reward system connects here.");
    }

    private void SetState(PrairieDogPeekState next)
    {
        currentState = next;
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