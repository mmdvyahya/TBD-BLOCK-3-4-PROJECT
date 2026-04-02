using UnityEngine;
using TMPro;

public class PrototypeGameManager : MonoBehaviour
{
    public static PrototypeGameManager Instance;

    [Header("UI")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private GameObject buildButton;
    [SerializeField] private GameObject interactButton;
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private GameObject nextGoalPanel;
    [SerializeField] private GameObject brochurePanel;
    [SerializeField] private TMP_Text brochureText;

    [Header("World")]
    [SerializeField] private HabitatPrototype starterHabitat;

    [Header("Minigame")]
    [SerializeField] private BeaverBalanceMinigame beaverBalanceMinigame;

    [Header("Crate Unlock")]
    [SerializeField] private CrateUnlockMinigame crateUnlockMinigame;

    private int coins = 0;

    private enum GameState
    {
        Intro,
        CrateUnlock,
        WaitingForBuild,
        WaitingForCoin,
        WaitingForInteract,
        InMinigame,
        Reward,
        Brochure,
        Done
    }

    private GameState currentState;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Debug.Log("GameManager Start");

        if (buildButton != null) buildButton.SetActive(false);
        if (interactButton != null) interactButton.SetActive(false);
        if (minigamePanel != null) minigamePanel.SetActive(false);
        if (rewardPanel != null) rewardPanel.SetActive(false);
        if (nextGoalPanel != null) nextGoalPanel.SetActive(false);
        if (brochurePanel != null) brochurePanel.SetActive(false);

        UpdateCoinUI();
        StartIntro();
    }

    private void StartIntro()
    {
        currentState = GameState.Intro;
        SetDialogue("Welcome, Junior Explorer! A new animal has just arrived!");
        Invoke(nameof(StartCrateUnlockStep), 1.5f);
    }

    private void StartCrateUnlockStep()
    {
        Debug.Log("StartCrateUnlockStep");
        currentState = GameState.CrateUnlock;
        SetDialogue("Let's open the animal crate!");

        if (crateUnlockMinigame != null)
        {
            crateUnlockMinigame.OpenCrateSequence();
        }
        else
        {
            ShowBuildStep();
        }
    }

    public void NotifyCrateUnlockComplete()
    {
        Debug.Log("NotifyCrateUnlockComplete");

        if (currentState != GameState.CrateUnlock) return;

        SetDialogue("Great! Now let's build a habitat for the new animal.");
        Invoke(nameof(ShowBuildStep), 1f);
    }

    private void ShowBuildStep()
    {
        Debug.Log("ShowBuildStep");
        currentState = GameState.WaitingForBuild;
        SetDialogue("Tap Build Habitat to prepare the enclosure!");

        if (buildButton != null)
            buildButton.SetActive(true);
    }

    public void PressBuildHabitat()
    {
        Debug.Log("PressBuildHabitat");

        if (currentState != GameState.WaitingForBuild) return;

        if (buildButton != null)
            buildButton.SetActive(false);

        SetDialogue("Animals help the zoo by earning coins!");
        currentState = GameState.WaitingForCoin;

        if (starterHabitat != null)
            starterHabitat.BuildHabitat();
    }

    public void NotifyFirstCoinCollected(int amount)
    {
        Debug.Log("NotifyFirstCoinCollected");

        if (currentState != GameState.WaitingForCoin) return;

        AddCoins(amount);

        SetDialogue("Let's check on the beaver!");
        if (interactButton != null)
            interactButton.SetActive(true);

        if (starterHabitat != null)
            starterHabitat.SetHighlight(true);

        currentState = GameState.WaitingForInteract;
    }

    public void NotifyExtraCoinCollected(int amount)
    {
        Debug.Log("NotifyExtraCoinCollected");
        AddCoins(amount);
    }

    public void PressInteract()
    {
        Debug.Log("PressInteract");

        if (currentState != GameState.WaitingForInteract) return;

        if (interactButton != null)
            interactButton.SetActive(false);

        if (starterHabitat != null)
            starterHabitat.SetHighlight(false);

        SetDialogue("Help the beaver balance the stick!");

        if (minigamePanel != null)
            minigamePanel.SetActive(true);

        if (beaverBalanceMinigame != null)
            beaverBalanceMinigame.OpenMinigame();
        else
            Debug.LogError("beaverBalanceMinigame is not assigned in GameManager!");

        currentState = GameState.InMinigame;
    }

    public void NotifyMinigameComplete()
    {
        Debug.Log("NotifyMinigameComplete");

        if (currentState != GameState.InMinigame) return;

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (rewardPanel != null)
            rewardPanel.SetActive(true);

        if (rewardText != null)
            rewardText.text = "Reward Unlocked!\n\n+ Habitat Upgrade\n+ Production Boost\n+ Explorer Badge";

        SetDialogue("Great job!");
        currentState = GameState.Reward;
    }

    public void PressRewardContinue()
    {
        Debug.Log("PressRewardContinue");

        if (currentState != GameState.Reward) return;

        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        if (brochurePanel != null && brochureText != null)
        {
            brochurePanel.SetActive(true);
            brochureText.text = "Beavers use sticks, mud, and branches to build dams and lodges.";
            SetDialogue("You unlocked an animal brochure!");
            currentState = GameState.Brochure;
        }
        else
        {
            ShowNextGoal();
        }
    }

    public void PressBrochureContinue()
    {
        Debug.Log("PressBrochureContinue");

        if (currentState != GameState.Brochure) return;

        if (brochurePanel != null)
            brochurePanel.SetActive(false);

        ShowNextGoal();
    }

    private void ShowNextGoal()
    {
        Debug.Log("ShowNextGoal");

        if (nextGoalPanel != null)
            nextGoalPanel.SetActive(true);

        SetDialogue("Great job! Save coins to unlock the next habitat.");
        currentState = GameState.Done;
    }

    private void AddCoins(int amount)
    {
        coins += amount;
        UpdateCoinUI();
    }

    private void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = "Coins: " + coins;
    }

    private void SetDialogue(string text)
    {
        if (dialogueText != null)
            dialogueText.text = text;
    }
}