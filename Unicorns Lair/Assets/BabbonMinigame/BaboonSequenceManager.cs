using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BaboonSequenceManager : MonoBehaviour
{
    private enum BaboonSequenceState { Demonstrating, PlayerTurn, Feedback, Complete }

    [Header("References")]
    [SerializeField] private BaboonSequenceButton[] sequenceButtons;
    [SerializeField] private Transform baboonVisual;
    [SerializeField] private Animator baboonAnimator;

    [Header("Sequence Settings")]
    [SerializeField] private int sequenceLength = 3;
    [SerializeField] private float demonstrationDelay = 0.45f;
    [SerializeField] private float buttonGapDelay = 0.25f;
    [SerializeField] private float buttonAnimationLeadTime = 0.12f;

    [Header("Baboon Animations")]
    [SerializeField] private string baboonWrongTrigger;
    [SerializeField] private string baboonCelebrateTrigger;

    [Header("Reward")]
    [Tooltip("How many coins the player earns when they win.")]
    [SerializeField] private int coinReward = 10;
    [Tooltip("Name of the scene to load when the minigame finishes or is closed.")]
    [SerializeField] private string returnSceneName = "MainArea";

    [Header("Debug")]
    [SerializeField] private bool autoFindButtons = true;

    [Header("Settings")]
    [Tooltip("Show a kid-friendly 'How to Play' explanation before the game starts.")]
    [SerializeField] private bool showHowToPlay = true;

    [Header("Back Button (PNG)")]
    [SerializeField] private Sprite backButtonSprite;
    [SerializeField] private Vector2 backButtonPos = new Vector2(30f, 30f);
    [SerializeField] private Vector2 backButtonSize = new Vector2(240f, 110f);

    [Header("Congrats Panel (PNG)")]
    [SerializeField] private Sprite congratsPanelSprite;
    [SerializeField] private Vector2 congratsPanelPos = new Vector2(0f, 0f);
    [SerializeField] private Vector2 congratsPanelSize = new Vector2(900f, 580f);
    [Range(0f, 1f)]
    [SerializeField] private float congratsPanelOpacity = 1f;

    [Header("Continue Button (PNG)")]
    [SerializeField] private Sprite continueButtonSprite;
    [SerializeField] private Vector2 continueButtonPos = new Vector2(0f, 32f);
    [SerializeField] private Vector2 continueButtonSize = new Vector2(500f, 110f);

    [Header("How To Play - Images")]
    [Tooltip("Instructional PNGs shown per line. The image swaps as lines advance. If fewer images than lines, the last image is reused.")]
    [SerializeField] private Sprite[] howToImages;
    [SerializeField] private Vector2 howToImagePos = new Vector2(0f, 180f);
    [SerializeField] private Vector2 howToImageSize = new Vector2(820f, 820f);

    [Header("How To Play - Text")]
    [SerializeField] private Vector2 howToTextPos = new Vector2(0f, -560f);
    [SerializeField] private Vector2 howToTextSize = new Vector2(900f, 240f);
    [SerializeField] private int howToTextFontSize = 34;
    [SerializeField] private Color howToTextColor = Color.white;
    [SerializeField] private TextAnchor howToTextAlignment = TextAnchor.UpperCenter;
    [SerializeField] private float howToTextPadLeft = 30f;
    [SerializeField] private float howToTextPadRight = 30f;
    [SerializeField] private float howToTextPadTop = 10f;
    [SerializeField] private float howToTextPadBottom = 10f;

    [Header("How To Play - Tap To Continue")]
    [SerializeField] private Vector2 howToTapPos = new Vector2(0f, -740f);
    [SerializeField] private Vector2 howToTapSize = new Vector2(440f, 50f);
    [SerializeField] private int howToTapFontSize = 26;
    [SerializeField] private Color howToTapColor = new Color(1f, 0.9f, 0.5f);

    [Header("How To Play Voice")]
    [SerializeField] private LocalizedSoundData howToPlayLocalized;

    [Header("How To Play - Lets Go Button (PNG)")]
    [SerializeField] private Sprite letsGoButtonSprite;
    [SerializeField] private Vector2 letsGoButtonPos = new Vector2(0f, -760f);
    [SerializeField] private Vector2 letsGoButtonSize = new Vector2(480f, 170f);

    [Header("How To Play - Background")]
    [Range(0f, 1f)]
    [SerializeField] private float howToDimOpacity = 0.78f;

    private GameObject _howToCanvas;
    private (string key, string fallback)[] _htLines;
    private int _htPage;
    private int _htLineCount;
    private Text _htText;
    private Image _htImage;
    private GameObject _htTapIndicator;
    private Button _htLetsGoBtn;

    private BaboonSequenceState _state;
    private readonly List<int> _sequence = new List<int>();
    private int _playerInputIndex;

    private Canvas _uiCanvas;
    private Text _titleText;
    private Text _instructionText;
    private Text _feedbackText;
    private GameObject _congratsCanvas;

    private bool _hasStartedBefore;
    private bool _complete;

    void Start()
    {
        LanguageManager.Ensure();
        GameStateManager.Ensure();

        if (autoFindButtons && (sequenceButtons == null || sequenceButtons.Length == 0))
            sequenceButtons = FindObjectsByType<BaboonSequenceButton>(FindObjectsSortMode.None);

        InitializeButtons();
        BuildUI();

        if (showHowToPlay) ShowHowToPlay();
        else StartMinigame();

        LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    void OnDestroy()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    void OnLanguageChanged() => RefreshTextForState();

    public void StartMinigame()
    {
        if (_hasStartedBefore)
        {
            PlaytestLogger.Instance?.LogMinigameRetry("BaboonSequence");
        }

        _hasStartedBefore = true;
        _complete = false;

        _state = BaboonSequenceState.Demonstrating;
        _playerInputIndex = 0;

        GenerateSequence();

        SetFeedback("");
        SetButtonsInteractable(false);

        StartCoroutine(PlaySequence());
    }

    void InitializeButtons()
    {
        for (int i = 0; i < sequenceButtons.Length; i++)
            if (sequenceButtons[i] != null) sequenceButtons[i].Initialize(this, i);
    }

    void GenerateSequence()
    {
        _sequence.Clear();

        int buttonCount = sequenceButtons.Length;

        for (int i = 0; i < buttonCount; i++)
            _sequence.Add(i);

        for (int i = 0; i < _sequence.Count; i++)
        {
            int rIdx = Random.Range(i, _sequence.Count);

            int temp = _sequence[i];
            _sequence[i] = _sequence[rIdx];
            _sequence[rIdx] = temp;
        }

        sequenceLength = buttonCount;
    }

    IEnumerator PlaySequence()
    {
        _state = BaboonSequenceState.Demonstrating;

        SetButtonsInteractable(false);
        RefreshTextForState();
        SetFeedback("");

        yield return new WaitForSeconds(demonstrationDelay);

        for (int i = 0; i < _sequence.Count; i++)
        {
            int idx = _sequence[i];

            if (idx >= 0 && idx < sequenceButtons.Length && sequenceButtons[idx] != null)
            {
                TriggerButtonAnimation(sequenceButtons[idx]);

                if (buttonAnimationLeadTime > 0f)
                    yield return new WaitForSeconds(buttonAnimationLeadTime);

                yield return StartCoroutine(sequenceButtons[idx].Flash());
                yield return StartCoroutine(BaboonPressReaction());
                yield return new WaitForSeconds(buttonGapDelay);
            }
        }

        _playerInputIndex = 0;
        _state = BaboonSequenceState.PlayerTurn;

        RefreshTextForState();
        SetFeedback("");
        SetButtonsInteractable(true);
    }

    public void NotifyButtonPressed(BaboonSequenceButton pressed)
    {
        if (_state != BaboonSequenceState.PlayerTurn || pressed == null)
            return;

        TriggerButtonAnimation(pressed);
        StartCoroutine(HandlePlayerPress(pressed));
    }

    IEnumerator HandlePlayerPress(BaboonSequenceButton pressed)
    {
        _state = BaboonSequenceState.Feedback;
        SetButtonsInteractable(false);

        yield return StartCoroutine(pressed.Flash());

        int expected = _sequence[_playerInputIndex];

        if (pressed.ButtonIndex == expected)
        {
            _playerInputIndex++;

            SetFeedback(SafeGet("minigame_baboon_correct", "Goed!"), new Color(0.4f, 1f, 0.5f));
            yield return StartCoroutine(BaboonHappyReaction());

            if (_playerInputIndex >= _sequence.Count)
            {
                CompleteMinigame();
                yield break;
            }

            _state = BaboonSequenceState.PlayerTurn;
            SetButtonsInteractable(true);
            SetFeedback("");
        }
        else
        {
            SetFeedback(SafeGet("minigame_baboon_wrong", "Probeer opnieuw! Kijk goed."), new Color(1f, 0.55f, 0.4f));
            TriggerBaboonAnimation(baboonWrongTrigger);
            yield return StartCoroutine(BaboonWrongReaction());
            yield return new WaitForSeconds(0.6f);

            PlaytestLogger.Instance?.LogMinigameRetry("BaboonSequence");

            StartCoroutine(PlaySequence());
        }
    }

    void CompleteMinigame()

    {
        DeviceVibration.Vibrate();
        _complete = true;

        PlaytestLogger.Instance?.LogMinigameSuccess("BaboonSequence");

        _state = BaboonSequenceState.Complete;

        SetButtonsInteractable(false);
        TriggerBaboonAnimation(baboonCelebrateTrigger);
        StartCoroutine(BaboonCelebrate());

        DestroyMainUI();
        ShowCongrats();
    }

    void RefreshTextForState()
    {
        if (_titleText != null)
            _titleText.text = SafeGet("minigame_baboon_title", "Baviaan Volg het Patroon");

        if (_instructionText == null) return;

        switch (_state)
        {
            case BaboonSequenceState.Demonstrating:
                _instructionText.text = SafeGet("minigame_baboon_watch", "Kijk naar de baviaan!");
                break;

            case BaboonSequenceState.PlayerTurn:
                _instructionText.text = SafeGet("minigame_baboon_your_turn", "Jouw beurt!");
                break;
        }
    }

    void SetFeedback(string text, Color? color = null)
    {
        if (_feedbackText == null) return;

        _feedbackText.text = text;
        _feedbackText.color = color ?? Color.white;
    }

    void ShowHowToPlay()
    {
        var cObj = new GameObject("HowToCanvas");
        _howToCanvas = cObj;
        var cv = cObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 24;
        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        cObj.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        var bg = cObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, howToDimOpacity);
        var dimBtn = cObj.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.targetGraphic = bg;
        dimBtn.onClick.AddListener(AdvanceHowTo);

        var imgObj = new GameObject("HowToImage");
        imgObj.transform.SetParent(cObj.transform, false);
        var iRt = imgObj.AddComponent<RectTransform>();
        iRt.anchorMin = iRt.anchorMax = iRt.pivot = new Vector2(0.5f, 0.5f);
        iRt.anchoredPosition = howToImagePos; iRt.sizeDelta = howToImageSize;
        _htImage = imgObj.AddComponent<Image>();
        _htImage.raycastTarget = false;
        _htImage.preserveAspect = true;

        var txtObj = new GameObject("HowToTextBox");
        txtObj.transform.SetParent(cObj.transform, false);
        var tRt = txtObj.AddComponent<RectTransform>();
        tRt.anchorMin = tRt.anchorMax = tRt.pivot = new Vector2(0.5f, 0.5f);
        tRt.anchoredPosition = howToTextPos; tRt.sizeDelta = howToTextSize;

        var txtInner = new GameObject("Text");
        txtInner.transform.SetParent(txtObj.transform, false);
        var tiRt = txtInner.AddComponent<RectTransform>();
        tiRt.anchorMin = Vector2.zero; tiRt.anchorMax = Vector2.one;
        tiRt.offsetMin = new Vector2(howToTextPadLeft, howToTextPadBottom);
        tiRt.offsetMax = new Vector2(-howToTextPadRight, -howToTextPadTop);
        _htText = txtInner.AddComponent<Text>();
        _htText.font = GetFont();
        _htText.fontSize = howToTextFontSize;
        _htText.fontStyle = FontStyle.Bold;
        _htText.alignment = howToTextAlignment;
        _htText.color = howToTextColor;
        _htText.raycastTarget = false;
        _htText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _htText.verticalOverflow = VerticalWrapMode.Overflow;

        var tapObj = new GameObject("TapToContinue");
        tapObj.transform.SetParent(cObj.transform, false);
        var tapRt = tapObj.AddComponent<RectTransform>();
        tapRt.anchorMin = tapRt.anchorMax = tapRt.pivot = new Vector2(0.5f, 0.5f);
        tapRt.anchoredPosition = howToTapPos; tapRt.sizeDelta = howToTapSize;
        var tapTxt = tapObj.AddComponent<Text>();
        tapTxt.font = GetFont(); tapTxt.fontSize = howToTapFontSize; tapTxt.fontStyle = FontStyle.Bold;
        tapTxt.alignment = TextAnchor.MiddleCenter;
        tapTxt.color = howToTapColor;
        tapTxt.raycastTarget = false;
        tapTxt.text = SafeGet("intro_tap_continue", "Tik om verder \u25B6");
        _htTapIndicator = tapObj;

        var lgObj = new GameObject("LetsGoButton");
        lgObj.transform.SetParent(cObj.transform, false);
        var lgRt = lgObj.AddComponent<RectTransform>();
        lgRt.anchorMin = lgRt.anchorMax = lgRt.pivot = new Vector2(0.5f, 0.5f);
        lgRt.anchoredPosition = letsGoButtonPos; lgRt.sizeDelta = letsGoButtonSize;
        var lgImg = lgObj.AddComponent<Image>();
        lgImg.sprite = letsGoButtonSprite;
        lgImg.color = Color.white;
        lgImg.preserveAspect = true;
        _htLetsGoBtn = lgObj.AddComponent<Button>();
        _htLetsGoBtn.targetGraphic = lgImg;
        _htLetsGoBtn.onClick.AddListener(() =>
        {
            if (_howToCanvas != null) Destroy(_howToCanvas);
            _howToCanvas = null;
            StartMinigame();
        });
        lgObj.SetActive(false);

        _htLines = new (string, string)[]
        {
            ("minigame_baboon_howto_intro", "De baviaan laat een patroon van lichtjes zien. Kun jij het nadoen?"),
            ("minigame_baboon_howto_line1", "Kijk goed welke knoppen oplichten, en in welke volgorde."),
            ("minigame_baboon_howto_line2", "Tik de knoppen daarna in precies dezelfde volgorde!"),
        };
        _htLineCount = _htLines.Length;
        _htPage = 0;

        ShowHowToPage(0);
    }

    void AdvanceHowTo()
    {
        if (_htLines == null) return;
        if (_htPage >= _htLineCount - 1) return;
        ShowHowToPage(_htPage + 1);
    }

    void ShowHowToPage(int index)
    {
        if (_htLines == null || _htLineCount == 0) return;
        _htPage = Mathf.Clamp(index, 0, _htLineCount - 1);

        if (_htText != null)
            _htText.text = SafeGet(_htLines[_htPage].key, _htLines[_htPage].fallback);

        if (_htImage != null)
        {
            Sprite sp = (howToImages != null && howToImages.Length > 0)
                ? howToImages[Mathf.Min(_htPage, howToImages.Length - 1)]
                : null;
            _htImage.sprite = sp;
            _htImage.enabled = sp != null;
        }

        // Play localized how-to voice for this page (glue first two clips)
        MinigameVoicePlayer.PlayLocalizedForPage(howToPlayLocalized, _htPage, true);

        bool last = _htPage >= _htLineCount - 1;
        if (_htTapIndicator != null) _htTapIndicator.SetActive(!last);
        if (_htLetsGoBtn != null) _htLetsGoBtn.gameObject.SetActive(last);
    }

    void MakeHowToRow(Transform parent, float y, string text)
    {
        var row = new GameObject("Row");
        row.transform.SetParent(parent, false);
        var rRt = row.AddComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0.5f, 1f); rRt.anchorMax = new Vector2(0.5f, 1f);
        rRt.pivot = new Vector2(0.5f, 1f); rRt.anchoredPosition = new Vector2(0f, y); rRt.sizeDelta = new Vector2(820f, 90f);
        row.AddComponent<Image>().color = new Color(0.42f, 0.28f, 0.12f, 0.85f);

        var lObj = new GameObject("Label");
        lObj.transform.SetParent(row.transform, false);
        var lrt = lObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(20f, 0f); lrt.offsetMax = new Vector2(-20f, 0f);
        var t = lObj.AddComponent<Text>();
        t.text = text; t.font = GetFont(); t.fontSize = 28; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
    }

    void BuildUI()
    {
        var cObj = new GameObject("BaboonCanvas");

        _uiCanvas = cObj.AddComponent<Canvas>();
        _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _uiCanvas.sortingOrder = 20;

        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        cObj.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        var header = new GameObject("Header");
        header.transform.SetParent(cObj.transform, false);

        var hrt = header.AddComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0f, 1f);
        hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(0f, 230f);

        header.AddComponent<Image>().color = new Color(0.12f, 0.08f, 0.05f, 0.92f);

        MakeLabel(header.transform,
            SafeGet("minigame_baboon_title", "Baviaan Volg het Patroon"),
            new Vector2(0f, -16f), new Vector2(1000f, 70f), 50, FontStyle.Bold,
            new Color(1f, 0.78f, 0.45f), out _titleText);

        MakeLabel(header.transform,
            SafeGet("minigame_baboon_watch", "Kijk naar de baviaan!"),
            new Vector2(0f, -100f), new Vector2(1000f, 60f), 32, FontStyle.Normal,
            new Color(1f, 0.92f, 0.78f), out _instructionText);

        MakeLabel(header.transform, "",
            new Vector2(0f, -170f), new Vector2(1000f, 50f), 30, FontStyle.Bold,
            Color.white, out _feedbackText);

        var stopBtn = MakeSpriteButton(cObj.transform, backButtonSprite, null, backButtonPos, backButtonSize);

        stopBtn.onClick.AddListener(ExitToMainArea);
    }

    void ShowCongrats()
    {
        var cObj = new GameObject("CongratsCanvas");
        _congratsCanvas = cObj;

        var cv = cObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 25;

        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        cObj.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        var bg = cObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false;

        var card = new GameObject("Card");
        card.transform.SetParent(cObj.transform, false);

        var crt = card.AddComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = congratsPanelPos;
        crt.sizeDelta = congratsPanelSize;
        crt.localScale = Vector3.zero;

        var cImg = card.AddComponent<Image>();
        if (congratsPanelSprite != null)
        {
            cImg.sprite = congratsPanelSprite;
            cImg.type = Image.Type.Simple;
            cImg.preserveAspect = false;
            cImg.color = new Color(1f, 1f, 1f, congratsPanelOpacity);
        }
        else cImg.color = new Color(0.14f, 0.11f, 0.07f, 0.97f);

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);

        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f);
        aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f);
        aRt.anchoredPosition = Vector2.zero;
        aRt.sizeDelta = new Vector2(0f, 14f);

        accent.AddComponent<Image>().color = new Color(1f, 0.55f, 0.25f);

        MakeLabel(card.transform, SafeGet("minigame_complete", "Gefeliciteerd!"),
            new Vector2(0f, -55f), new Vector2(840f, 80f), 56, FontStyle.Bold, Color.white, out _);

        MakeLabel(card.transform, SafeGet("minigame_baboon_success_title", "Geweldig gedaan!"),
            new Vector2(0f, -150f), new Vector2(840f, 60f), 36, FontStyle.Normal, Color.white, out _);

        MakeLabel(card.transform,
            SafeGet("minigame_coins_earned", $"Je hebt {coinReward} munten verdiend!"),
            new Vector2(0f, -240f), new Vector2(840f, 60f), 38, FontStyle.Normal, Color.white, out _);

        MakeLabel(card.transform,
            SafeGet("minigame_baboon_success_desc", "De baviaan is trots op je!"),
            new Vector2(0f, -310f), new Vector2(840f, 50f), 26, FontStyle.Normal, Color.white, out _);

        var continueBtn = MakeSpriteButton(card.transform, continueButtonSprite, SafeGet("btn_continue", "Doorgaan"), continueButtonPos, continueButtonSize);

        continueBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
        continueBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
        continueBtn.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);

        continueBtn.onClick.AddListener(OnContinue);

        StartCoroutine(PopInCard(crt));
    }

    IEnumerator PopInCard(RectTransform rt)
    {
        float t = 0f;

        while (t < 0.35f)
        {
            t += Time.deltaTime;

            if (rt == null)
                yield break;

            float p = t / 0.35f;
            float overshoot = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;

            rt.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, p) * overshoot;

            yield return null;
        }

        if (rt != null)
            rt.localScale = Vector3.one;
    }

    void OnContinue()
    {
        GameStateManager.Instance.AddCoins(coinReward);
        SceneManager.LoadScene(returnSceneName);
    }

    void ExitToMainArea()
    {
        if (!_complete)
        {
            PlaytestLogger.Instance?.LogMinigameFail(
                "BaboonSequence",
                "Player exited before completion"
            );
        }

        SceneManager.LoadScene(returnSceneName);
    }

    void DestroyMainUI()
    {
        if (_uiCanvas != null)
            Destroy(_uiCanvas.gameObject);

        _uiCanvas = null;
    }

    IEnumerator BaboonPressReaction()
    {
        if (baboonVisual == null) yield break;

        Vector3 baseScale = baboonVisual.localScale;
        baboonVisual.localScale = baseScale * 1.05f;

        yield return new WaitForSeconds(0.12f);

        baboonVisual.localScale = baseScale;
    }

    IEnumerator BaboonHappyReaction()
    {
        if (baboonVisual == null) yield break;

        Vector3 baseScale = baboonVisual.localScale;
        baboonVisual.localScale = baseScale * 1.08f;

        yield return new WaitForSeconds(0.16f);

        baboonVisual.localScale = baseScale;
    }

    IEnumerator BaboonWrongReaction()
    {
        if (baboonVisual == null) yield break;

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

    IEnumerator BaboonCelebrate()
    {
        if (baboonVisual == null) yield break;

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

    void TriggerButtonAnimation(BaboonSequenceButton button)
    {
        // Prevents errors if parameters aren't set up correctly
        if (baboonVisual == null || button == null || string.IsNullOrWhiteSpace(button.animationTrigger))
            return;

        baboonAnimator.SetTrigger(button.animationTrigger);
    }

    void TriggerBaboonAnimation(string trigger)
    {
        if (baboonAnimator == null || string.IsNullOrWhiteSpace(trigger))
            return;

        baboonAnimator.SetTrigger(trigger);
    }

    void SetButtonsInteractable(bool value)
    {
        foreach (var b in sequenceButtons)
            if (b != null) b.SetInteractable(value);
    }

    void MakeLabel(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, FontStyle style, Color color, out Text refOut)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);

        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var t = obj.AddComponent<Text>();
        t.text = text;
        t.font = GetFont();
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.raycastTarget = false;

        refOut = t;
    }

    Button MakeSpriteButton(Transform parent, Sprite sprite, string label, Vector2 pos, Vector2 size)
    {
        var obj = new GameObject("SpriteBtn");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 0f); rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        var img = obj.AddComponent<Image>();
        Color baseCol;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
            baseCol = Color.white;
        }
        else
        {
            baseCol = new Color(0.3f, 0.3f, 0.3f, 0.95f);
            img.color = baseCol;
        }

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = baseCol,
            highlightedColor = baseCol * 1.12f,
            pressedColor = baseCol * 0.8f,
            selectedColor = baseCol,
            disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        if (!string.IsNullOrEmpty(label))
        {
            var lObj = new GameObject("Label");
            lObj.transform.SetParent(obj.transform, false);
            var lrt = lObj.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var t = lObj.AddComponent<Text>();
            t.text = label; t.font = GetFont(); t.fontSize = 42; t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
        }

        return btn;
    }

    Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
    {
        var obj = new GameObject($"Btn_{label}");
        obj.transform.SetParent(parent, false);

        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

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
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var t = lObj.AddComponent<Text>();
        t.text = label;
        t.font = GetFont();
        t.fontSize = 42;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;

        return btn;
    }

    string SafeGet(string key, string fallback)
    {
        var lm = LanguageManager.Instance;

        if (lm == null)
            return fallback;

        var result = lm.Get(key);

        return result == $"[{key}]" ? fallback : result;
    }

    void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
            return;

        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    static Font _font;

    static Font GetFont()
    {
        if (_font != null)
            return _font;

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (_font == null)
            _font = Font.CreateDynamicFontFromOSFont("Arial", 24);

        return _font;
    }
}