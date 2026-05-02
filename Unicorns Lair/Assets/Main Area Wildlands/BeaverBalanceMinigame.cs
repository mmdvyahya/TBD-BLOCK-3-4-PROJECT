using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BeaverBalanceMinigame : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject beaverObject;
    [SerializeField] private Transform stickObject;
    [SerializeField] private Transform stickPivotPoint;
    [SerializeField] private Camera mainCamera;

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

    private static readonly Vector3 MinigameCamPos = new Vector3(1165.94f, 18.75157f, 1206.586f);
    private static readonly Quaternion MinigameCamRot = Quaternion.Euler(21.349f, 3.162f, 0f);

    private float _currentAngle;
    private float _stableTimeLeft;
    private bool _isRunning;
    private bool _complete;
    private float _balancedZ;
    private float _halfRange;
    private Vector3 _initialStickPos;
    private Quaternion _initialStickRot;

    private Vector3 _originalCamPos;
    private Quaternion _originalCamRot;

    private Canvas _uiCanvas;
    private Text _countdownText;
    private Text _instructionText;
    private Text _statusText;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (beaverObject != null) HideRenderers(beaverObject);
        if (stickObject != null) HideRenderers(stickObject.gameObject);

        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);
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
        if (stable) _stableTimeLeft -= Time.deltaTime;
        _stableTimeLeft = Mathf.Max(0f, _stableTimeLeft);

        UpdateUI(stable);

        if (_stableTimeLeft <= 0f && !_complete)
            StartCoroutine(CompleteSequence());
    }

    public void OpenMinigame()
    {
        if (_isRunning) return;

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

        if (mainCamera == null) mainCamera = Camera.main;

        _originalCamPos = mainCamera.transform.position;
        _originalCamRot = mainCamera.transform.rotation;

        if (beaverObject != null) ShowRenderers(beaverObject);
        if (stickObject != null) ShowRenderers(stickObject.gameObject);

        BuildUI();
        StartCoroutine(MoveCameraAndStart());
    }

    public void CloseMinigame()
    {
        _isRunning = false;
        if (beaverObject != null) HideRenderers(beaverObject);
        if (stickObject != null) HideRenderers(stickObject.gameObject);
        DestroyUI();
        StartCoroutine(ReturnCameraAndNotify());
    }

    IEnumerator ReturnCameraAndNotify()
    {
        yield return StartCoroutine(ReturnCamera());

        var manager = FindFirstObjectByType<MainAreaManager>();
        if (manager != null) manager.NotifyMinigameComplete();
    }

    IEnumerator MoveCameraAndStart()
    {
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float t = 0f, dur = 1.2f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            mainCamera.transform.position = Vector3.Lerp(startPos, MinigameCamPos, p);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, MinigameCamRot, p);
            yield return null;
        }
        mainCamera.transform.position = MinigameCamPos;
        mainCamera.transform.rotation = MinigameCamRot;

        _isRunning = true;
        UpdateUI(false);
    }

    IEnumerator ReturnCamera()
    {
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float t = 0f, dur = 1.0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            mainCamera.transform.position = Vector3.Lerp(startPos, _originalCamPos, p);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, _originalCamRot, p);
            yield return null;
        }
        mainCamera.transform.position = _originalCamPos;
        mainCamera.transform.rotation = _originalCamRot;
    }

    IEnumerator CompleteSequence()
    {
        _complete = true;
        _isRunning = false;

        if (_statusText != null) _statusText.text = "🎉 " + SafeGet("minigame_complete", "Gefeliciteerd!");
        if (_instructionText != null) _instructionText.text = SafeGet("minigame_success_desc", "De bever heeft de stok in balans gehouden!");

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
        cardRt.anchoredPosition = Vector2.zero;
        cardRt.sizeDelta = new Vector2(900f, 560f);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.08f, 0.12f, 0.20f, 0.96f);
        cardImg.raycastTarget = false;

        var accentTop = new GameObject("AccentTop");
        accentTop.transform.SetParent(card.transform, false);
        var atRt = accentTop.AddComponent<RectTransform>();
        atRt.anchorMin = new Vector2(0f, 1f); atRt.anchorMax = new Vector2(1f, 1f);
        atRt.pivot = new Vector2(0.5f, 1f); atRt.anchoredPosition = Vector2.zero; atRt.sizeDelta = new Vector2(0f, 12f);
        accentTop.AddComponent<Image>().color = new Color(1f, 0.82f, 0.2f);

        MakeCongratsLabel(card.transform, "🎉", new Vector2(0f, -50f), new Vector2(860f, 100f), 80, FontStyle.Normal, new Color(1f, 0.85f, 0.2f));
        MakeCongratsLabelLocalized(card.transform, "minigame_complete", "Gefeliciteerd!",
            new Vector2(0f, -155f), new Vector2(860f, 80f), 60, FontStyle.Bold, Color.white);
        MakeCongratsLabelLocalized(card.transform, "minigame_coins_earned", "Je hebt 10 munten verdiend!",
            new Vector2(0f, -250f), new Vector2(860f, 60f), 40, FontStyle.Normal, new Color(0.35f, 1f, 0.60f));
        MakeCongratsLabelLocalized(card.transform, "minigame_success_desc", "De bever heeft de stok in balans gehouden!",
            new Vector2(0f, -320f), new Vector2(860f, 50f), 28, FontStyle.Normal, new Color(0.75f, 0.88f, 1f));

        var continueBtn = new GameObject("ContinueBtn");
        continueBtn.transform.SetParent(card.transform, false);
        var cbrt = continueBtn.AddComponent<RectTransform>();
        cbrt.anchorMin = cbrt.anchorMax = cbrt.pivot = new Vector2(0.5f, 0f);
        cbrt.anchoredPosition = new Vector2(0f, 36f);
        cbrt.sizeDelta = new Vector2(500f, 120f);
        var cImg = continueBtn.AddComponent<Image>();
        cImg.color = new Color(0.12f, 0.68f, 0.34f);
        var cBtn = continueBtn.AddComponent<Button>();
        cBtn.targetGraphic = cImg;
        cBtn.colors = new ColorBlock
        {
            normalColor = new Color(0.12f, 0.68f, 0.34f),
            highlightedColor = new Color(0.20f, 0.85f, 0.46f),
            pressedColor = new Color(0.07f, 0.46f, 0.22f),
            selectedColor = new Color(0.12f, 0.68f, 0.34f),
            disabledColor = new Color(0.35f, 0.35f, 0.35f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        GameObject capturedCanvas = cObj;
        cBtn.onClick.AddListener(() =>
        {
            GameStateManager.Ensure();
            GameStateManager.Instance.AddCoins(10);
            Destroy(capturedCanvas);
            CloseMinigame();
        });

        var clbl = new GameObject("Label");
        clbl.transform.SetParent(continueBtn.transform, false);
        var clrt = clbl.AddComponent<RectTransform>();
        clrt.anchorMin = Vector2.zero; clrt.anchorMax = Vector2.one;
        clrt.offsetMin = clrt.offsetMax = Vector2.zero;
        var ctxt = clbl.AddComponent<Text>();
        ctxt.text = SafeGet("btn_continue", "Doorgaan");
        ctxt.font = GetFont(); ctxt.fontSize = 50;
        ctxt.fontStyle = FontStyle.Bold;
        ctxt.alignment = TextAnchor.MiddleCenter;
        ctxt.color = Color.white; ctxt.raycastTarget = false;
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
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = obj.AddComponent<Text>();
        t.text = text; t.font = GetFont(); t.fontSize = fontSize;
        t.fontStyle = style; t.color = color;
        t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
    }

    void MakeCongratsLabelLocalized(Transform parent, string key, string fallback, Vector2 pos, Vector2 size,
                                     int fontSize, FontStyle style, Color color)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = obj.AddComponent<Text>();
        t.font = GetFont(); t.fontSize = fontSize; t.fontStyle = style; t.color = color;
        t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
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
        hrt.anchorMin = new Vector2(0f, 1f); hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f); hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(0f, 140f);
        header.AddComponent<Image>().color = new Color(0.08f, 0.12f, 0.20f, 0.90f);

        MakeLabel(header.transform, SafeGet("minigame_beaver_title", "🦫  Bever Balans!"),
            new Vector2(0f, -14f), new Vector2(1000f, 70f), 52, FontStyle.Bold, Color.white, ref _statusText);

        string instrKey = allowKeyboardDebug ? "minigame_instruction_pc" : "minigame_instruction_tablet";
        MakeLabel(header.transform, SafeGet(instrKey, allowKeyboardDebug ? "Druk A / D om te kantelen" : "Kantel de tablet!"),
            new Vector2(0f, -86f), new Vector2(1000f, 44f), 32, FontStyle.Normal,
            new Color(0.80f, 0.92f, 1f), ref _instructionText);

        var countdownBg = new GameObject("CountdownBg");
        countdownBg.transform.SetParent(cObj.transform, false);
        var cbrt = countdownBg.AddComponent<RectTransform>();
        cbrt.anchorMin = new Vector2(0.5f, 0.5f); cbrt.anchorMax = new Vector2(0.5f, 0.5f);
        cbrt.pivot = new Vector2(0.5f, 0.5f); cbrt.anchoredPosition = new Vector2(0f, 320f);
        cbrt.sizeDelta = new Vector2(440f, 110f);
        countdownBg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        MakeLabel(countdownBg.transform, "", Vector2.zero, new Vector2(420f, 100f), 58, FontStyle.Bold,
            Color.white, ref _countdownText);

        var stopBtn = new GameObject("StopBtn");
        stopBtn.transform.SetParent(cObj.transform, false);
        var sbrt = stopBtn.AddComponent<RectTransform>();
        sbrt.anchorMin = new Vector2(0f, 0f); sbrt.anchorMax = new Vector2(0f, 0f);
        sbrt.pivot = new Vector2(0f, 0f); sbrt.anchoredPosition = new Vector2(30f, 30f);
        sbrt.sizeDelta = new Vector2(240f, 110f);
        var sImg = stopBtn.AddComponent<Image>();
        sImg.color = new Color(0.55f, 0.18f, 0.18f);
        var sBtn = stopBtn.AddComponent<Button>();
        sBtn.targetGraphic = sImg;
        sBtn.colors = new ColorBlock
        {
            normalColor = new Color(0.55f, 0.18f, 0.18f),
            highlightedColor = new Color(0.72f, 0.25f, 0.25f),
            pressedColor = new Color(0.38f, 0.10f, 0.10f),
            selectedColor = new Color(0.55f, 0.18f, 0.18f),
            disabledColor = new Color(0.3f, 0.3f, 0.3f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        sBtn.onClick.AddListener(CloseMinigame);

        var sLbl = new GameObject("Label");
        sLbl.transform.SetParent(stopBtn.transform, false);
        var slrt = sLbl.AddComponent<RectTransform>();
        slrt.anchorMin = Vector2.zero; slrt.anchorMax = Vector2.one;
        slrt.offsetMin = slrt.offsetMax = Vector2.zero;
        var sTxt = sLbl.AddComponent<Text>();
        sTxt.font = GetFont(); sTxt.fontSize = 42;
        sTxt.fontStyle = FontStyle.Bold; sTxt.alignment = TextAnchor.MiddleCenter;
        sTxt.color = Color.white; sTxt.raycastTarget = false;
        var stopLoc = sLbl.AddComponent<LocalizedText>();
        stopLoc.key = "btn_back";
        stopLoc.Refresh();
    }

    void RefreshStaticUI()
    {
        if (_instructionText != null)
        {
            string instrKey = allowKeyboardDebug ? "minigame_instruction_pc" : "minigame_instruction_tablet";
            _instructionText.text = SafeGet(instrKey, allowKeyboardDebug ? "Druk A / D om te kantelen" : "Kantel de tablet!");
        }
        if (_statusText != null && !_complete)
            _statusText.text = SafeGet("minigame_beaver_title", "🦫  Bever Balans!");
    }

    void MakeLabel(Transform parent, string text, Vector2 pos, Vector2 size,
                   int fontSize, FontStyle style, Color color, ref Text refTarget)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = obj.AddComponent<Text>();
        t.text = text; t.font = GetFont(); t.fontSize = fontSize;
        t.fontStyle = style; t.color = color;
        t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        refTarget = t;
    }

    void UpdateUI(bool stable)
    {
        if (_countdownText == null) return;
        _countdownText.text = _stableTimeLeft.ToString("F1") + "s";
        _countdownText.color = stable ? new Color(0.25f, 1f, 0.45f) : Color.white;

        if (_statusText != null && !_complete)
            _statusText.text = stable ? "⚖️  In balans!" : "🦫  Bever Balans!";
    }

    void DestroyUI()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= RefreshStaticUI;
        if (_uiCanvas != null) { Destroy(_uiCanvas.gameObject); _uiCanvas = null; }
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
        if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        return _font;
    }
}