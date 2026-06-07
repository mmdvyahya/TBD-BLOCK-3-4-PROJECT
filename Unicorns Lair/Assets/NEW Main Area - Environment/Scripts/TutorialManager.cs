using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    public static event System.Action UnlockChanged;

    enum SubState
    {
        WaitingForBuy = 0,
        Building = 1,
        WaitingForTap = 2,
        WaitingForInspect = 3,
        Inspecting = 4,
        WaitingForBack = 5,
        WaitingForMinigame = 6,
        WaitingForMinigameReturn = 7,
        Complete = 99
    }

    [Header("Habitat Build Order")]
    [SerializeField]
    private string[] habitatOrder =
    {
        "beaver_habitat","polarbear_habitat","racoon_habitat","prairiedog_habitat","baboon_habitat","hippo_habitat",
    };
    [Tooltip("The build order is fully randomized each new playthrough. The shuffled order is saved so it stays consistent if the game is reopened mid-tutorial.")]
    [SerializeField] private bool randomizeBuildOrder = true;
    [Tooltip("After the FIRST habitat, the tutorial skips the inspect/minigame steps. The next build button simply appears once the player has at least this many coins.")]
    [SerializeField] private int coinsToUnlockNext = 100;
    [Header("Mini Usability Test")]
    [SerializeField] private bool showMiniTestAfterTutorial = true;

    [Header("Random Animal Facts")]
    [Tooltip("After the tutorial is complete, the zookeeper pops in with a random fact now and then.")]
    [SerializeField] private bool enableRandomFacts = true;
    [Tooltip("Seconds to wait before the first random fact (and after each return to the zoo).")]
    [SerializeField] private float firstFactDelay = 25f;
    [SerializeField] private float factMinInterval = 35f;
    [SerializeField] private float factMaxInterval = 70f;

    [Header("Welcome Back")]
    [Tooltip("When the game is reopened with an existing save, the zookeeper greets the player with a random welcome-back line.")]
    [SerializeField] private bool enableWelcomeBack = true;

    private static bool _welcomedThisSession;

    private GameObject _miniTestOverlay;
    private int _miniTestQuestionIndex;
    private int[] _miniTestAnswers;
    [Tooltip("Habitats listed here stay locked (no buy button) for the entire tutorial. They unlock once the tutorial completes. Use this for habitats present in the scene that should not be part of the guided sequence.")]
    [SerializeField] private string[] tutorialSkipHabitats;

    [Header("Visuals")]
    [Tooltip("Optional. Sprite shown hovering above the habitat when player needs to tap on it.")]
    [SerializeField] private Sprite tapIcon;
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private float glowGroundOffset = 0.05f;

    [Header("Tutorial Pointer (Buy/Build marker)")]
    [Tooltip("PNG used as the bouncing marker over the buy/build button. Replaces the yellow triangle. If empty, the triangle is shown.")]
    [SerializeField] private Sprite pointerSprite;
    [Tooltip("Size of the pointer marker in reference pixels.")]
    [SerializeField] private Vector2 pointerSize = new Vector2(140f, 140f);
    [Tooltip("How far above the target the pointer floats.")]
    [SerializeField] private float pointerYOffset = 110f;
    [Tooltip("How much the pointer bobs up and down.")]
    [SerializeField] private float pointerBobAmount = 18f;
    [Tooltip("How much the pointer pulses in size (0 = no pulse).")]
    [SerializeField] private float pointerPulseAmount = 0.08f;
    [SerializeField] private float inspectionPromptSeconds = 5f;

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    [System.Serializable]
    public struct DialogueLine
    {
        public string localizationKey;
        [TextArea(2, 4)] public string fallbackText;
    }

    [Header("Intro Dialogue")]
    [SerializeField] private string speakerNameKey = "intro_speaker";
    [SerializeField] private string speakerNameFallback = "Boswachter";

    [Header("Dialogue Box Background")]
    [Tooltip("Optional PNG used as the dialogue box background. If set, the speaker name tag and accent strips are hidden (your PNG provides them). If empty, the generated box is used.")]
    [SerializeField] private Sprite dialogueBoxSprite;
    [Tooltip("Size of the dialogue box in reference pixels. Match this to your PNG's aspect ratio so it isn't stretched.")]
    [SerializeField] private Vector2 dialogueBoxSize = new Vector2(1000f, 480f);
    [Tooltip("Transparency of the PNG background. 1 = fully opaque, 0 = fully transparent.")]
    [Range(0f, 1f)]
    [SerializeField] private float dialogueBoxOpacity = 1f;

    [Header("Dialogue Text Layout")]
    [Tooltip("Padding from the LEFT edge of the box to where text starts.")]
    [SerializeField] private float textPadLeft = 48f;
    [Tooltip("Padding from the RIGHT edge of the box to where text ends.")]
    [SerializeField] private float textPadRight = 48f;
    [Tooltip("Padding from the TOP of the box. Increase this to lower the text.")]
    [SerializeField] private float textPadTop = 50f;
    [Tooltip("Padding from the BOTTOM of the box.")]
    [SerializeField] private float textPadBottom = 90f;

    [Header("Tap-To-Continue Indicator")]
    [Tooltip("Anchor/pivot inside the box (0,0 = bottom-left, 1,1 = top-right, 0.5,0 = bottom-center).")]
    [SerializeField] private Vector2 tapIndicatorAnchor = new Vector2(1f, 0f);
    [Tooltip("Offset from that anchor, in reference pixels.")]
    [SerializeField] private Vector2 tapIndicatorPosition = new Vector2(-30f, 18f);
    [SerializeField] private Vector2 tapIndicatorSize = new Vector2(280f, 44f);
    [SerializeField] private TextAnchor tapIndicatorAlignment = TextAnchor.MiddleRight;
    [Tooltip("Characters per second typed out.")]
    [SerializeField] private float charsPerSecond = 38f;
    [Tooltip("Shared voice sequence asset that plays on every new intro dialogue line.")]
    [Header("Dialogue Voice Lines")]
    [SerializeField] private LocalizedSoundData introDialogueLocalized;
    [SerializeField] private LocalizedSoundData welcomeBackLocalized;
    [SerializeField] private LocalizedSoundData tutorialCompleteLocalized;
    [SerializeField] private LocalizedSoundData randomFactLocalized;
    private SoundData _activeVoiceSound;
    private int _activeVoiceClipIndex = -1;
    private SoundData _activeTutorialVoice;  // Track currently playing tutorial voice

    [SerializeField]
    private DialogueLine[] introDialogue = new DialogueLine[]
    {
        new DialogueLine { localizationKey = "intro_dlg_0", fallbackText = "Hé daar, kleine ontdekker!" },
        new DialogueLine { localizationKey = "intro_dlg_1", fallbackText = "Welkom in Wildlands... gelukkig ben jij er!" },
        new DialogueLine { localizationKey = "intro_dlg_2", fallbackText = "Oh nee, we hebben jouw hulp echt nodig!" },
        new DialogueLine { localizationKey = "intro_dlg_3", fallbackText = "De dieren hebben nog geen plek om te wonen..." },
        new DialogueLine { localizationKey = "intro_dlg_4", fallbackText = "Help jij ons om een dierentuin voor ze te bouwen?" },
        new DialogueLine { localizationKey = "intro_dlg_5", fallbackText = "Speel leuke minigames met de dieren om munten te verdienen!" },
        new DialogueLine { localizationKey = "intro_dlg_6", fallbackText = "Bouw met die munten gloednieuwe verblijven!" },
        new DialogueLine { localizationKey = "intro_dlg_7", fallbackText = "Klaar om de allerbeste dierenheld te worden? Tik om te beginnen!" },
    };

    private bool _introActive;
    private int _dialogueIndex;
    private bool _typing;
    private bool _skipTyping;
    private GameObject _dialogueOverlay;
    private GameObject _dialogueBox;
    private Text _dialogueText;
    private Text _dialogueSpeaker;
    private GameObject _continueIndicator;
    private Coroutine _typeCoroutine;
    private Coroutine _indicatorPulseCoroutine;
    private DialogueLine[] _activeDialogue;
    private System.Action _onDialogueComplete;

    private int _currentStep;
    private bool _hasSeenIntro;
    private bool _tutorialFinished;
    private SubState _sub;
    private float _inspectTimer;
    private float _factTimer = -1f;
    private bool _cardOpen;
    private DialogueLine[] _factBank;

    private Canvas _tutorialCanvas;
    private GameObject _bannerObj;
    private Text _bannerText;
    private string _lastInstructionToken;
    private GameObject _pointerObj;
    private RectTransform _pointerRt;
    private Image _pointerImage;
    private Image _pointerMarkerImage;
    private Text _pointerFallbackText;
    private Transform _pointerTarget;
    private GameObject _habitatGlowObj;
    private Material _glowMat;
    private Vector3 _originalGlowScale = Vector3.one;

    private Button _pulsingCardBack;
    private Button _pulsingCardInspect;
    private Button _pulsingCardMinigame;
    private Button _pulsingInspectBack;
    private Coroutine _pulseCoroutine;

    const string PREF_STEP = "tutorial_step";
    const string PREF_INTRO = "tutorial_intro_seen";
    const string PREF_SUB = "tutorial_substate";
    const string PREF_COINS = "tutorial_coins_before_minigame";
    const string PREF_ORDER = "tutorial_order";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        LanguageManager.Ensure();
        GameStateManager.Ensure();

        LoadOrShuffleOrder();

        _currentStep = PlayerPrefs.GetInt(PREF_STEP, 0);
        _hasSeenIntro = PlayerPrefs.GetInt(PREF_INTRO, 0) == 1;
        _sub = (SubState)PlayerPrefs.GetInt(PREF_SUB, (int)SubState.WaitingForBuy);
        _tutorialFinished = _currentStep >= habitatOrder.Length;

        BuildTutorialCanvas();
        ApplyUnlocks();

        GameStateManager.Instance.ItemBuilt += OnItemBuilt;
        GameStateManager.Instance.ItemBought += OnItemBought;
        GameStateManager.Instance.CoinsChanged += OnCoinsChanged;
        LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
        HabitatInteractionController.CardShown += OnCardShown;
        HabitatInteractionController.CardClosed += OnCardClosed;
        HabitatInteractionController.InspectBackButtonShown += OnInspectBackShown;
        HabitatInteractionController.MinigamePressed += OnMinigamePressed;

        if (!_hasSeenIntro)
        {
            _introActive = true;
            ApplyUnlocks();
            HidePointer();
            HideBanner();
            ShowDialogue(introDialogue, OnIntroDialogueComplete, VoiceLocalizer.Resolve(introDialogueLocalized));
        }
        else
        {
            if (enableWelcomeBack && !_welcomedThisSession)
            {
                _welcomedThisSession = true;
                HidePointer();
                HideBanner();
                var options = GetWelcomeBackDialogue();
                int index = Random.Range(0, options.Length);
                ShowDialogue(new DialogueLine[] { options[index] }, ResumeFromState, VoiceLocalizer.Resolve(welcomeBackLocalized), index);
            }
            else ResumeFromState();
        }

        if (_tutorialFinished && enableRandomFacts) _factTimer = firstFactDelay;
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null) GameStateManager.Instance.ItemBuilt -= OnItemBuilt;
        if (GameStateManager.Instance != null) GameStateManager.Instance.ItemBought -= OnItemBought;
        if (GameStateManager.Instance != null) GameStateManager.Instance.CoinsChanged -= OnCoinsChanged;
        if (LanguageManager.Instance != null) LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
        HabitatInteractionController.CardShown -= OnCardShown;
        HabitatInteractionController.CardClosed -= OnCardClosed;
        HabitatInteractionController.InspectBackButtonShown -= OnInspectBackShown;
        HabitatInteractionController.MinigamePressed -= OnMinigamePressed;
        if (Instance == this) Instance = null;
    }

    void LoadOrShuffleOrder()
    {
        if (!randomizeBuildOrder) return;
        if (habitatOrder == null || habitatOrder.Length == 0) return;

        string saved = PlayerPrefs.GetString(PREF_ORDER, "");
        if (!string.IsNullOrEmpty(saved))
        {
            var parts = saved.Split(',');
            if (parts.Length == habitatOrder.Length && SameSet(parts, habitatOrder))
            {
                habitatOrder = parts;
                return;
            }
        }

        // Fisher-Yates shuffle of a copy, then persist so the order is stable across reloads.
        var arr = (string[])habitatOrder.Clone();
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        habitatOrder = arr;
        PlayerPrefs.SetString(PREF_ORDER, string.Join(",", habitatOrder));
        PlayerPrefs.Save();
    }

    static bool SameSet(string[] a, string[] b)
    {
        if (a.Length != b.Length) return false;
        var listB = new System.Collections.Generic.List<string>(b);
        foreach (var s in a)
        {
            if (!listB.Remove(s)) return false;
        }
        return listB.Count == 0;
    }

    bool HasCoinsForNext()
    {
        return GameStateManager.Instance != null && GameStateManager.Instance.Coins >= coinsToUnlockNext;
    }

    void OnCoinsChanged(int amount)
    {
        if (_tutorialFinished) return;
        if (_currentStep == 0) return;                 // first habitat uses the full guided flow
        if (_sub != SubState.WaitingForBuy) return;    // only refresh while waiting to build the next one
        ApplyUnlocks();                                // show/hide the buy button for the new coin total
        EnterSubState(SubState.WaitingForBuy);         // refresh pointer + banner
    }

    public bool IsHabitatUnlocked(string habitatId)
    {
        if (_introActive) return false;
        if (_tutorialFinished) return true;
        if (tutorialSkipHabitats != null && System.Array.IndexOf(tutorialSkipHabitats, habitatId) >= 0)
            return false;
        int idx = System.Array.IndexOf(habitatOrder, habitatId);
        if (idx < 0) return true;
        if (idx < _currentStep) return true;   // already built / past steps stay unlocked
        if (idx > _currentStep) return false;  // future steps locked
        // current step:
        if (_currentStep == 0) return true;    // first habitat is always available
        return HasCoinsForNext();              // later habitats unlock only once the player has enough coins
    }

    void ApplyUnlocks() => UnlockChanged?.Invoke();

    void ResumeFromState()
    {
        if (_tutorialFinished) { HidePointer(); HideBanner(); HideGlow(); return; }

        if (_sub == SubState.WaitingForMinigameReturn)
        {
            int coinsBefore = PlayerPrefs.GetInt(PREF_COINS, GameStateManager.Instance.Coins);
            if (GameStateManager.Instance.Coins > coinsBefore) { AdvanceToNextHabitatStep(); return; }
            EnterSubState(SubState.WaitingForTap);
            return;
        }

        // Habitats after the first skip the tap/inspect/minigame steps entirely.
        if (_currentStep >= 1)
        {
            bool built = GameStateManager.Instance.IsBuilt(habitatOrder[_currentStep]);
            if (_sub == SubState.Building && !built) EnterSubState(SubState.Building);
            else if (_sub == SubState.Building && built) AdvanceToNextHabitatStep();
            else EnterSubState(SubState.WaitingForBuy);
            return;
        }

        switch (_sub)
        {
            case SubState.WaitingForBuy: EnterSubState(SubState.WaitingForBuy); break;
            case SubState.Building:
                if (GameStateManager.Instance.IsBuilt(habitatOrder[_currentStep]))
                    EnterSubState(SubState.WaitingForTap);
                else
                    EnterSubState(SubState.Building);
                break;
            case SubState.WaitingForTap: EnterSubState(SubState.WaitingForTap); break;
            default: EnterSubState(SubState.WaitingForTap); break;
        }
    }

    [Header("Tutorial Voice Lines")]
    [SerializeField] private LocalizedSoundData tutorialWaitingForBuyLocalizedFirst;
    [SerializeField] private LocalizedSoundData tutorialWaitingForBuyLocalizedSecond;
    [SerializeField] private LocalizedSoundData tutorialBuildingLocalized;
    [SerializeField] private LocalizedSoundData tutorialWaitingForTapLocalized;
    [SerializeField] private LocalizedSoundData tutorialWaitingForInspectLocalized;
    [SerializeField] private LocalizedSoundData tutorialInspectingLocalized;
    [SerializeField] private LocalizedSoundData tutorialPressBackLocalized;
    [SerializeField] private LocalizedSoundData tutorialPressMinigameLocalized;

    void StopTutorialVoice()
    {
        if (_activeTutorialVoice != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.FadeOut(_activeTutorialVoice);
            _activeTutorialVoice = null;
        }
    }

    void PlayTutorialVoice(LocalizedSoundData localized)
    {
        StopTutorialVoice();
        if (localized == null) return;
        _activeTutorialVoice = VoiceLocalizer.Resolve(localized);
        VoiceLocalizer.PlayLocalized(localized);
    }

    void EnterSubState(SubState next)
    {
        _sub = next;
        PlayerPrefs.SetInt(PREF_SUB, (int)_sub);
        PlayerPrefs.Save();

        StopPulsing();
        HideGlow();
        _pointerTarget = null;

        if (_tutorialFinished || _currentStep >= habitatOrder.Length) { HidePointer(); HideBanner(); return; }

        string currentId = habitatOrder[_currentStep];
        var habitat = FindHabitat(currentId);


        Debug.Log("Entering tutorial substate: " + next);
        Debug.Log("currentstep: " + _currentStep);
switch (next)
        {
            case SubState.WaitingForBuy:
                if (_currentStep == 0)
                {
                    if (habitat != null) _pointerTarget = habitat.GetButtonAnchor();
                    ShowPointer(useTapIcon: false);
                    ShowInstruction("tutorial_first_buy", "Tik op de groene knop om je allereerste verblijf te bouwen!", tutorialWaitingForBuyLocalizedFirst);
                }
                else if (HasCoinsForNext())
                {
                    if (habitat != null) _pointerTarget = habitat.GetButtonAnchor();
                    ShowPointer(useTapIcon: false);
                    ShowInstruction("tutorial_next_buy", "Geweldig! Laten we een huis bouwen voor het volgende dier!", tutorialWaitingForBuyLocalizedSecond);
                }
                // No voice line for this one
                else
                {
                    HidePointer();
                    ShowInstruction("tutorial_earn_coins", "Speel minigames om munten te verdienen. Als je er genoeg hebt, kun je het volgende verblijf bouwen!");
                }
                break;

            case SubState.Building:
                HidePointer();
                ShowInstruction("tutorial_building", "Daar komt 'ie... je verblijf wordt gebouwd!", tutorialBuildingLocalized);
                break;

            case SubState.WaitingForTap:
                if (habitat != null)
                {
                    _pointerTarget = habitat.GetButtonAnchor();
                    SpawnGlowAroundHabitat(habitat);
                }
                ShowPointer(useTapIcon: true);
                ShowInstruction("tutorial_tap_habitat", "Tik nu op het verblijf om je nieuwe dier te ontmoeten!", tutorialWaitingForTapLocalized);
                break;

            case SubState.WaitingForInspect:
                HidePointer();
                ShowInstruction("tutorial_press_inspect", "Druk op de knop Inspecteren om alles van dichtbij te bekijken!", tutorialWaitingForInspectLocalized);
                StartPulsing(_pulsingCardInspect);
                break;

            case SubState.Inspecting:
                HidePointer();
                ShowInstruction("tutorial_inspecting", "Kijk maar goed rond! Kantel je tablet om overal naar te kijken.", tutorialInspectingLocalized);
                _inspectTimer = inspectionPromptSeconds;
                break;

            case SubState.WaitingForBack:
                HidePointer();
                ShowInstruction("tutorial_press_back", "Klaar met kijken? Druk op de knop Terug om verder te gaan!", tutorialPressBackLocalized);
                StartPulsing(_pulsingInspectBack);
                break;

            case SubState.WaitingForMinigame:
                HidePointer();
                ShowInstruction("tutorial_press_minigame", "Speel nu de minigame om munten te verdienen!", tutorialPressMinigameLocalized);
                StartPulsing(_pulsingCardMinigame);
                break;
        }
    }

    void OnItemBought(string itemId)
    {
        if (_tutorialFinished) return;
        if (_currentStep >= habitatOrder.Length) return;
        if (habitatOrder[_currentStep] != itemId) return;
        if (_sub != SubState.WaitingForBuy) return;
        EnterSubState(SubState.Building);
    }

    void OnItemBuilt(string itemId)
    {
        if (_tutorialFinished) return;
        if (_currentStep >= habitatOrder.Length) return;
        if (habitatOrder[_currentStep] != itemId) return;
        if (_currentStep == 0)
            EnterSubState(SubState.WaitingForTap);   // first habitat: full guided flow
        else
            AdvanceToNextHabitatStep();              // later habitats: no tap/inspect/minigame
    }

    void OnCardShown(Button back, Button inspect, Button minigame, InspectableHabitat habitat)
    {
        _cardOpen = true;
        _pulsingCardBack = back;
        _pulsingCardInspect = inspect;
        _pulsingCardMinigame = minigame;

        if (_sub == SubState.WaitingForTap)
        {
            var dlg = (_currentStep < habitatOrder.Length) ? GetAnimalDialogue(habitatOrder[_currentStep]) : null;
            if (dlg != null && dlg.Length > 0)
            {
                HideBanner();
                HidePointer();
                HideGlow();
                StopPulsing();
                SoundData animalSound = GetAnimalSoundData(habitatOrder[_currentStep]);
                ShowDialogue(dlg, () => EnterSubState(SubState.WaitingForInspect), animalSound);
            }
            else EnterSubState(SubState.WaitingForInspect);
        }
        else if (_sub == SubState.WaitingForBack) EnterSubState(SubState.WaitingForMinigame);
    }

    void OnCardClosed()
    {
        _cardOpen = false;
        _pulsingCardBack = _pulsingCardInspect = _pulsingCardMinigame = null;
        StopPulsing();
    }

    void OnInspectBackShown(Button back)
    {
        _pulsingInspectBack = back;
        if (_sub == SubState.WaitingForInspect) EnterSubState(SubState.Inspecting);
    }

    void OnMinigamePressed(InspectableHabitat habitat)
    {
        if (_tutorialFinished) return;
        if (_currentStep != 0) return;   // only the first habitat drives the minigame tutorial step
        PlayerPrefs.SetInt(PREF_COINS, GameStateManager.Instance.Coins);
        EnterSubState(SubState.WaitingForMinigameReturn);
    }

    void AdvanceToNextHabitatStep()
    {
        _currentStep++;
        PlayerPrefs.SetInt(PREF_STEP, _currentStep);

        if (_currentStep >= habitatOrder.Length)
        {
            _tutorialFinished = true;

            HidePointer();
            HideGlow();
            HideBanner();
            StopPulsing();

            ApplyUnlocks();

            PlayerPrefs.SetInt(PREF_SUB, (int)SubState.Complete);
            PlayerPrefs.Save();

            ShowDialogue(GetTutorialCompleteDialogue(), OnTutorialCompleteDialogueDone, VoiceLocalizer.Resolve(tutorialCompleteLocalized));

            return;
        }
        ApplyUnlocks();
        EnterSubState(SubState.WaitingForBuy);
    }

    void Update()
    {
        // if(Keyboard.current.tKey.wasPressedThisFrame)
        // {
        //     ShowDialogue(GetTutorialCompleteDialogue(), OnTutorialCompleteDialogueDone, VoiceLocalizer.Resolve(tutorialCompleteLocalized));
        // }

        if (_tutorialFinished && enableRandomFacts && _factTimer > 0f)
        {
            _factTimer -= Time.deltaTime;
            if (_factTimer <= 0f)
            {
                if (CanShowRandomFact()) ShowRandomFact();
                else _factTimer = 5f;
            }
        }

        if (_sub == SubState.Inspecting && _inspectTimer > 0f)
        {
            _inspectTimer -= Time.deltaTime;
            if (_inspectTimer <= 0f) EnterSubState(SubState.WaitingForBack);
        }

        if (_pointerObj != null && _pointerObj.activeSelf && _pointerTarget != null)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 screen = mainCamera.WorldToScreenPoint(_pointerTarget.position);
                if (screen.z <= 0f) _pointerObj.SetActive(false);
                else
                {
                    _pointerObj.SetActive(true);
                    var canvasRt = _tutorialCanvas.GetComponent<RectTransform>();
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out Vector2 local);
                    float bob = Mathf.Sin(Time.time * 4f) * pointerBobAmount;
                    local.y += pointerYOffset + bob;
                    _pointerRt.anchoredPosition = local;
                    float pulse = 1f + Mathf.Sin(Time.time * 5f) * pointerPulseAmount;
                    _pointerRt.localScale = Vector3.one * pulse;
                }
            }
        }

        if (_habitatGlowObj != null && _glowMat != null)
        {
            float a = 0.35f + Mathf.Sin(Time.time * 3f) * 0.25f;
            var c = glowColor; c.a = a;
            _glowMat.color = c;
            if (_glowMat.HasProperty("_BaseColor")) _glowMat.SetColor("_BaseColor", c);
            float s = 1f + Mathf.Sin(Time.time * 3f) * 0.05f;
            _habitatGlowObj.transform.localScale = new Vector3(_originalGlowScale.x * s, _originalGlowScale.y, _originalGlowScale.z * s);
        }
    }

    void SpawnGlowAroundHabitat(Habitat habitat)
    {
        HideGlow();
        if (habitat == null) return;

        Bounds? combined = null;
        var renderers = habitat.GetComponentsInChildren<Renderer>(false);
        foreach (var r in renderers)
        {
            if (combined == null) combined = r.bounds;
            else { var b = combined.Value; b.Encapsulate(r.bounds); combined = b; }
        }
        if (combined == null) return;

        var b2 = combined.Value;
        Vector3 groundCenter = new Vector3(b2.center.x, b2.min.y + glowGroundOffset, b2.center.z);
        float size = Mathf.Max(b2.size.x, b2.size.z) * 1.3f;

        _habitatGlowObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _habitatGlowObj.name = "TutorialHabitatGlow";
        var col = _habitatGlowObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        _habitatGlowObj.transform.position = groundCenter;
        _habitatGlowObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        _habitatGlowObj.transform.localScale = new Vector3(size, size, 1f);
        _originalGlowScale = _habitatGlowObj.transform.localScale;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        _glowMat = new Material(shader);
        var c = glowColor; c.a = 0.45f;
        _glowMat.color = c;
        if (_glowMat.HasProperty("_BaseColor")) _glowMat.SetColor("_BaseColor", c);
        if (_glowMat.HasProperty("_Surface")) _glowMat.SetFloat("_Surface", 1f);
        if (_glowMat.HasProperty("_Blend")) _glowMat.SetFloat("_Blend", 0f);
        _glowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _glowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _glowMat.SetInt("_ZWrite", 0);
        _glowMat.renderQueue = 3000;
        _glowMat.mainTexture = MakeRingTexture(256);
        _habitatGlowObj.GetComponent<Renderer>().material = _glowMat;
    }

    void HideGlow()
    {
        if (_habitatGlowObj != null) Destroy(_habitatGlowObj);
        _habitatGlowObj = null;
        _glowMat = null;
    }

    Texture2D MakeRingTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = size * 0.5f;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / c;
                float dy = (y - c) / c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a;
                if (d > 1f) a = 0f;
                else if (d > 0.85f) a = 1f - (d - 0.85f) / 0.15f;
                else if (d > 0.65f) a = 1f;
                else if (d > 0.45f) a = (d - 0.45f) / 0.2f;
                else a = 0f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    void StartPulsing(Button btn)
    {
        if (btn == null) return;
        StopPulsing();
        _pulseCoroutine = StartCoroutine(PulseButton(btn));
    }

    void StopPulsing()
    {
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = null;
    }

    IEnumerator PulseButton(Button btn)
    {
        if (btn == null) yield break;
        var rt = btn.GetComponent<RectTransform>();
        Vector3 baseScale = rt.localScale;
        while (btn != null && rt != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.10f;
            rt.localScale = baseScale * pulse;
            yield return null;
        }
    }

    Habitat FindHabitat(string id)
    {
        var all = FindObjectsByType<Habitat>(FindObjectsSortMode.None);
        foreach (var h in all) if (h != null && h.HabitatId == id) return h;
        return null;
    }

    void BuildTutorialCanvas()
    {
        var cObj = new GameObject("TutorialCanvas");
        _tutorialCanvas = cObj.AddComponent<Canvas>();
        _tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _tutorialCanvas.sortingOrder = 30;
        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        cObj.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        _bannerObj = new GameObject("Banner");
        _bannerObj.transform.SetParent(cObj.transform, false);
        var brt = _bannerObj.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 1f); brt.anchorMax = new Vector2(0.5f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(0f, -140f);
        brt.sizeDelta = new Vector2(900f, 120f);
        var bImg = _bannerObj.AddComponent<Image>();
        bImg.color = new Color(0.10f, 0.45f, 0.18f, 0.92f);
        bImg.raycastTarget = false;
        var bTxtObj = new GameObject("Text");
        bTxtObj.transform.SetParent(_bannerObj.transform, false);
        var btrt = bTxtObj.AddComponent<RectTransform>();
        btrt.anchorMin = Vector2.zero; btrt.anchorMax = Vector2.one;
        btrt.offsetMin = new Vector2(18f, 6f); btrt.offsetMax = new Vector2(-18f, -6f);
        _bannerText = bTxtObj.AddComponent<Text>();
        _bannerText.font = GetFont(); _bannerText.fontSize = 32; _bannerText.fontStyle = FontStyle.Bold;
        _bannerText.alignment = TextAnchor.MiddleCenter; _bannerText.color = Color.white; _bannerText.raycastTarget = false;
        bTxtObj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);
        _bannerObj.SetActive(false);

        _pointerObj = new GameObject("Pointer");
        _pointerObj.transform.SetParent(cObj.transform, false);
        _pointerRt = _pointerObj.AddComponent<RectTransform>();
        _pointerRt.anchorMin = _pointerRt.anchorMax = _pointerRt.pivot = new Vector2(0.5f, 0.5f);
        _pointerRt.sizeDelta = pointerSize;

        _pointerFallbackText = _pointerObj.AddComponent<Text>();
        _pointerFallbackText.text = "▼";
        _pointerFallbackText.font = GetFont();
        _pointerFallbackText.fontSize = 100; _pointerFallbackText.fontStyle = FontStyle.Bold;
        _pointerFallbackText.alignment = TextAnchor.MiddleCenter;
        _pointerFallbackText.color = glowColor;
        _pointerFallbackText.raycastTarget = false;
        var pOutline = _pointerObj.AddComponent<Outline>();
        pOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        pOutline.effectDistance = new Vector2(3f, -3f);

        if (pointerSprite != null)
        {
            var markerObj = new GameObject("PointerMarkerImage");
            markerObj.transform.SetParent(_pointerObj.transform, false);
            var mrt = markerObj.AddComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
            mrt.offsetMin = mrt.offsetMax = Vector2.zero;
            _pointerMarkerImage = markerObj.AddComponent<Image>();
            _pointerMarkerImage.sprite = pointerSprite;
            _pointerMarkerImage.color = Color.white;
            _pointerMarkerImage.raycastTarget = false;
            _pointerMarkerImage.preserveAspect = true;
            _pointerMarkerImage.enabled = false;
        }

        if (tapIcon != null)
        {
            var iconObj = new GameObject("TapIconImage");
            iconObj.transform.SetParent(_pointerObj.transform, false);
            var irt = iconObj.AddComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = irt.offsetMax = Vector2.zero;
            _pointerImage = iconObj.AddComponent<Image>();
            _pointerImage.sprite = tapIcon;
            _pointerImage.color = Color.white;
            _pointerImage.raycastTarget = false;
            _pointerImage.preserveAspect = true;
            _pointerImage.enabled = false;
        }
        _pointerObj.SetActive(false);
    }

    void ShowPointer(bool useTapIcon)
    {
        if (_pointerObj == null) return;
        _pointerObj.SetActive(true);

        bool showTap = useTapIcon && _pointerImage != null;
        bool showMarker = !useTapIcon && _pointerMarkerImage != null;

        if (_pointerImage != null) _pointerImage.enabled = showTap;
        if (_pointerMarkerImage != null) _pointerMarkerImage.enabled = showMarker;
        if (_pointerFallbackText != null) _pointerFallbackText.enabled = !showTap && !showMarker;
    }

    void HidePointer()
    {
        if (_pointerObj != null) _pointerObj.SetActive(false);
    }

    void ShowDialogue(DialogueLine[] lines, System.Action onComplete, SoundData voiceSound = null, int clipIndex = -1)
    {
        _activeVoiceSound = voiceSound;
        _activeVoiceClipIndex = clipIndex;

        if (lines == null || lines.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        _activeDialogue = lines;
        _onDialogueComplete = onComplete;
        _dialogueIndex = 0;
        _typing = false;
        _skipTyping = false;

        if (_dialogueOverlay != null) Destroy(_dialogueOverlay);

        _dialogueOverlay = new GameObject("IntroDialogueOverlay");
        _dialogueOverlay.transform.SetParent(_tutorialCanvas.transform, false);
        var ort = _dialogueOverlay.AddComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        var dim = _dialogueOverlay.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        var dimBtn = _dialogueOverlay.AddComponent<Button>();
        dimBtn.targetGraphic = dim;
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(OnDialogueTapped);

        _dialogueBox = new GameObject("DialogueBox");
        _dialogueBox.transform.SetParent(_dialogueOverlay.transform, false);
        var brt = _dialogueBox.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0f); brt.anchorMax = new Vector2(0.5f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.anchoredPosition = new Vector2(0f, 100f);
        brt.sizeDelta = dialogueBoxSize;
        var bImg = _dialogueBox.AddComponent<Image>();
        bImg.raycastTarget = false;

        bool usePng = dialogueBoxSprite != null;

        if (usePng)
        {
            bImg.sprite = dialogueBoxSprite;
            bImg.type = Image.Type.Simple;
            bImg.preserveAspect = false;
            bImg.color = new Color(1f, 1f, 1f, dialogueBoxOpacity);
        }
        else
        {
            bImg.color = new Color(0.08f, 0.13f, 0.24f, 0.97f);

            var accentTop = new GameObject("AccentTop");
            accentTop.transform.SetParent(_dialogueBox.transform, false);
            var atRt = accentTop.AddComponent<RectTransform>();
            atRt.anchorMin = new Vector2(0f, 1f); atRt.anchorMax = new Vector2(1f, 1f);
            atRt.pivot = new Vector2(0.5f, 1f); atRt.anchoredPosition = Vector2.zero; atRt.sizeDelta = new Vector2(0f, 10f);
            accentTop.AddComponent<Image>().color = new Color(0.3f, 0.75f, 1f);

            var accentLeft = new GameObject("AccentLeft");
            accentLeft.transform.SetParent(_dialogueBox.transform, false);
            var alRt = accentLeft.AddComponent<RectTransform>();
            alRt.anchorMin = new Vector2(0f, 0f); alRt.anchorMax = new Vector2(0f, 1f);
            alRt.pivot = new Vector2(0f, 0.5f); alRt.anchoredPosition = Vector2.zero; alRt.sizeDelta = new Vector2(8f, 0f);
            accentLeft.AddComponent<Image>().color = new Color(0.3f, 0.75f, 1f);

            var nameTag = new GameObject("SpeakerTag");
            nameTag.transform.SetParent(_dialogueBox.transform, false);
            var ntRt = nameTag.AddComponent<RectTransform>();
            ntRt.anchorMin = new Vector2(0f, 1f); ntRt.anchorMax = new Vector2(0f, 1f);
            ntRt.pivot = new Vector2(0f, 0f);
            ntRt.anchoredPosition = new Vector2(36f, 6f);
            ntRt.sizeDelta = new Vector2(320f, 70f);
            var ntImg = nameTag.AddComponent<Image>();
            ntImg.color = new Color(0.3f, 0.75f, 1f);
            ntImg.raycastTarget = false;

            var nameTxtObj = new GameObject("Text");
            nameTxtObj.transform.SetParent(nameTag.transform, false);
            var nameTxtRt = nameTxtObj.AddComponent<RectTransform>();
            nameTxtRt.anchorMin = Vector2.zero; nameTxtRt.anchorMax = Vector2.one;
            nameTxtRt.offsetMin = new Vector2(18f, 0f); nameTxtRt.offsetMax = new Vector2(-18f, 0f);
            _dialogueSpeaker = nameTxtObj.AddComponent<Text>();
            _dialogueSpeaker.text = SafeGet(speakerNameKey, speakerNameFallback);
            _dialogueSpeaker.font = GetFont();
            _dialogueSpeaker.fontSize = 36;
            _dialogueSpeaker.fontStyle = FontStyle.Bold;
            _dialogueSpeaker.alignment = TextAnchor.MiddleLeft;
            _dialogueSpeaker.color = new Color(0.08f, 0.13f, 0.24f);
            _dialogueSpeaker.raycastTarget = false;
        }

        var txtObj = new GameObject("DialogueText");
        txtObj.transform.SetParent(_dialogueBox.transform, false);
        var txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = new Vector2(0f, 0f); txtRt.anchorMax = new Vector2(1f, 1f);
        txtRt.offsetMin = new Vector2(textPadLeft, textPadBottom);
        txtRt.offsetMax = new Vector2(-textPadRight, -textPadTop);
        _dialogueText = txtObj.AddComponent<Text>();
        _dialogueText.text = "";
        _dialogueText.font = GetFont();
        _dialogueText.fontSize = 42;
        _dialogueText.fontStyle = FontStyle.Normal;
        _dialogueText.alignment = TextAnchor.UpperLeft;
        _dialogueText.color = new Color(0.95f, 0.97f, 1f);
        _dialogueText.raycastTarget = false;
        _dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _dialogueText.verticalOverflow = VerticalWrapMode.Truncate;
        txtObj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);

        _continueIndicator = new GameObject("ContinueIndicator");
        _continueIndicator.transform.SetParent(_dialogueBox.transform, false);
        var ciRt = _continueIndicator.AddComponent<RectTransform>();
        ciRt.anchorMin = ciRt.anchorMax = ciRt.pivot = tapIndicatorAnchor;
        ciRt.anchoredPosition = tapIndicatorPosition;
        ciRt.sizeDelta = tapIndicatorSize;
        var ciTxt = _continueIndicator.AddComponent<Text>();
        ciTxt.text = SafeGet("intro_tap_continue", "Tik om verder ▶");
        ciTxt.font = GetFont(); ciTxt.fontSize = 24; ciTxt.fontStyle = FontStyle.Bold;
        ciTxt.alignment = tapIndicatorAlignment;
        ciTxt.color = new Color(0.3f, 0.75f, 1f);
        ciTxt.raycastTarget = false;
        _continueIndicator.SetActive(false);

        StartCoroutine(SlideInDialogue(brt));
    }

    IEnumerator SlideInDialogue(RectTransform boxRt)
    {
        Vector2 endPos = boxRt.anchoredPosition;
        Vector2 startPos = endPos + new Vector2(0f, -boxRt.sizeDelta.y - 200f);
        boxRt.anchoredPosition = startPos;
        float t = 0f, dur = 0.45f;
        while (t < dur)
        {
            t += Time.deltaTime;
            if (boxRt == null) yield break;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            boxRt.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
            yield return null;
        }
        if (boxRt != null) boxRt.anchoredPosition = endPos;
        StartTypingCurrentLine();
    }

    void StartTypingCurrentLine()
    {
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        PlayVoiceLine(_activeVoiceSound);
        _typeCoroutine = StartCoroutine(TypeLine(_dialogueIndex));
    }

    void PlayVoiceLine(SoundData data)
    {
        if (data == null || SoundManager.Instance == null) return;
        SoundManager.Instance.FadeOut(data);

        if (_activeVoiceClipIndex >= 0)
            SoundManager.Instance.PlayClipAt(data, _activeVoiceClipIndex);
        else
            SoundManager.Instance.Play(data);

        _activeVoiceClipIndex = -1;
    }

    IEnumerator TypeLine(int index)
    {
        if (index < 0 || index >= _activeDialogue.Length) yield break;
        HideContinueIndicator();
        _typing = true;
        _skipTyping = false;

        var line = _activeDialogue[index];
        string content = !string.IsNullOrEmpty(line.localizationKey)
            ? SafeGet(line.localizationKey, line.fallbackText)
            : line.fallbackText;
        if (string.IsNullOrEmpty(content)) content = "";

        _dialogueText.text = "";
        float delay = 1f / Mathf.Max(1f, charsPerSecond);
        for (int i = 0; i < content.Length; i++)
        {
            if (_skipTyping) { _dialogueText.text = content; break; }
            _dialogueText.text = content.Substring(0, i + 1);
            char c = content[i];
            float wait = delay;
            if (c == '.' || c == '!' || c == '?') wait = delay * 7f;
            else if (c == ',' || c == ';' || c == ':') wait = delay * 3f;
            yield return new WaitForSeconds(wait);
        }
        _typing = false;
        _skipTyping = false;
        ShowContinueIndicator();
    }

    void OnDialogueTapped()
    {
        if (_dialogueOverlay == null) return;
        if (_typing) { _skipTyping = true; return; }
        AdvanceDialogue();
    }

    void AdvanceDialogue()
    {
        _dialogueIndex++;
        if (_dialogueIndex >= _activeDialogue.Length)
        {
            StartCoroutine(CloseDialogue());
        }
        else
        {
            StartTypingCurrentLine();
        }
    }

    void ShowContinueIndicator()
    {
        if (_continueIndicator == null) return;
        _continueIndicator.SetActive(true);
        if (_indicatorPulseCoroutine != null) StopCoroutine(_indicatorPulseCoroutine);
        _indicatorPulseCoroutine = StartCoroutine(PulseIndicator());
    }

    void HideContinueIndicator()
    {
        if (_continueIndicator != null) _continueIndicator.SetActive(false);
        if (_indicatorPulseCoroutine != null) { StopCoroutine(_indicatorPulseCoroutine); _indicatorPulseCoroutine = null; }
    }

    IEnumerator PulseIndicator()
    {
        while (_continueIndicator != null && _continueIndicator.activeSelf)
        {
            float t = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(0.92f, 1.08f, t);
            _continueIndicator.transform.localScale = new Vector3(scale, scale, 1f);
            var txt = _continueIndicator.GetComponent<Text>();
            if (txt != null)
            {
                var c = txt.color; c.a = Mathf.Lerp(0.55f, 1f, t); txt.color = c;
            }
            yield return null;
        }
    }

    IEnumerator CloseDialogue()
    {
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        HideContinueIndicator();

        StopTutorialVoice();

        if (_activeVoiceSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.FadeOut(_activeVoiceSound);
            _activeVoiceClipIndex = -1;
        }

        var brt = _dialogueBox != null ? _dialogueBox.GetComponent<RectTransform>() : null;
        if (brt != null)
        {
            Vector2 startPos = brt.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, -brt.sizeDelta.y - 200f);
            float t = 0f, dur = 0.35f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (brt == null) break;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                brt.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
                if (_dialogueOverlay != null)
                {
                    var dim = _dialogueOverlay.GetComponent<Image>();
                    if (dim != null) { var c = dim.color; c.a = Mathf.Lerp(0.55f, 0f, p); dim.color = c; }
                }
                yield return null;
            }
        }

        if (_dialogueOverlay != null) Destroy(_dialogueOverlay);
        _dialogueOverlay = null;
        _dialogueBox = null;
        _dialogueText = null;
        _dialogueSpeaker = null;
        _continueIndicator = null;

        var done = _onDialogueComplete;
        _onDialogueComplete = null;
        _activeDialogue = null;
        done?.Invoke();
    }

    void OnIntroDialogueComplete()
    {
        PlayerPrefs.SetInt(PREF_INTRO, 1);
        PlayerPrefs.Save();
        _hasSeenIntro = true;
        _introActive = false;

        ApplyUnlocks();
        ResumeFromState();
    }

    [Header("Animal Dialogue Voice Lines")]
    [SerializeField] private LocalizedSoundData beaverDialogueLocalized;
    [SerializeField] private LocalizedSoundData polarBearDialogueLocalized;
    [SerializeField] private LocalizedSoundData raccoonDialogueLocalized;
    [SerializeField] private LocalizedSoundData prairieDogDialogueLocalized;
    [SerializeField] private LocalizedSoundData baboonDialogueLocalized;
    [SerializeField] private LocalizedSoundData hippoDialogueLocalized;
    [SerializeField] private LocalizedSoundData parrotDialogueLocalized;
    [SerializeField] private LocalizedSoundData otterDialogueLocalized;

    SoundData GetAnimalSoundData(string habitatId)
    {
        switch (habitatId)
        {
            case "beaver_habitat":
                return VoiceLocalizer.Resolve(beaverDialogueLocalized);
            case "polarbear_habitat":
                return VoiceLocalizer.Resolve(polarBearDialogueLocalized);
            case "racoon_habitat":
                return VoiceLocalizer.Resolve(raccoonDialogueLocalized);
            case "prairiedog_habitat":
                return VoiceLocalizer.Resolve(prairieDogDialogueLocalized);
            case "baboon_habitat":
                return VoiceLocalizer.Resolve(baboonDialogueLocalized);
            case "hippo_habitat":
                return VoiceLocalizer.Resolve(hippoDialogueLocalized);
            case "parrot_habitat":
                return VoiceLocalizer.Resolve(parrotDialogueLocalized);
            case "otter_habitat":
                return VoiceLocalizer.Resolve(otterDialogueLocalized);
            default:
                return null;
        }
    }

    DialogueLine[] GetAnimalDialogue(string habitatId)
    {
        string meetKey, factKey, meetFb, factFb;

        switch (habitatId)
        {
            case "beaver_habitat":
                meetKey = "tutorial_meet_beaver";
                meetFb = "Zeg hallo tegen onze bever! Bevers zijn de echte bouwmeesters van de natuur, net als jij vandaag!";
                factKey = "tutorial_fact_beaver";
                factFb = "De voortanden van een bever blijven altijd groeien, dus knaagt hij de hele dag op hout om ze precies goed te houden.";
                break;
            case "polarbear_habitat":
                meetKey = "tutorial_meet_polarbear";
                meetFb = "Brrr! Maak kennis met onze ijsbeer. Hij houdt meer van de kou dan wie dan ook in de hele dierentuin.";
                factKey = "tutorial_fact_polarbear";
                factFb = "De vacht van een ijsbeer lijkt wit, maar is eigenlijk doorzichtig! Daaronder is zijn huid zwart.";
                break;
            case "racoon_habitat":
                meetKey = "tutorial_meet_raccoon";
                meetFb = "Hier zijn onze slimme wasberen. Let goed op je snacks als die in de buurt zijn!";
                factKey = "tutorial_fact_raccoon";
                factFb = "Wasberen dopen hun eten graag in water voordat ze het opeten, bijna alsof ze het wassen.";
                break;
            case "prairiedog_habitat":
                meetKey = "tutorial_meet_prairiedog";
                meetFb = "Plop! Daar is onze prairiehond. Laat de naam je niet voor de gek houden, het is helemaal geen hond!";
                factKey = "tutorial_fact_prairiedog";
                factFb = "Prairiehonden zijn eigenlijk een soort eekhoorn, en ze wonen in enorme ondergrondse steden vol tunnels.";
                break;
            case "baboon_habitat":
                meetKey = "tutorial_meet_baboon";
                meetFb = "Maak kennis met onze bavianen! Zij zijn een van de slimste dieren in de hele dierentuin.";
                factKey = "tutorial_fact_baboon";
                factFb = "Bavianen leven in grote families die troepen heten, en ze praten met elkaar met geluiden en gekke gezichten.";
                break;
            case "hippo_habitat":
                meetKey = "tutorial_meet_hippo";
                meetFb = "Plons! Daar komt ons nijlpaard. Hij blijft graag lekker koel in het water.";
                factKey = "tutorial_fact_hippo";
                factFb = "Een nijlpaard kan wel vijf hele minuten zijn adem onder water inhouden!";
                break;
            case "parrot_habitat":
                meetKey = "tutorial_meet_parrot";
                meetFb = "En hier is onze papegaai, het kletsende dier van Wildlands!";
                factKey = "tutorial_fact_parrot";
                factFb = "Sommige papegaaien kunnen de woorden die mensen zeggen nadoen. Misschien leren ze zelfs jouw naam!";
                break;
            case "otter_habitat":
                meetKey = "tutorial_meet_otter";
                meetFb = "Hier is onze speelse otter! Otters houden van spetteren, glijden en de hele dag spelen.";
                factKey = "tutorial_fact_otter";
                factFb = "Otters houden elkaars pootjes vast als ze slapen, zodat ze niet van elkaar wegdrijven!";
                break;
            default:
                return null;
        }

        return new DialogueLine[]
        {
            new DialogueLine { localizationKey = meetKey, fallbackText = meetFb },
            new DialogueLine { localizationKey = factKey, fallbackText = factFb },
        };
    }

    DialogueLine[] GetTutorialCompleteDialogue()
    {
        return new DialogueLine[]
        {
            new DialogueLine { localizationKey = "tutorial_done_0", fallbackText = "Het is je gelukt, ontdekker! Nu heeft elk dier een heerlijk thuis." },
            new DialogueLine { localizationKey = "tutorial_done_1", fallbackText = "Wildlands zit vol leven, allemaal dankzij jou. De dieren zullen het nooit vergeten!" },
            new DialogueLine { localizationKey = "tutorial_done_2", fallbackText = "De dierentuin is van jou om van te genieten. Kom je vrienden gerust opzoeken, en blijf nieuwsgierig!" },
        };
    }

    void OnTutorialCompleteDialogueDone()
    {
        if (showMiniTestAfterTutorial)
            StartCoroutine(ShowMiniTestAfterDelay(0.5f));

        if (enableRandomFacts) _factTimer = firstFactDelay;
    }

    DialogueLine[] GetWelcomeBackDialogue()
    {
        return new DialogueLine[]
        {
            new DialogueLine { localizationKey = "welcome_back_0", fallbackText = "Welkom terug, ontdekker! De dieren hebben je gemist." },
            new DialogueLine { localizationKey = "welcome_back_1", fallbackText = "Welkom terug! Kijk eens hoe ver onze dierentuin al is gekomen. Daar mag je trots op zijn!" },
            new DialogueLine { localizationKey = "welcome_back_2", fallbackText = "Welkom terug! Neem rustig de tijd om even rond te kijken." },
            new DialogueLine { localizationKey = "welcome_back_3", fallbackText = "Welkom terug! Wanneer je er klaar voor bent, is er altijd nog een verblijf om te bouwen." },
        };
    }

    bool CanShowRandomFact()
    {
        return _dialogueOverlay == null
            && _miniTestOverlay == null
            && !_cardOpen
            && !_introActive;
    }

    void ScheduleNextFact()
    {
        _factTimer = Random.Range(factMinInterval, factMaxInterval);
    }

    void ShowRandomFact()
    {
        EnsureFactBank();
        if (_factBank == null || _factBank.Length == 0) { _factTimer = factMaxInterval; return; }

        int index = Random.Range(0, _factBank.Length);
        ShowDialogue(new DialogueLine[] { _factBank[index] }, ScheduleNextFact, VoiceLocalizer.Resolve(randomFactLocalized), index);
    }

    void EnsureFactBank()
    {
        if (_factBank != null) return;

        _factBank = new DialogueLine[]
        {
            new DialogueLine { localizationKey = "rfact_beaver_0", fallbackText = "Wist je dat de tanden van een bever oranje zijn? Die kleur komt door ijzer, en dat maakt ze supersterk." },
            new DialogueLine { localizationKey = "rfact_beaver_1", fallbackText = "Wist je dat bevers wel 15 minuten lang hun adem onder water kunnen inhouden? Dat is hartstikke lang!" },
            new DialogueLine { localizationKey = "rfact_beaver_2", fallbackText = "Een bever slaat met zijn platte staart op het water, met een grote PLONS, om zijn familie voor gevaar te waarschuwen." },

            new DialogueLine { localizationKey = "rfact_polarbear_0", fallbackText = "Wist je dat een ijsbeer een zeehond kan ruiken van meer dan een kilometer ver weg? Wat een neus!" },
            new DialogueLine { localizationKey = "rfact_polarbear_1", fallbackText = "IJsberen zijn geweldige zwemmers en kunnen urenlang doorzwemmen zonder te stoppen." },
            new DialogueLine { localizationKey = "rfact_polarbear_2", fallbackText = "Wist je dat een ijsbeerbaby een welp heet? Bij de geboorte is hij ongeveer zo groot als een cavia!" },

            new DialogueLine { localizationKey = "rfact_raccoon_0", fallbackText = "Wist je dat de pootjes van een wasbeer zo handig zijn dat ze bijna als kleine handjes werken?" },
            new DialogueLine { localizationKey = "rfact_raccoon_1", fallbackText = "De donkere vacht rond de ogen van een wasbeer lijkt net een klein maskertje." },
            new DialogueLine { localizationKey = "rfact_raccoon_2", fallbackText = "Wist je dat wasberen meestal 's nachts wakker zijn? Zulke dieren noemen we nachtdieren." },

            new DialogueLine { localizationKey = "rfact_prairiedog_0", fallbackText = "Wist je dat prairiehonden elkaar begroeten met iets dat net op een kusje lijkt?" },
            new DialogueLine { localizationKey = "rfact_prairiedog_1", fallbackText = "Prairiehonden hebben verschillende geluiden voor verschillend gevaar: een blafje voor een havik, een ander voor een coyote." },
            new DialogueLine { localizationKey = "rfact_prairiedog_2", fallbackText = "Wist je dat hun ondergrondse steden wel honderden tunnels kunnen hebben? Net een gigantisch doolhof!" },

            new DialogueLine { localizationKey = "rfact_baboon_0", fallbackText = "Wist je dat bavianen elkaar schoonhouden door voorzichtig door elkaars vacht te kammen?" },
            new DialogueLine { localizationKey = "rfact_baboon_1", fallbackText = "Bavianen zijn echte probleemoplossers en kunnen zelfs simpele puzzels uitvogelen." },
            new DialogueLine { localizationKey = "rfact_baboon_2", fallbackText = "Een baviaan loopt op alle vier zijn poten, maar kan rechtop gaan staan om rond te kijken." },

            new DialogueLine { localizationKey = "rfact_hippo_0", fallbackText = "Wist je dat een nijlpaard zijn eigen roze zonnebrand maakt? Zijn huid geeft een speciale olie af die de zon tegenhoudt." },
            new DialogueLine { localizationKey = "rfact_hippo_1", fallbackText = "Hoewel nijlpaarden enorm groot zijn, kunnen ze verrassend snel rennen op het land." },
            new DialogueLine { localizationKey = "rfact_hippo_2", fallbackText = "Wist je dat een nijlpaardbaby onder water melk kan drinken bij zijn moeder? Wat een knappe truc!" },

            new DialogueLine { localizationKey = "rfact_otter_0", fallbackText = "Wist je dat otters elkaars pootjes vasthouden als ze slapen? Zo drijven ze in het water niet uit elkaar!" },
            new DialogueLine { localizationKey = "rfact_otter_1", fallbackText = "Een otter heeft een speciaal zakje van huid onder zijn arm om zijn lievelingssteentje veilig te bewaren." },
            new DialogueLine { localizationKey = "rfact_otter_2", fallbackText = "Wist je dat otters de dikste vacht van alle dieren hebben? Wel een miljoen haartjes op een klein plekje!" },

            new DialogueLine { localizationKey = "rfact_parrot_0", fallbackText = "Wist je dat sommige papegaaien meer dan 50 jaar oud kunnen worden? Dat is langer dan een hond of een kat!" },
            new DialogueLine { localizationKey = "rfact_parrot_1", fallbackText = "Papegaaien gebruiken hun sterke snavel als gereedschap om harde noten open te kraken." },
            new DialogueLine { localizationKey = "rfact_parrot_2", fallbackText = "Een papegaai heeft twee tenen die naar voren wijzen en twee naar achteren, perfect om aan takken vast te houden." },
        };
    }

    void OnLanguageChanged() { EnterSubState(_sub); }

