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

    private GameObject _howToCanvas;

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
        bg.color = new Color(0f, 0f, 0f, 0.78f);

        var card = new GameObject("Card");
        card.transform.SetParent(cObj.transform, false);
        var crt = card.AddComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(920f, 700f);
        crt.localScale = Vector3.zero;
        var cImg = card.AddComponent<Image>();
        cImg.color = new Color(0.14f, 0.10f, 0.06f, 0.98f);

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 14f);
        accent.AddComponent<Image>().color = new Color(1f, 0.78f, 0.45f);

        MakeLabel(card.transform, SafeGet("minigame_baboon_howto_title", "Hoe speel je?"),
            new Vector2(0f, -40f), new Vector2(840f, 80f), 54, FontStyle.Bold, new Color(1f, 0.82f, 0.5f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_baboon_howto_intro", "De baviaan laat een patroon van lichtjes zien. Kun jij het nadoen?"),
            new Vector2(0f, -150f), new Vector2(820f, 120f), 30, FontStyle.Normal, new Color(1f, 0.96f, 0.9f), out _);

        MakeHowToRow(card.transform, -300f,
            SafeGet("minigame_baboon_howto_line1", "Kijk goed welke knoppen oplichten, en in welke volgorde."));
        MakeHowToRow(card.transform, -410f,
            SafeGet("minigame_baboon_howto_line2", "Tik de knoppen daarna in precies dezelfde volgorde!"));

        var startBtn = MakeButton(card.transform, SafeGet("btn_lets_go", "Laten we beginnen!"),
            new Vector2(0f, 36f), new Vector2(520f, 120f), new Color(0.18f, 0.62f, 0.32f));
        var sbRt = startBtn.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0.5f, 0f); sbRt.anchorMax = new Vector2(0.5f, 0f); sbRt.pivot = new Vector2(0.5f, 0f);
        startBtn.onClick.AddListener(() =>
        {
            if (_howToCanvas != null) Destroy(_howToCanvas);
            _howToCanvas = null;
            StartMinigame();
        });

        StartCoroutine(PopInCard(crt));
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

        var stopBtn = MakeButton(cObj.transform, SafeGet("btn_back", "Stop"),
            new Vector2(30f, 30f), new Vector2(240f, 110f), new Color(0.55f, 0.18f, 0.18f));

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
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(900f, 580f);
        crt.localScale = Vector3.zero;

        var cImg = card.AddComponent<Image>();
        cImg.color = new Color(0.16f, 0.10f, 0.06f, 0.97f);

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
            new Vector2(0f, -150f), new Vector2(840f, 60f), 36, FontStyle.Normal, new Color(1f, 0.78f, 0.45f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_coins_earned", $"Je hebt {coinReward} munten verdiend!"),
            new Vector2(0f, -240f), new Vector2(840f, 60f), 38, FontStyle.Normal, new Color(0.35f, 1f, 0.55f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_baboon_success_desc", "De baviaan is trots op je!"),
            new Vector2(0f, -310f), new Vector2(840f, 50f), 26, FontStyle.Normal, new Color(0.95f, 0.85f, 0.7f), out _);

        var continueBtn = MakeButton(card.transform, SafeGet("btn_continue", "Doorgaan"),
            new Vector2(0f, 32f), new Vector2(500f, 110f), new Color(0.18f, 0.62f, 0.32f));

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