using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BeaverBalanceMinigame : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject beaverObject;
    [SerializeField] private Transform stickObject;
    [SerializeField] private Transform stickPivotPoint;

    [Header("Scene")]
    [Tooltip("Name of the scene to load when the minigame finishes or is closed.")]
    [SerializeField] private string returnSceneName = "MainArea";

    [Header("Reward")]
    [Tooltip("How many coins the player earns when they win.")]
    [SerializeField] private int coinReward = 10;

    [Header("Stick Rotation (from Inspector values)")]
    [SerializeField] private float stickRotX = 11.46f;
    [SerializeField] private float stickRotY = -3.982f;
    [SerializeField] private float stickMaxLeftZ = 124.369f;
    [SerializeField] private float stickMaxRightZ = 35.185f;
    [SerializeField] private float stickMoveSpeed = 80f;
    [SerializeField] private float instabilitySpeed = 8f;

    [Header("Balance Rules")]
    [SerializeField] private float balanceZoneDegrees = 6f;
    [SerializeField] private float stableTimeRequired = 5f;

    [Header("Desktop Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;
    [SerializeField] private float keyboardTiltSpeed = 1.5f;

    [Header("Tablet Input")]
    [SerializeField] private bool useAccelerometer = true;
    [SerializeField] private float accelerometerMultiplier = 35f;
    [SerializeField] private float accelerometerDeadZone = 0.05f;

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
    [SerializeField] private Vector2 congratsPanelSize = new Vector2(900f, 560f);
    [Range(0f, 1f)]
    [SerializeField] private float congratsPanelOpacity = 1f;

    [Header("Continue Button (PNG)")]
    [SerializeField] private Sprite continueButtonSprite;
    [SerializeField] private Vector2 continueButtonPos = new Vector2(0f, 36f);
    [SerializeField] private Vector2 continueButtonSize = new Vector2(500f, 120f);

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

    [Header("How To Play - Lets Go Button (PNG)")]
    [SerializeField] private Sprite letsGoButtonSprite;
    [SerializeField] private Vector2 letsGoButtonPos = new Vector2(0f, -760f);
    [SerializeField] private Vector2 letsGoButtonSize = new Vector2(480f, 170f);

    [Header("How To Play Voice")]
    [SerializeField] private LocalizedSoundData howToPlayLocalized;

    [Header("How To Play - Background")]
    [Range(0f, 1f)]
    [SerializeField] private float howToDimOpacity = 0.78f;

    private float _currentAngle;
    private float _stableTimeLeft;
    private bool _isRunning;
    private bool _complete;
    private bool _hasStartedBefore;

    private float _balancedZ;
    private float _halfRange;
    private Vector3 _initialStickPos;
    private Quaternion _initialStickRot;

    private Canvas _uiCanvas;
    private Text _countdownText;
    private Text _instructionText;
    private Text _statusText;
    private GameObject _howToCanvas;

    private (string key, string fallback)[] _htLines;
    private int _htPage;
    private int _htLineCount;
    private Text _htText;
    private Image _htImage;
    private GameObject _htTapIndicator;
    private Button _htLetsGoBtn;
    // voice handled by MinigameVoicePlayer

    void Start()
    {
        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);

        LanguageManager.Ensure();

        if (showHowToPlay) ShowHowToPlay();
        else OpenMinigame();
    }

    void Update()
    {
        if (!_isRunning) return;

        float inputTilt = 0f;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (allowKeyboardDebug && Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) inputTilt -= keyboardTiltSpeed;
            if (Keyboard.current.dKey.isPressed) inputTilt += keyboardTiltSpeed;
        }
#endif

        if (useAccelerometer && Accelerometer.current != null)
        {
            float ax = Accelerometer.current.acceleration.ReadValue().x;
            if (Mathf.Abs(ax) > accelerometerDeadZone)
                inputTilt += ax * accelerometerMultiplier * Time.deltaTime;
        }

        _currentAngle += instabilitySpeed * Time.deltaTime;
        _currentAngle += inputTilt * stickMoveSpeed * Time.deltaTime;
        _currentAngle = Mathf.Clamp(_currentAngle, -_halfRange, _halfRange);

        if (stickObject != null)
        {
            if (stickPivotPoint != null)
            {
                stickObject.position = _initialStickPos;
                stickObject.rotation = _initialStickRot;
                stickObject.RotateAround(stickPivotPoint.position, stickPivotPoint.forward, _currentAngle);
            }
            else
            {
                stickObject.localEulerAngles = new Vector3(stickRotX, stickRotY, _balancedZ + _currentAngle);
            }
        }

        bool stable = Mathf.Abs(_currentAngle) <= balanceZoneDegrees;

        if (stable)
            _stableTimeLeft -= Time.deltaTime;

        _stableTimeLeft = Mathf.Max(0f, _stableTimeLeft);

        UpdateUI(stable);

        if (_stableTimeLeft <= 0f && !_complete)
            StartCoroutine(CompleteSequence());
    }

    public void OpenMinigame()
    {
        if (_isRunning) return;

        if (_hasStartedBefore)
        {
            PlaytestLogger.Instance?.LogMinigameRetry("BeaverBalance");
        }

        _hasStartedBefore = true;

        _balancedZ = (stickMaxLeftZ + stickMaxRightZ) * 0.5f;
        _halfRange = (stickMaxLeftZ - stickMaxRightZ) * 0.5f;
        _currentAngle = 0f;
        _stableTimeLeft = stableTimeRequired;
        _complete = false;

        if (stickObject != null)
        {
            _initialStickPos = stickObject.position;
            _initialStickRot = stickObject.rotation;
        }

        if (beaverObject != null)
            ShowRenderers(beaverObject);

        if (stickObject != null)
            ShowRenderers(stickObject.gameObject);

        BuildUI();

        _isRunning = true;

        UpdateUI(false);
    }

    public void CloseMinigame()
    {
        if (!_complete)
        {
            PlaytestLogger.Instance?.LogMinigameFail(
                "BeaverBalance",
                "Player exited before completion"
            );
        }

        _isRunning = false;

        if (beaverObject != null)
            HideRenderers(beaverObject);

        if (stickObject != null)
            HideRenderers(stickObject.gameObject);

        DestroyUI();

        SceneManager.LoadScene(returnSceneName);
    }

    IEnumerator CompleteSequence()
    {
        DeviceVibration.Vibrate();
        _complete = true;
        _isRunning = false;

        PlaytestLogger.Instance?.LogMinigameSuccess("BeaverBalance");

        if (_statusText != null)
            _statusText.text = SafeGet("minigame_complete", "Gefeliciteerd!");

        if (_instructionText != null)
            _instructionText.text = SafeGet("minigame_success_desc", "De bever heeft de stok in balans gehouden!");

        yield return new WaitForSeconds(1.5f);

        DestroyUI();
        ShowCongratsScreen();
    }

    void ShowCongratsScreen()
    {
        var cObj = new GameObject("CongratsCanvas");
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
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        bg.raycastTarget = false;

        var card = new GameObject("Card");
        card.transform.SetParent(cObj.transform, false);

        var cardRt = card.AddComponent<RectTransform>();
        cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = congratsPanelPos;
        cardRt.sizeDelta = congratsPanelSize;

        var cardImg = card.AddComponent<Image>();
        if (congratsPanelSprite != null)
        {
            cardImg.sprite = congratsPanelSprite;
            cardImg.type = Image.Type.Simple;
            cardImg.preserveAspect = false;
            cardImg.color = new Color(1f, 1f, 1f, congratsPanelOpacity);
        }
        else cardImg.color = new Color(0.08f, 0.12f, 0.20f, 0.96f);
        cardImg.raycastTarget = false;

        var accentTop = new GameObject("AccentTop");
        accentTop.transform.SetParent(card.transform, false);

        var atRt = accentTop.AddComponent<RectTransform>();
        atRt.anchorMin = new Vector2(0f, 1f);
        atRt.anchorMax = new Vector2(1f, 1f);
        atRt.pivot = new Vector2(0.5f, 1f);
        atRt.anchoredPosition = Vector2.zero;
        atRt.sizeDelta = new Vector2(0f, 12f);

        accentTop.AddComponent<Image>().color = new Color(1f, 0.82f, 0.2f);

        MakeCongratsLabel(card.transform, "", new Vector2(0f, -50f), new Vector2(860f, 100f), 80, FontStyle.Normal, Color.white);

        MakeCongratsLabelLocalized(card.transform, "minigame_complete", "Gefeliciteerd!",
            new Vector2(0f, -155f), new Vector2(860f, 80f), 60, FontStyle.Bold, Color.white);

        MakeCongratsLabelLocalized(card.transform, "minigame_coins_earned", "Je hebt 10 munten verdiend!",
            new Vector2(0f, -250f), new Vector2(860f, 60f), 40, FontStyle.Normal, Color.white);

        MakeCongratsLabelLocalized(card.transform, "minigame_success_desc", "De bever heeft de stok in balans gehouden!",
            new Vector2(0f, -320f), new Vector2(860f, 50f), 28, FontStyle.Normal, Color.white);

        var continueBtn = new GameObject("ContinueBtn");
        continueBtn.transform.SetParent(card.transform, false);

        var cbrt = continueBtn.AddComponent<RectTransform>();
        cbrt.anchorMin = cbrt.anchorMax = cbrt.pivot = new Vector2(0.5f, 0f);
        cbrt.anchoredPosition = continueButtonPos;
        cbrt.sizeDelta = continueButtonSize;

        var cImg = continueBtn.AddComponent<Image>();
        Color cBase;
        if (continueButtonSprite != null)
        {
            cImg.sprite = continueButtonSprite;
            cImg.type = Image.Type.Simple;
            cImg.preserveAspect = true;
            cImg.color = Color.white;
            cBase = Color.white;
        }
        else
        {
            cBase = new Color(0.12f, 0.68f, 0.34f);
            cImg.color = cBase;
        }

        var cBtn = continueBtn.AddComponent<Button>();
        cBtn.targetGraphic = cImg;
        cBtn.colors = new ColorBlock
        {
            normalColor = cBase,
            highlightedColor = cBase * 1.12f,
            pressedColor = cBase * 0.8f,
            selectedColor = cBase,
            disabledColor = new Color(0.35f, 0.35f, 0.35f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        GameObject capturedCanvas = cObj;

        cBtn.onClick.AddListener(() =>
        {
            GameStateManager.Ensure();
            GameStateManager.Instance.AddCoins(coinReward);
            Destroy(capturedCanvas);
            CloseMinigame();
        });

        var clbl = new GameObject("Label");
        clbl.transform.SetParent(continueBtn.transform, false);

        var clrt = clbl.AddComponent<RectTransform>();
        clrt.anchorMin = Vector2.zero;
        clrt.anchorMax = Vector2.one;
        clrt.offsetMin = clrt.offsetMax = Vector2.zero;

        var ctxt = clbl.AddComponent<Text>();
        ctxt.text = SafeGet("btn_continue", "Doorgaan");
        ctxt.font = GetFont();
        ctxt.fontSize = 50;
        ctxt.fontStyle = FontStyle.Bold;
        ctxt.alignment = TextAnchor.MiddleCenter;
        ctxt.color = Color.white;
        ctxt.raycastTarget = false;

        var ctxtLoc = clbl.AddComponent<LocalizedText>();
        ctxtLoc.key = "btn_continue";
        ctxtLoc.Refresh();
    }

    void MakeCongratsLabel(Transform parent, string text, Vector2 pos, Vector2 size,
                            int fontSize, FontStyle style, Color color)
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
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
    }

    void MakeCongratsLabelLocalized(Transform parent, string key, string fallback, Vector2 pos, Vector2 size,
                                     int fontSize, FontStyle style, Color color)
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
        t.font = GetFont();
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.text = SafeGet(key, fallback);

        var loc = obj.AddComponent<LocalizedText>();
        loc.key = key;
        loc.Refresh();
    }

    string SafeGet(string key, string fallback)
    {
        var lm = LanguageManager.Instance;
        if (lm == null) return fallback;

        var result = lm.Get(key);

        return (result == $"[{key}]") ? fallback : result;
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
            OpenMinigame();
        });
        lgObj.SetActive(false);

        _htLines = new (string, string)[]
        {
            ("minigame_beaver_howto_intro", "De bever balanceert een stok. Help hem om hem recht te houden!"),
            ("minigame_beaver_howto_line1", "Kantel de tablet zachtjes naar links en rechts."),
            ("minigame_beaver_howto_line2", "Houd de stok in het midden tot de tijd op is!"),
        };
        _htLineCount = _htLines.Length;
        _htPage = 0;

        ShowHowToPage(0);
    }

    void AdvanceHowTo()
    {
        if (_htLines == null) return;
        if (_htPage >= _htLineCount - 1) return; // on last line, must press the Lets Go button
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
        // Delegate voice playback to the shared minigame voice player
        MinigameVoicePlayer.PlayLocalizedForPage(howToPlayLocalized, _htPage, true);
        bool last = _htPage >= _htLineCount - 1;
        if (_htTapIndicator != null) _htTapIndicator.SetActive(!last);
        if (_htLetsGoBtn != null) _htLetsGoBtn.gameObject.SetActive(last);
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

    void MakeHowToLabel(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = obj.AddComponent<Text>();
        t.text = text; t.font = GetFont(); t.fontSize = fontSize; t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter; t.color = color; t.raycastTarget = false;
    }

    void MakeHowToRow(Transform parent, float y, string text)
    {
        var row = new GameObject("Row");
        row.transform.SetParent(parent, false);
        var rRt = row.AddComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0.5f, 1f); rRt.anchorMax = new Vector2(0.5f, 1f);
        rRt.pivot = new Vector2(0.5f, 1f); rRt.anchoredPosition = new Vector2(0f, y); rRt.sizeDelta = new Vector2(820f, 90f);
        row.AddComponent<Image>().color = new Color(0.40f, 0.28f, 0.14f, 0.85f);

        var lObj = new GameObject("Label");
        lObj.transform.SetParent(row.transform, false);
        var lrt = lObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(20f, 0f); lrt.offsetMax = new Vector2(-20f, 0f);
        var t = lObj.AddComponent<Text>();
        t.text = text; t.font = GetFont(); t.fontSize = 28; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
    }

    Button MakeHowToButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
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

    void BuildUI()
    {
        var cObj = new GameObject("MinigameCanvas");

        _uiCanvas = cObj.AddComponent<Canvas>();
        _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _uiCanvas.sortingOrder = 20;

        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        cObj.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();
        LanguageManager.Ensure();

        LanguageManager.Instance.LanguageChanged += RefreshStaticUI;

        var header = new GameObject("Header");
        header.transform.SetParent(cObj.transform, false);

        var hrt = header.AddComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0f, 1f);
        hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(0f, 140f);

        header.AddComponent<Image>().color = new Color(0.08f, 0.12f, 0.20f, 0.90f);

        MakeLabel(header.transform, SafeGet("minigame_beaver_title", "Bever Balans!"),
            new Vector2(0f, -14f), new Vector2(1000f, 70f), 52, FontStyle.Bold, Color.white, ref _statusText);

        string instrKey = allowKeyboardDebug ? "minigame_instruction_pc" : "minigame_instruction_tablet";

        MakeLabel(header.transform, SafeGet(instrKey, allowKeyboardDebug ? "Kantel de tablet!" : "Kantel de tablet!"),
            new Vector2(0f, -86f), new Vector2(1000f, 44f), 32, FontStyle.Normal,
            new Color(0.80f, 0.92f, 1f), ref _instructionText);

        var countdownBg = new GameObject("CountdownBg");
        countdownBg.transform.SetParent(cObj.transform, false);

        var cbrt = countdownBg.AddComponent<RectTransform>();
        cbrt.anchorMin = new Vector2(0.5f, 0.5f);
        cbrt.anchorMax = new Vector2(0.5f, 0.5f);
        cbrt.pivot = new Vector2(0.5f, 0.5f);
        cbrt.anchoredPosition = new Vector2(0f, 320f);
        cbrt.sizeDelta = new Vector2(440f, 110f);

        countdownBg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        MakeLabel(countdownBg.transform, "", Vector2.zero, new Vector2(420f, 100f), 58, FontStyle.Bold,
            Color.white, ref _countdownText);

        var stopBtn = new GameObject("StopBtn");
        stopBtn.transform.SetParent(cObj.transform, false);

        var sbrt = stopBtn.AddComponent<RectTransform>();
        sbrt.anchorMin = new Vector2(0f, 0f);
        sbrt.anchorMax = new Vector2(0f, 0f);
        sbrt.pivot = new Vector2(0f, 0f);
        sbrt.anchoredPosition = backButtonPos;
        sbrt.sizeDelta = backButtonSize;

        var sImg = stopBtn.AddComponent<Image>();
        Color sBase;
        if (backButtonSprite != null)
        {
            sImg.sprite = backButtonSprite;
            sImg.type = Image.Type.Simple;
            sImg.preserveAspect = true;
            sImg.color = Color.white;
            sBase = Color.white;
        }
        else
        {
            sBase = new Color(0.55f, 0.18f, 0.18f);
            sImg.color = sBase;
        }

        var sBtn = stopBtn.AddComponent<Button>();
        sBtn.targetGraphic = sImg;
        sBtn.colors = new ColorBlock
        {
            normalColor = sBase,
            highlightedColor = sBase * 1.12f,
            pressedColor = sBase * 0.8f,
            selectedColor = sBase,
            disabledColor = new Color(0.3f, 0.3f, 0.3f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        sBtn.onClick.AddListener(CloseMinigame);
    }

    void RefreshStaticUI()
    {
        if (_instructionText != null)
        {
            string instrKey = allowKeyboardDebug ? "minigame_instruction_pc" : "minigame_instruction_tablet";
            _instructionText.text = SafeGet(instrKey, allowKeyboardDebug ? "Kantel de tablet!" : "Kantel de tablet!");
        }

        if (_statusText != null && !_complete)
            _statusText.text = SafeGet("minigame_beaver_title", "Bever Balans!");
    }

    void MakeLabel(Transform parent, string text, Vector2 pos, Vector2 size,
                   int fontSize, FontStyle style, Color color, ref Text refTarget)
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
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;

        refTarget = t;
    }

    void UpdateUI(bool stable)
    {
        if (_countdownText == null) return;

        _countdownText.text = _stableTimeLeft.ToString("F1") + "s";
        _countdownText.color = stable ? new Color(0.25f, 1f, 0.45f) : Color.white;

        if (_statusText != null && !_complete)
            _statusText.text = stable ? "In balans!" : "Bever Balans!";
    }

    void DestroyUI()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= RefreshStaticUI;

        if (_uiCanvas != null)
        {
            Destroy(_uiCanvas.gameObject);
            _uiCanvas = null;
        }
    }

    static void HideRenderers(GameObject obj)
    {
        foreach (var r in obj.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
    }

    static void ShowRenderers(GameObject obj)
    {
        foreach (var r in obj.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;
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

        if (_font == null)
            _font = Font.CreateDynamicFontFromOSFont("Arial", 24);

        return _font;
    }
}