void ShowInstruction(string key, string fallback, LocalizedSoundData voice = null)
    {
        // Each tutorial part shows its instruction once, in the same PNG dialogue window as the intro.
        // The player can tap it away; it only reappears when the tutorial moves to a different part.
        string token = key + "#" + _currentStep;
        if (token == _lastInstructionToken) return;
        _lastInstructionToken = token;

        if (voice != null) PlayTutorialVoice(voice);

        var line = new DialogueLine { localizationKey = key, fallbackText = fallback };
        ShowDialogue(new DialogueLine[] { line }, null);
    }

    void ShowBanner(string text)
    {
        if (_bannerObj == null) return;
        _bannerObj.SetActive(true);
        _bannerText.text = text;
    }

    void HideBanner()
    {
        if (_bannerObj != null) _bannerObj.SetActive(false);
    }

    IEnumerator HideBannerAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideBanner();
    }
    IEnumerator ShowMiniTestAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowMiniUsabilityTest();
    }

    void ShowMiniUsabilityTest()
    {
        if (_miniTestOverlay != null)
            return;

        _miniTestQuestionIndex = 0;
        _miniTestAnswers = new int[3];

        _miniTestOverlay = new GameObject("MiniUsabilityTestOverlay");
        _miniTestOverlay.transform.SetParent(_tutorialCanvas.transform, false);

        var rt = _miniTestOverlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = _miniTestOverlay.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        ShowMiniTestQuestion();
    }

    void ShowMiniTestQuestion()
    {
        foreach (Transform child in _miniTestOverlay.transform)
            Destroy(child.gameObject);

        string[] questions =
        {
        "Was the game easy to understand?",
        "Was the game fun to play?",
        "Would you like to play more?"
    };

        var card = new GameObject("QuestionCard");
        card.transform.SetParent(_miniTestOverlay.transform, false);

        var cardRt = card.AddComponent<RectTransform>();
        cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = Vector2.zero;
        cardRt.sizeDelta = new Vector2(900f, 620f);

        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.08f, 0.13f, 0.24f, 0.97f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(card.transform, false);

        var titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -35f);
        titleRt.sizeDelta = new Vector2(820f, 80f);

        var title = titleObj.AddComponent<Text>();
        title.font = GetFont();
        title.fontSize = 42;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        title.text = "Question " + (_miniTestQuestionIndex + 1) + " / 3";

        var qObj = new GameObject("QuestionText");
        qObj.transform.SetParent(card.transform, false);

        var qRt = qObj.AddComponent<RectTransform>();
        qRt.anchorMin = new Vector2(0.5f, 1f);
        qRt.anchorMax = new Vector2(0.5f, 1f);
        qRt.pivot = new Vector2(0.5f, 1f);
        qRt.anchoredPosition = new Vector2(0f, -140f);
        qRt.sizeDelta = new Vector2(820f, 160f);

        var qText = qObj.AddComponent<Text>();
        qText.font = GetFont();
        qText.fontSize = 44;
        qText.fontStyle = FontStyle.Bold;
        qText.alignment = TextAnchor.MiddleCenter;
        qText.color = new Color(0.95f, 0.97f, 1f);
        qText.text = questions[_miniTestQuestionIndex];

        for (int i = 1; i <= 5; i++)
        {
            int score = i;

            float x = -320f + (i - 1) * 160f;

            var btn = MakeButton(
                card.transform,
                score.ToString(),
                new Vector2(x, 150f),
                new Vector2(120f, 120f),
                new Color(0.18f, 0.55f, 0.95f)
            );

            btn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            btn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            btn.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);

            btn.onClick.AddListener(() => AnswerMiniTest(score));
        }

        var hintObj = new GameObject("Hint");
        hintObj.transform.SetParent(card.transform, false);

        var hRt = hintObj.AddComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0.5f, 0f);
        hRt.anchorMax = new Vector2(0.5f, 0f);
        hRt.pivot = new Vector2(0.5f, 0f);
        hRt.anchoredPosition = new Vector2(0f, 50f);
        hRt.sizeDelta = new Vector2(820f, 70f);

        var hint = hintObj.AddComponent<Text>();
        hint.font = GetFont();
        hint.fontSize = 28;
        hint.alignment = TextAnchor.MiddleCenter;
        hint.color = new Color(0.75f, 0.88f, 1f);
        hint.text = "1 = not really     5 = very much";
    }

    void AnswerMiniTest(int score)
    {
        _miniTestAnswers[_miniTestQuestionIndex] = score;

        PlaytestLogger.Instance?.LogSUSAnswer(_miniTestQuestionIndex + 1, score);

        _miniTestQuestionIndex++;

        if (_miniTestQuestionIndex >= _miniTestAnswers.Length)
        {
            CompleteMiniUsabilityTest();
        }
        else
        {
            ShowMiniTestQuestion();
        }
    }

    void CompleteMiniUsabilityTest()
    {
        int rawScore = 0;

        for (int i = 0; i < _miniTestAnswers.Length; i++)
            rawScore += _miniTestAnswers[i];

        float averageScore = rawScore / 3f;
        float percentageScore = averageScore * 20f;

        PlaytestLogger.Instance?.LogSUSScore(rawScore, percentageScore);
        PlaytestLogger.Instance?.EndSessionProperly("Completed mini usability test after tutorial");

        if (_miniTestOverlay != null)
            Destroy(_miniTestOverlay);

        _miniTestOverlay = null;

        ShowBanner("Thank you for playing!");
        StartCoroutine(HideBannerAfter(4f));
    }
    Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
    {
        var obj = new GameObject("Btn");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = obj.AddComponent<Image>();
        img.color = color;
        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = color,
            highlightedColor = color * 1.2f,
            pressedColor = color * 0.7f,
            selectedColor = color,
            disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        var lObj = new GameObject("Label");
        lObj.transform.SetParent(obj.transform, false);
        var lrt = lObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var t = lObj.AddComponent<Text>();
        t.text = label; t.font = GetFont(); t.fontSize = 42; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
        return btn;
    }

    string SafeGet(string key, string fallback)
    {
        var lm = LanguageManager.Instance;
        if (lm == null) return fallback;
        var result = lm.Get(key);
        return result == $"[{key}]" ? fallback : result;
    }

    void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    static Font _font;
    static Font GetFont()
    {
        if (_font != null) return _font;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        return _font;
    }
}