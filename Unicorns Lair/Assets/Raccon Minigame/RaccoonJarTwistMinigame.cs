using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RaccoonJarTwistMinigame : MonoBehaviour
{
    [Header("Main Objects")]
    [SerializeField] private Transform jarLid;
    [SerializeField] private GameObject treatsObject;
    [SerializeField] private Transform raccoonObject;

    [Header("Game Settings")]
    [SerializeField] private int twistsNeeded = 3;
    [SerializeField] private float finishDelay = 1.2f;

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

    [Header("Reward")]
    [Tooltip("How many coins the player earns when they win.")]
    [SerializeField] private int coinReward = 10;
    [Tooltip("Name of the scene to load when the minigame finishes or is closed.")]
    [SerializeField] private string returnSceneName = "MainArea";

    [Header("Rotation Detection")]
    [SerializeField] private bool useGyroscope = true;
    [SerializeField] private float twistThreshold = 1.35f;
    [SerializeField] private float twistCooldown = 0.35f;

    [Header("PC Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;
    [SerializeField] private Key leftTwistKey = Key.A;
    [SerializeField] private Key rightTwistKey = Key.D;

    [Header("Visual Feedback")]
    [SerializeField] private float lidRotationPerTwist = 35f;
    [SerializeField] private float lidPopHeight = 0.75f;
    [SerializeField] private float animationSpeed = 8f;

    private int currentTwists;
    private bool leftDetected;
    private bool rightDetected;
    private bool isFinished;
    private bool _started;
    private float lastTwistTime = -999f;

    private Quaternion lidStartRotation;
    private Vector3 lidStartPosition;
    private Vector3 raccoonStartScale;

    private Canvas _uiCanvas;
    private Text _titleText;
    private Text _hintText;
    private GameObject _howToCanvas;
    private GameObject _congratsCanvas;
    private Image[] _pips;

    private (string key, string fallback)[] _htLines;
    private int _htPage;
    private int _htLineCount;
    private Text _htText;
    private Image _htImage;
    private GameObject _htTapIndicator;
    private Button _htLetsGoBtn;

    private void Awake()
    {
        if (jarLid != null)
        {
            lidStartRotation = jarLid.localRotation;
            lidStartPosition = jarLid.localPosition;
        }
        if (raccoonObject != null) raccoonStartScale = raccoonObject.localScale;
        if (treatsObject != null) treatsObject.SetActive(false);
    }

    private void Start()
    {
        LanguageManager.Ensure();
        GameStateManager.Ensure();

        BuildUI();
        UpdateProgressUI();

        if (showHowToPlay) ShowHowToPlay();
        else _started = true;

        LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        if (_titleText != null) _titleText.text = SafeGet("minigame_raccoon_title", "Draai de Pot Open!");
        if (_hintText != null && !isFinished) _hintText.text = SafeGet("minigame_raccoon_instruction", "Draai de tablet heen en weer om de pot te openen!");
    }

    private void OnEnable() => EnableSensors();
    private void OnDisable() => DisableSensors();

    private void Update()
    {
        if (!_started || isFinished)
            return;

        DetectRotationInput();
        DetectKeyboardDebugInput();
    }

    private void EnableSensors()
    {
        if (!useGyroscope) return;
        if (UnityEngine.InputSystem.Gyroscope.current != null) InputSystem.EnableDevice(UnityEngine.InputSystem.Gyroscope.current);
        if (AttitudeSensor.current != null) InputSystem.EnableDevice(AttitudeSensor.current);
    }

    private void DisableSensors()
    {
        if (UnityEngine.InputSystem.Gyroscope.current != null) InputSystem.DisableDevice(UnityEngine.InputSystem.Gyroscope.current);
        if (AttitudeSensor.current != null) InputSystem.DisableDevice(AttitudeSensor.current);
    }

    private void DetectRotationInput()
    {
        if (!useGyroscope) return;
        if (UnityEngine.InputSystem.Gyroscope.current == null) return;

        float zRotation = UnityEngine.InputSystem.Gyroscope.current.angularVelocity.ReadValue().z;

        if (Time.time - lastTwistTime < twistCooldown) return;

        if (zRotation > twistThreshold) rightDetected = true;
        if (zRotation < -twistThreshold) leftDetected = true;

        if (leftDetected && rightDetected) RegisterSuccessfulTwist();
    }

    private void DetectKeyboardDebugInput()
    {
        if (!allowKeyboardDebug) return;
        if (Keyboard.current == null) return;
        if (Time.time - lastTwistTime < twistCooldown) return;

        if (Keyboard.current[leftTwistKey].wasPressedThisFrame) leftDetected = true;
        if (Keyboard.current[rightTwistKey].wasPressedThisFrame) rightDetected = true;

        if (leftDetected && rightDetected) RegisterSuccessfulTwist();
    }

    private void RegisterSuccessfulTwist()
    {
        lastTwistTime = Time.time;
        leftDetected = false;
        rightDetected = false;

        currentTwists = Mathf.Clamp(currentTwists + 1, 0, twistsNeeded);
        UpdateProgressUI();

        StopAllCoroutines();
        StartCoroutine(AnimateTwistFeedback());

        if (currentTwists >= twistsNeeded)
            StartCoroutine(FinishMinigame());
    }

    private void UpdateProgressUI()
    {
        if (_pips == null) return;
        for (int i = 0; i < _pips.Length; i++)
        {
            if (_pips[i] == null) continue;
            _pips[i].color = i < currentTwists
                ? new Color(0.95f, 0.65f, 0.20f)
                : new Color(1f, 1f, 1f, 0.25f);
        }
    }

    private IEnumerator AnimateTwistFeedback()
    {
        if (jarLid == null) yield break;

        Quaternion startRot = jarLid.localRotation;
        Quaternion targetRot = lidStartRotation * Quaternion.Euler(0f, currentTwists * lidRotationPerTwist, 0f);

        Vector3 startScale = jarLid.localScale;
        Vector3 biggerScale = startScale * 1.08f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed;
            jarLid.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            jarLid.localScale = Vector3.Lerp(startScale, biggerScale, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        jarLid.localRotation = targetRot;
        jarLid.localScale = startScale;

        if (raccoonObject != null) StartCoroutine(AnimateRaccoonBounce());
    }

    private IEnumerator AnimateRaccoonBounce()
    {
        Vector3 startScale = raccoonStartScale;
        Vector3 bounceScale = raccoonStartScale * 1.12f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 10f;
            raccoonObject.localScale = Vector3.Lerp(startScale, bounceScale, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        raccoonObject.localScale = startScale;
    }

    private IEnumerator FinishMinigame()
    {
        isFinished = true;

        if (_hintText != null) _hintText.text = SafeGet("minigame_complete", "Gefeliciteerd!");
        if (treatsObject != null) treatsObject.SetActive(true);

        yield return StartCoroutine(AnimateJarOpen());
        yield return new WaitForSeconds(finishDelay);

        DestroyMainUI();
        ShowCongrats();
    }

    private IEnumerator AnimateJarOpen()
    {
        if (jarLid == null) yield break;

        Vector3 startPos = jarLid.localPosition;
        Vector3 targetPos = lidStartPosition + new Vector3(0f, lidPopHeight, 0.25f);

        Quaternion startRot = jarLid.localRotation;
        Quaternion targetRot = startRot * Quaternion.Euler(25f, 0f, 35f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2.5f;
            jarLid.localPosition = Vector3.Lerp(startPos, targetPos, t);
            jarLid.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        jarLid.localPosition = targetPos;
        jarLid.localRotation = targetRot;
    }

    // ---------- UI ----------

    private void BuildUI()
    {
        var cObj = new GameObject("RaccoonCanvas");
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
        hrt.sizeDelta = new Vector2(0f, 240f);
        header.AddComponent<Image>().color = new Color(0.16f, 0.12f, 0.10f, 0.92f);

        MakeLabel(header.transform, SafeGet("minigame_raccoon_title", "Draai de Pot Open!"),
            new Vector2(0f, -16f), new Vector2(1000f, 70f), 50, FontStyle.Bold,
            new Color(1f, 0.85f, 0.55f), out _titleText);

        MakeLabel(header.transform, SafeGet("minigame_raccoon_instruction", "Draai de tablet heen en weer om de pot te openen!"),
            new Vector2(0f, -100f), new Vector2(1000f, 60f), 28, FontStyle.Normal,
            new Color(1f, 0.95f, 0.88f), out _hintText);

        // Progress pips row
        var pipRow = new GameObject("PipRow");
        pipRow.transform.SetParent(header.transform, false);
        var prRt = pipRow.AddComponent<RectTransform>();
        prRt.anchorMin = new Vector2(0.5f, 1f); prRt.anchorMax = new Vector2(0.5f, 1f);
        prRt.pivot = new Vector2(0.5f, 1f); prRt.anchoredPosition = new Vector2(0f, -160f);
        prRt.sizeDelta = new Vector2(1000f, 60f);

        int n = Mathf.Max(1, twistsNeeded);
        _pips = new Image[n];
        float pipSize = 46f;
        float spacing = 26f;
        float totalW = n * pipSize + (n - 1) * spacing;
        float startX = -totalW * 0.5f + pipSize * 0.5f;
        for (int i = 0; i < n; i++)
        {
            var pip = new GameObject($"Pip_{i}");
            pip.transform.SetParent(pipRow.transform, false);
            var piRt = pip.AddComponent<RectTransform>();
            piRt.anchorMin = piRt.anchorMax = piRt.pivot = new Vector2(0.5f, 0.5f);
            piRt.anchoredPosition = new Vector2(startX + i * (pipSize + spacing), 0f);
            piRt.sizeDelta = new Vector2(pipSize, pipSize);
            var img = pip.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.25f);
            img.raycastTarget = false;
            _pips[i] = img;
        }

        var stopBtn = MakeSpriteButton(cObj.transform, backButtonSprite, null, backButtonPos, backButtonSize);
        stopBtn.onClick.AddListener(ExitToMainArea);
    }

    private void ShowHowToPlay()
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

        // Full-screen dim that also captures taps to advance to the next line.
        var bg = cObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, howToDimOpacity);
        var dimBtn = cObj.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.targetGraphic = bg;
        dimBtn.onClick.AddListener(AdvanceHowTo);

        // Instructional image (swaps per line)
        var imgObj = new GameObject("HowToImage");
        imgObj.transform.SetParent(cObj.transform, false);
        var iRt = imgObj.AddComponent<RectTransform>();
        iRt.anchorMin = iRt.anchorMax = iRt.pivot = new Vector2(0.5f, 0.5f);
        iRt.anchoredPosition = howToImagePos; iRt.sizeDelta = howToImageSize;
        _htImage = imgObj.AddComponent<Image>();
        _htImage.raycastTarget = false;
        _htImage.preserveAspect = true;

        // Text box (taps pass through to the dim behind it)
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

        // Tap-to-continue indicator
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
        tapTxt.text = SafeGet("intro_tap_continue", "Tik om verder ▶");
        _htTapIndicator = tapObj;

        // "Laten we beginnen!" PNG button (shown on the last line)
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
            _started = true;
        });
        lgObj.SetActive(false);

        _htLines = new (string, string)[]
        {
            ("minigame_raccoon_howto_intro", "De wasbeer heeft een pot met snoepjes gevonden, maar de deksel zit muurvast!"),
            ("minigame_raccoon_howto_line1", "Draai de tablet een kant op, en dan de andere kant."),
            ("minigame_raccoon_howto_line2", "Blijf heen en weer draaien tot de deksel eraf ploft!"),
        };
        _htLineCount = _htLines.Length;
        _htPage = 0;

        ShowHowToPage(0);
    }

    private void AdvanceHowTo()
    {
        if (_htLines == null) return;
        if (_htPage >= _htLineCount - 1) return; // on last line, must press the Lets Go button
        ShowHowToPage(_htPage + 1);
    }

    private void ShowHowToPage(int index)
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

        MinigameVoicePlayer.PlayLocalizedForPage(howToPlayLocalized, _htPage, true);

        bool last = _htPage >= _htLineCount - 1;
        if (_htTapIndicator != null) _htTapIndicator.SetActive(!last);
        if (_htLetsGoBtn != null) _htLetsGoBtn.gameObject.SetActive(last);
    }

    private void ShowCongrats()
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
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 14f);
        accent.AddComponent<Image>().color = new Color(0.95f, 0.65f, 0.2f);

        MakeLabel(card.transform, SafeGet("minigame_complete", "Gefeliciteerd!"),
            new Vector2(0f, -55f), new Vector2(840f, 80f), 56, FontStyle.Bold, Color.white, out _);

        MakeLabel(card.transform, SafeGet("minigame_raccoon_success_title", "Lekkere snoepjes!"),
            new Vector2(0f, -150f), new Vector2(840f, 60f), 36, FontStyle.Normal, Color.white, out _);

        MakeLabel(card.transform,
            SafeGet("minigame_coins_earned", $"Je hebt {coinReward} munten verdiend!"),
            new Vector2(0f, -240f), new Vector2(840f, 60f), 38, FontStyle.Normal, Color.white, out _);

        MakeLabel(card.transform,
            SafeGet("minigame_raccoon_success_desc", "De wasbeer heeft de pot opengekregen!"),
            new Vector2(0f, -310f), new Vector2(840f, 50f), 26, FontStyle.Normal, Color.white, out _);

        var continueBtn = MakeSpriteButton(card.transform, continueButtonSprite, SafeGet("btn_continue", "Doorgaan"), continueButtonPos, continueButtonSize);
        var cbRt = continueBtn.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(0.5f, 0f); cbRt.anchorMax = new Vector2(0.5f, 0f); cbRt.pivot = new Vector2(0.5f, 0f);
        continueBtn.onClick.AddListener(OnContinue);

        StartCoroutine(PopInCard(crt));
    }

    private IEnumerator PopInCard(RectTransform rt)
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

    private void OnContinue()
    {
        GameStateManager.Instance.AddCoins(coinReward);
        SceneManager.LoadScene(returnSceneName);
    }

    private void ExitToMainArea() => SceneManager.LoadScene(returnSceneName);

    private void DestroyMainUI()
    {
        if (_uiCanvas != null) Destroy(_uiCanvas.gameObject);
        _uiCanvas = null;
    }

    private void MakeLabel(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, FontStyle style, Color color, out Text refOut)
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

    private Button MakeSpriteButton(Transform parent, Sprite sprite, string label, Vector2 pos, Vector2 size)
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

    private Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
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

    private string SafeGet(string key, string fallback)
    {
        var lm = LanguageManager.Instance;
        if (lm == null) return fallback;
        var result = lm.Get(key);
        return result == $"[{key}]" ? fallback : result;
    }

    private void EnsureEventSystem()
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