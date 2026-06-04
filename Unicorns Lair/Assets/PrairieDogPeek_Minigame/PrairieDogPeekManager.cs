using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PrairieDogPeekManager : MonoBehaviour
{
    private enum PrairieDogPeekState { WaitingForShake, Revealing, WaitingForChoice, Feedback, Complete }

    [Header("References")]
    [SerializeField] private PrairieDogShakeInput shakeInput;
    [SerializeField] private PrairieDogHole[] holes;

    [Header("Rounds")]
    [Tooltip("How many times the player has to find the correct hole before winning.")]
    [SerializeField] private int roundsToWin = 3;

    [Header("Difficulty")]
    [Tooltip("How long the prairie dog stays visible at round 1 (seconds).")]
    [SerializeField] private float baseVisibleDuration = 2f;
    [Tooltip("How much shorter the visible time gets each successful round (seconds).")]
    [SerializeField] private float visibleDurationDecreasePerRound = 0.25f;
    [Tooltip("Visible time will never go below this minimum (seconds).")]
    [SerializeField] private float minVisibleDuration = 0.4f;
    [Tooltip("Pop animation speed (constant, doesn't scale).")]
    [SerializeField] private float popSpeed = 8f;

    [Header("Timing")]
    [SerializeField] private float feedbackDuration = 1.2f;

    [Header("Reward")]
    [Tooltip("How many coins the player earns when they win.")]
    [SerializeField] private int coinReward = 10;
    [Tooltip("Name of the scene to load when the minigame finishes or is closed.")]
    [SerializeField] private string returnSceneName = "MainArea";

    [Header("Debug")]
    [SerializeField] private bool autoFindHoles = true;

    [Header("Settings")]
    [Tooltip("Show a kid-friendly 'How to Play' explanation before the game starts.")]
    [SerializeField] private bool showHowToPlay = true;

    private GameObject _howToCanvas;
    private bool _started;

    private PrairieDogPeekState _state;
    private int _correctHoleIndex = -1;
    private int _currentRound = 0;
    private int _correctCount = 0;

    private Canvas _uiCanvas;
    private Text _titleText;
    private Text _instructionText;
    private Text _feedbackText;
    private GameObject _congratsCanvas;
    private bool _complete;

    void Start()
    {
        LanguageManager.Ensure();
        GameStateManager.Ensure();

        if (shakeInput == null) shakeInput = FindFirstObjectByType<PrairieDogShakeInput>();
        if (autoFindHoles && (holes == null || holes.Length == 0))
            holes = FindObjectsByType<PrairieDogHole>(FindObjectsSortMode.None);

        InitializeHoles();
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

    void Update()
    {
        if (!_started) return;
        if (_state != PrairieDogPeekState.WaitingForShake) return;
        if (shakeInput != null && shakeInput.WasShakeDetectedThisFrame)
            StartCoroutine(RevealSequence());
    }

    public void StartMinigame()
    {
        _started = true;
        _complete = false;
        _currentRound = 0;
        _correctCount = 0;
        _correctHoleIndex = -1;
        SetState(PrairieDogPeekState.WaitingForShake);
        SetFeedback("");
        foreach (var hole in holes) if (hole != null) hole.HideDogInstant();
    }

    void InitializeHoles()
    {
        for (int i = 0; i < holes.Length; i++)
            if (holes[i] != null) holes[i].Initialize(this, i);
    }

    IEnumerator RevealSequence()
    {
        SetState(PrairieDogPeekState.Revealing);
        SetFeedback("");

        if (_correctHoleIndex < 0)
            _correctHoleIndex = Random.Range(0, holes.Length);

        int running = 0;

        foreach (var h in holes)
        {
            if (h == null) continue;

            running++;
            StartCoroutine(ShakeHoleAndCount(h, () => running--));
        }

        while (running > 0)
            yield return null;

        var correctHole = holes[_correctHoleIndex];

        if (correctHole == null)
        {
            SetState(PrairieDogPeekState.WaitingForShake);
            yield break;
        }

        yield return StartCoroutine(correctHole.PopDogUp(popSpeed));

        SetState(PrairieDogPeekState.WaitingForChoice);

        yield return new WaitForSeconds(CurrentVisibleDuration());

        if (_state == PrairieDogPeekState.WaitingForChoice)
        {
            yield return StartCoroutine(correctHole.HideDogDown(popSpeed));

            _correctHoleIndex = -1;
            SetFeedback("");
            SetState(PrairieDogPeekState.WaitingForShake);
        }
    }

    IEnumerator ShakeHoleAndCount(PrairieDogHole hole, System.Action onDone)
    {
        yield return StartCoroutine(hole.PlayHoleShake());
        onDone?.Invoke();
    }

    float CurrentVisibleDuration()
    {
        float d = baseVisibleDuration - _currentRound * visibleDurationDecreasePerRound;
        return Mathf.Max(minVisibleDuration, d);
    }

    public void NotifyHolePressed(PrairieDogHole hole)
    {
        if (_state != PrairieDogPeekState.WaitingForChoice || hole == null) return;
        StartCoroutine(HandleChoice(hole));
    }

    IEnumerator HandleChoice(PrairieDogHole selectedHole)
    {
        SetState(PrairieDogPeekState.Feedback);

        bool correct = selectedHole.HoleIndex == _correctHoleIndex;
        if (correct)
        {
            _correctCount++;
            SetFeedback(SafeGet("minigame_prairiedog_correct", "Goed gedaan!"), new Color(0.4f, 1f, 0.5f));
            yield return StartCoroutine(selectedHole.PlayCorrectFeedback(popSpeed));
        }
        else
        {
            SetFeedback(SafeGet("minigame_prairiedog_wrong", "Niet helemaal! Hier was hij."), new Color(1f, 0.55f, 0.4f));
            yield return StartCoroutine(selectedHole.PlayWrongFeedback());
            var correctHole = holes[_correctHoleIndex];
            if (correctHole != null) yield return StartCoroutine(correctHole.PlayCorrectFeedback(popSpeed));
        }
        PlaytestLogger.Instance?.LogMinigameRetry("PrairieDog");
        yield return new WaitForSeconds(feedbackDuration);

        if (correct)
        {
            if (_correctCount >= roundsToWin)
            {
                CompleteMinigame();
            }
            else
            {
                _currentRound++;
                _correctHoleIndex = -1;
                SetFeedback("");
                SetState(PrairieDogPeekState.WaitingForShake);
            }
        }
        else
        {
            SetFeedback("");
            StartCoroutine(RevealSequence());
        }
    }

    void CompleteMinigame()
    {
        _complete = true;
        PlaytestLogger.Instance?.LogMinigameSuccess("PrairieDog");
        SetState(PrairieDogPeekState.Complete);
        DestroyMainUI();
        ShowCongrats();
    }

    void SetState(PrairieDogPeekState next)
    {
        _state = next;
        RefreshTextForState();
    }

    void RefreshTextForState()
    {
        if (_titleText != null)
            _titleText.text = SafeGet("minigame_prairiedog_title", "Prairiehond Spel");

        if (_instructionText == null) return;
        switch (_state)
        {
            case PrairieDogPeekState.WaitingForShake:
                _instructionText.text = SafeGet(GetShakeInstructionKey(), "Schud de tablet!");
                break;
            case PrairieDogPeekState.Revealing:
                _instructionText.text = SafeGet("minigame_prairiedog_watch", "Kijk goed!");
                break;
            case PrairieDogPeekState.WaitingForChoice:
                _instructionText.text = SafeGet("minigame_prairiedog_tap", "Tik op het juiste hol!");
                break;
            case PrairieDogPeekState.Feedback:
            case PrairieDogPeekState.Complete:
                break;
        }
    }

    string GetShakeInstructionKey()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return "minigame_prairiedog_shake_pc";
#else
        return "minigame_prairiedog_shake_tablet";
#endif
    }

    void SetFeedback(string text, Color? color = null)
    {
        if (_feedbackText == null) return;
        _feedbackText.text = text;
        if (color.HasValue) _feedbackText.color = color.Value;
        else _feedbackText.color = new Color(1f, 1f, 1f);
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
        cImg.color = new Color(0.16f, 0.12f, 0.06f, 0.98f);

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 14f);
        accent.AddComponent<Image>().color = new Color(0.93f, 0.72f, 0.32f);

        MakeLabel(card.transform, SafeGet("minigame_prairiedog_howto_title", "Hoe speel je?"),
            new Vector2(0f, -40f), new Vector2(840f, 80f), 54, FontStyle.Bold, new Color(1f, 0.85f, 0.5f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_prairiedog_howto_intro", "De prairiehonden verstoppen zich in hun holen. Kun jij ze vinden?"),
            new Vector2(0f, -150f), new Vector2(820f, 120f), 30, FontStyle.Normal, new Color(1f, 0.96f, 0.9f), out _);

        MakeHowToRow(card.transform, -300f,
            SafeGet("minigame_prairiedog_howto_line1", "Schud de tablet en kijk goed welk hol de prairiehond kiest."));
        MakeHowToRow(card.transform, -410f,
            SafeGet("minigame_prairiedog_howto_line2", "Tik daarna op het juiste hol om hem te vinden!"));

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
        row.AddComponent<Image>().color = new Color(0.42f, 0.30f, 0.12f, 0.85f);

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
        var cObj = new GameObject("PrairieDogCanvas");
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
        hrt.anchorMin = new Vector2(0f, 1f); hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f); hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(0f, 230f);
        header.AddComponent<Image>().color = new Color(0.10f, 0.08f, 0.06f, 0.92f);

        MakeLabel(header.transform,
            SafeGet("minigame_prairiedog_title", "Prairiehond Spel"),
            new Vector2(0f, -16f), new Vector2(1000f, 70f), 50, FontStyle.Bold,
            new Color(1f, 0.85f, 0.55f), out _titleText);

        MakeLabel(header.transform,
            SafeGet(GetShakeInstructionKey(), "Schud de tablet!"),
            new Vector2(0f, -100f), new Vector2(1000f, 60f), 32, FontStyle.Normal,
            new Color(1f, 0.95f, 0.8f), out _instructionText);

        MakeLabel(header.transform,
            "",
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
        cImg.color = new Color(0.14f, 0.10f, 0.08f, 0.97f);

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 14f);
        accent.AddComponent<Image>().color = new Color(1f, 0.65f, 0.20f);

        MakeLabel(card.transform, SafeGet("minigame_complete", "Gefeliciteerd!"),
            new Vector2(0f, -55f), new Vector2(840f, 80f), 56, FontStyle.Bold, Color.white, out _);

        MakeLabel(card.transform, SafeGet("minigame_prairiedog_success_title", "Goed gespot!"),
            new Vector2(0f, -150f), new Vector2(840f, 60f), 36, FontStyle.Normal, new Color(1f, 0.85f, 0.55f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_coins_earned", $"Je hebt {coinReward} munten verdiend!"),
            new Vector2(0f, -240f), new Vector2(840f, 60f), 38, FontStyle.Normal, new Color(0.35f, 1f, 0.55f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_prairiedog_success_desc", "Goed gedaan, dierenoppasser!"),
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
            if (rt == null) yield break;
            float p = t / 0.35f;
            float overshoot = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
            rt.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, p) * overshoot;
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
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
                "PrairieDog",
                "Player exited before completion"
            );
        }

        SceneManager.LoadScene(returnSceneName);
    }

    void DestroyMainUI()
    {
        if (_uiCanvas != null) Destroy(_uiCanvas.gameObject);
        _uiCanvas = null;
    }

    void MakeLabel(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, FontStyle style, Color color, out Text refOut)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = obj.AddComponent<Text>();
        t.text = text; t.font = GetFont(); t.fontSize = fontSize; t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter; t.color = color; t.raycastTarget = false;
        refOut = t;
    }

    Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
    {
        var obj = new GameObject($"Btn_{label}");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
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