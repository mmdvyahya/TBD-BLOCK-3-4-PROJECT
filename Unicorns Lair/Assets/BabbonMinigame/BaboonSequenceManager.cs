using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BaboonSequenceManager : MonoBehaviour
{
    private enum BaboonSequenceState
    {
        Demonstrating,
        PlayerTurn,
        Feedback,
        Complete
    }

    [Header("References")]
    [SerializeField] private BaboonSequenceButton[] sequenceButtons;
    [SerializeField] private Transform baboonVisual;

    [Header("UI")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Sequence Settings")]
    [SerializeField] private int sequenceLength = 3;
    [SerializeField] private float demonstrationDelay = 0.45f;
    [SerializeField] private float buttonGapDelay = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool autoFindButtons = true;

    private BaboonSequenceState currentState;
    private readonly List<int> sequence = new ();
    private int playerInputIndex;

    private void Start()
    {
        if (autoFindButtons && (sequenceButtons == null || sequenceButtons.Length == 0))
            sequenceButtons = FindObjectsByType<BaboonSequenceButton>(FindObjectsSortMode.None);

        InitializeButtons();
        StartMinigame();
    }

    public void StartMinigame()
    {
        currentState = BaboonSequenceState.Demonstrating;
        playerInputIndex = 0;

        GenerateSequence();

        SetInstruction("Watch the baboon!");
        SetFeedback("");

        SetButtonsInteractable(false);

        StartCoroutine(PlaySequence());
    }

    private void InitializeButtons()
    {
        for (int i = 0; i < sequenceButtons.Length; i++)
        {
            if (sequenceButtons[i] != null)
                sequenceButtons[i].Initialize(this, i);
        }
    }
    private void GenerateSequence()
    {
        sequence.Clear();

        int buttonCount = sequenceButtons.Length;

        for (int i = 0; i < buttonCount; i++)
        {
            sequence.Add(i);
        }

        for (int i = 0; i < sequence.Count; i++)
        {
            int randomIndex = Random.Range(i, sequence.Count);

            int temp = sequence[i];
            sequence[i] = sequence[randomIndex];
            sequence[randomIndex] = temp;
        }

        sequenceLength = buttonCount;
    }

    private IEnumerator PlaySequence()
    {
        currentState = BaboonSequenceState.Demonstrating;

        SetButtonsInteractable(false);
        SetInstruction("Watch the baboon!");
        SetFeedback("");

        yield return new WaitForSeconds(demonstrationDelay);

        for (int i = 0; i < sequence.Count; i++)
        {
            int buttonIndex = sequence[i];

            if (buttonIndex >= 0 && buttonIndex < sequenceButtons.Length && sequenceButtons[buttonIndex] != null)
            {
                yield return StartCoroutine(sequenceButtons[buttonIndex].Flash());
                yield return StartCoroutine(BaboonPressReaction());
                yield return new WaitForSeconds(buttonGapDelay);
            }
        }

        playerInputIndex = 0;
        currentState = BaboonSequenceState.PlayerTurn;

        SetInstruction("Your turn!");
        SetFeedback("");

        SetButtonsInteractable(true);
    }

    public void NotifyButtonPressed(BaboonSequenceButton pressedButton)
    {
        if (currentState != BaboonSequenceState.PlayerTurn)
            return;

        if (pressedButton == null)
            return;

        StartCoroutine(HandlePlayerPress(pressedButton));
    }

    private IEnumerator HandlePlayerPress(BaboonSequenceButton pressedButton)
    {
        currentState = BaboonSequenceState.Feedback;
        SetButtonsInteractable(false);

        yield return StartCoroutine(pressedButton.Flash());

        int expectedButtonIndex = sequence[playerInputIndex];

        if (pressedButton.ButtonIndex == expectedButtonIndex)
        {
            playerInputIndex++;
            SetFeedback("Good!");

            yield return StartCoroutine(BaboonHappyReaction());

            if (playerInputIndex >= sequence.Count)
            {
                CompleteMinigame();
                yield break;
            }

            currentState = BaboonSequenceState.PlayerTurn;
            SetButtonsInteractable(true);
            SetFeedback("");
        }
        else
        {
            SetFeedback("Try again! Watch carefully.");
            yield return StartCoroutine(BaboonWrongReaction());
            yield return new WaitForSeconds(0.6f);

            StartCoroutine(PlaySequence());
        }
    }

    private void CompleteMinigame()
    {
        currentState = BaboonSequenceState.Complete;

        SetButtonsInteractable(false);

        SetInstruction("");
        SetFeedback("Baboon sequence complete!");

        StartCoroutine(BaboonCelebrate());

        Debug.Log("[BaboonSequence] Minigame complete. Reward system connects here.");
    }

    private IEnumerator BaboonPressReaction()
    {
        if (baboonVisual == null)
            yield break;

        Vector3 baseScale = baboonVisual.localScale;
        baboonVisual.localScale = baseScale * 1.05f;
        yield return new WaitForSeconds(0.12f);
        baboonVisual.localScale = baseScale;
    }

    private IEnumerator BaboonHappyReaction()
    {
        if (baboonVisual == null)
            yield break;

        Vector3 baseScale = baboonVisual.localScale;
        baboonVisual.localScale = baseScale * 1.08f;
        yield return new WaitForSeconds(0.16f);
        baboonVisual.localScale = baseScale;
    }

    private IEnumerator BaboonWrongReaction()
    {
        if (baboonVisual == null)
            yield break;

        Vector3 basePos = baboonVisual.localPosition;
        float t = 0f;

        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float x = Mathf.Sin(t * 35f) * 0.08f;
            baboonVisual.localPosition = basePos + new Vector3(x, 0f, 0f);
            yield return null;
        }

        baboonVisual.localPosition = basePos;
    }

    private IEnumerator BaboonCelebrate()
    {
        if (baboonVisual == null)
            yield break;

        Vector3 baseScale = baboonVisual.localScale;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t * 25f) * 0.1f;
            baboonVisual.localScale = baseScale * s;
            yield return null;
        }

        baboonVisual.localScale = baseScale;
    }

    private void SetButtonsInteractable(bool value)
    {
        foreach (var sequenceButton in sequenceButtons)
        {
            if (sequenceButton != null)
                sequenceButton.SetInteractable(value);
        }
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