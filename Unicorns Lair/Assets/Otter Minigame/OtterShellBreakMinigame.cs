using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OtterShellBreakMinigame : MonoBehaviour
{
    public enum ShakeAxis { X, Y, Z }

    [Header("Scene Objects")]
    [SerializeField] private GameObject otterModel;

    [Header("Shell Stages")]
    [Tooltip("Shell stage objects already placed in the scene, each at its correct position: 0 = closed, 1 = small crack, 2 = medium, 3 = big, 4 = broken. The script shows one and hides the rest.")]
    [SerializeField] private GameObject[] shellStages;

    [Header("Game Settings")]
    [SerializeField] private int shakesNeeded = 5;
    [SerializeField] private float finishDelay = 1.5f;

    [Header("Settings")]
    [Tooltip("Show a kid-friendly 'How to Play' explanation before the game starts.")]
    [SerializeField] private bool showHowToPlay = true;

    [Header("Reward")]
    [Tooltip("How many coins the player earns when they win.")]
    [SerializeField] private int coinReward = 10;
    [Tooltip("Name of the scene to load when the minigame finishes or is closed.")]
    [SerializeField] private string returnSceneName = "MainArea";

    [Header("Shake Settings")]
    [SerializeField] private float forwardShakeThreshold = 1.25f;
    [SerializeField] private float shakeCooldown = 0.35f;
    [SerializeField] private ShakeAxis forwardAxis = ShakeAxis.Z;
    [SerializeField] private bool invertAxis = false;

    [Header("Animation")]
    [SerializeField] private Animator otterAnimator;
    [SerializeField] private string anticipationTrigger = "Anticipate";
    [SerializeField] private string happyTrigger = "Happy";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip crackSound;
    [SerializeField] private AudioClip completeSound;

    [Header("PC Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;
    [SerializeField] private Key debugShakeKey = Key.Space;

    private int currentShakes;
    private float lastShakeTime = -999f;
    private bool isRunning;
    private bool isCompleted;

    private Vector3 lastAcceleration;
    private bool hasLastAcceleration;

    private Canvas _uiCanvas;
    private Text _titleText;
    private Text _hintText;
    private GameObject _howToCanvas;
    private GameObject _congratsCanvas;
    private Image[] _pips;

    private void Start()
    {
        LanguageManager.Ensure();
        GameStateManager.Ensure();

        BuildUI();

        if (otterModel != null) otterModel.SetActive(true);
        ShowShellStage(0);
        UpdateProgressUI();

        if (showHowToPlay) ShowHowToPlay();
        else StartMinigame();

        LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        if (_titleText != null) _titleText.text = SafeGet("minigame_otter_title", "Kraak de Schelp!");
        if (_hintText != null && !isCompleted) _hintText.text = SafeGet("minigame_otter_instruction", "Schud de tablet om de schelp te kraken!");
    }

    private void OnEnable()
    {
        if (Accelerometer.current != null) InputSystem.EnableDevice(Accelerometer.current);
    }

    private void OnDisable()
    {
        if (Accelerometer.current != null) InputSystem.DisableDevice(Accelerometer.current);
    }

    private void Update()
    {
        if (!isRunning || isCompleted)
            return;

        DetectForwardShake();
        DetectDebugInput();
    }

    private void StartMinigame()
    {
        currentShakes = 0;
        lastShakeTime = -999f;
        isRunning = true;
        isCompleted = false;
        hasLastAcceleration = false;

        if (otterModel != null) otterModel.SetActive(true);

        ShowShellStage(0);
        if (_hintText != null) _hintText.text = SafeGet("minigame_otter_instruction", "Schud de tablet om de schelp te kraken!");
        UpdateProgressUI();
    }

    private void DetectForwardShake()
    {
        if (Accelerometer.current == null) return;

        Vector3 acceleration = Accelerometer.current.acceleration.ReadValue();

        if (!hasLastAcceleration)
        {
            lastAcceleration = acceleration;
            hasLastAcceleration = true;
            return;
        }

        Vector3 delta = acceleration - lastAcceleration;
        lastAcceleration = acceleration;

        float forwardValue = GetAxisValue(delta);
        if (invertAxis) forwardValue *= -1f;

        if (forwardValue >= forwardShakeThreshold && Time.time >= lastShakeTime + shakeCooldown)
            RegisterShake();
    }

    private void DetectDebugInput()
    {
        if (!allowKeyboardDebug) return;
        if (Keyboard.current == null) return;
        if (Keyboard.current[debugShakeKey].wasPressedThisFrame) RegisterShake();
    }

    private void RegisterShake()
    {
        if (isCompleted) return;

        lastShakeTime = Time.time;
        currentShakes++;

        PlaySound(crackSound);
        PlayOtterAnticipation();
        UpdateShellVisual();
        UpdateProgressUI();

        if (currentShakes >= shakesNeeded)
            CompleteMinigame();
    }

    private void UpdateShellVisual()
    {
        if (shellStages == null || shellStages.Length == 0) return;

        float progress = Mathf.Clamp01((float)currentShakes / shakesNeeded);
        int lastStageIndex = shellStages.Length - 1;
        int stageIndex = Mathf.Clamp(Mathf.FloorToInt(progress * lastStageIndex), 0, lastStageIndex);
        ShowShellStage(stageIndex);
    }

    private void ShowShellStage(int index)
    {
        if (shellStages == null) return;
        for (int i = 0; i < shellStages.Length; i++)
            if (shellStages[i] != null)
                shellStages[i].SetActive(i == index);
    }

    private void CompleteMinigame()
    {
        isCompleted = true;
        isRunning = false;

        if (shellStages != null && shellStages.Length > 0)
            ShowShellStage(shellStages.Length - 1);

        if (_hintText != null) _hintText.text = SafeGet("minigame_complete", "Gefeliciteerd!");

        PlaySound(completeSound);
        PlayOtterHappy();

        StartCoroutine(FinishRoutine());
    }

    private IEnumerator FinishRoutine()
    {
        yield return new WaitForSeconds(finishDelay);
        DestroyMainUI();
        ShowCongrats();
    }

    private void UpdateProgressUI()
    {
        if (_pips == null) return;
        for (int i = 0; i < _pips.Length; i++)
        {
            if (_pips[i] == null) continue;
            _pips[i].color = i < currentShakes
                ? new Color(0.35f, 0.75f, 0.95f)
                : new Color(1f, 1f, 1f, 0.25f);
        }
    }

    private float GetAxisValue(Vector3 value)
    {
        switch (forwardAxis)
        {
            case ShakeAxis.X: return value.x;
            case ShakeAxis.Y: return value.y;
            case ShakeAxis.Z: return value.z;
            default: return value.z;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    private void PlayOtterAnticipation()
    {
        if (otterAnimator != null && !string.IsNullOrEmpty(anticipationTrigger))
            otterAnimator.SetTrigger(anticipationTrigger);
    }

    private void PlayOtterHappy()
    {
        if (otterAnimator != null && !string.IsNullOrEmpty(happyTrigger))
            otterAnimator.SetTrigger(happyTrigger);
    }

    // ---------- UI ----------

    private void BuildUI()
    {
        var cObj = new GameObject("OtterCanvas");
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
        header.AddComponent<Image>().color = new Color(0.08f, 0.14f, 0.18f, 0.92f);

        MakeLabel(header.transform, SafeGet("minigame_otter_title", "Kraak de Schelp!"),
            new Vector2(0f, -16f), new Vector2(1000f, 70f), 50, FontStyle.Bold,
            new Color(0.6f, 0.9f, 1f), out _titleText);

        MakeLabel(header.transform, SafeGet("minigame_otter_instruction", "Schud de tablet om de schelp te kraken!"),
            new Vector2(0f, -100f), new Vector2(1000f, 60f), 28, FontStyle.Normal,
            new Color(0.9f, 0.97f, 1f), out _hintText);

        var pipRow = new GameObject("PipRow");
        pipRow.transform.SetParent(header.transform, false);
        var prRt = pipRow.AddComponent<RectTransform>();
        prRt.anchorMin = new Vector2(0.5f, 1f); prRt.anchorMax = new Vector2(0.5f, 1f);
        prRt.pivot = new Vector2(0.5f, 1f); prRt.anchoredPosition = new Vector2(0f, 160f - 320f);
        prRt.sizeDelta = new Vector2(1000f, 60f);

        int n = Mathf.Max(1, shakesNeeded);
        _pips = new Image[n];
        float pipSize = 42f;
        float spacing = 22f;
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

        var stopBtn = MakeButton(cObj.transform, SafeGet("btn_back", "Stop"),
            new Vector2(30f, 30f), new Vector2(240f, 110f), new Color(0.55f, 0.18f, 0.18f));
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
        cImg.color = new Color(0.08f, 0.14f, 0.18f, 0.98f);

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 14f);
        accent.AddComponent<Image>().color = new Color(0.35f, 0.75f, 0.95f);

        MakeLabel(card.transform, SafeGet("minigame_otter_howto_title", "Hoe speel je?"),
            new Vector2(0f, -40f), new Vector2(840f, 80f), 54, FontStyle.Bold, new Color(0.6f, 0.9f, 1f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_otter_howto_intro", "De otter heeft een schelp met een lekker hapje erin gevonden, maar hij zit potdicht!"),
            new Vector2(0f, -150f), new Vector2(820f, 120f), 30, FontStyle.Normal, new Color(0.92f, 0.97f, 1f), out _);

        var row1 = new GameObject("Line1");
        row1.transform.SetParent(card.transform, false);
        var r1Rt = row1.AddComponent<RectTransform>();
        r1Rt.anchorMin = new Vector2(0.5f, 1f); r1Rt.anchorMax = new Vector2(0.5f, 1f);
        r1Rt.pivot = new Vector2(0.5f, 1f); r1Rt.anchoredPosition = new Vector2(0f, -300f); r1Rt.sizeDelta = new Vector2(820f, 90f);
        row1.AddComponent<Image>().color = new Color(0.16f, 0.34f, 0.46f, 0.85f);
        MakeLabel(row1.transform,
            SafeGet("minigame_otter_howto_line1", "Schud de tablet om de schelp te kraken."),
            Vector2.zero, new Vector2(780f, 80f), 28, FontStyle.Bold, Color.white, out var l1);
        var l1Rt = l1.rectTransform; l1Rt.anchorMin = Vector2.zero; l1Rt.anchorMax = Vector2.one;
        l1Rt.offsetMin = new Vector2(20f, 0f); l1Rt.offsetMax = new Vector2(-20f, 0f); l1Rt.pivot = new Vector2(0.5f, 0.5f);
        l1.alignment = TextAnchor.MiddleCenter;

        var row2 = new GameObject("Line2");
        row2.transform.SetParent(card.transform, false);
        var r2Rt = row2.AddComponent<RectTransform>();
        r2Rt.anchorMin = new Vector2(0.5f, 1f); r2Rt.anchorMax = new Vector2(0.5f, 1f);
        r2Rt.pivot = new Vector2(0.5f, 1f); r2Rt.anchoredPosition = new Vector2(0f, -410f); r2Rt.sizeDelta = new Vector2(820f, 90f);
        row2.AddComponent<Image>().color = new Color(0.16f, 0.34f, 0.46f, 0.85f);
        MakeLabel(row2.transform,
            SafeGet("minigame_otter_howto_line2", "Blijf schudden tot de schelp openbreekt!"),
            Vector2.zero, new Vector2(780f, 80f), 28, FontStyle.Bold, Color.white, out var l2);
        var l2Rt = l2.rectTransform; l2Rt.anchorMin = Vector2.zero; l2Rt.anchorMax = Vector2.one;
        l2Rt.offsetMin = new Vector2(20f, 0f); l2Rt.offsetMax = new Vector2(-20f, 0f); l2Rt.pivot = new Vector2(0.5f, 0.5f);
        l2.alignment = TextAnchor.MiddleCenter;

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
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(900f, 580f);
        crt.localScale = Vector3.zero;
        var cImg = card.AddComponent<Image>();
        cImg.color = new Color(0.08f, 0.14f, 0.18f, 0.97f);

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 14f);
        accent.AddComponent<Image>().color = new Color(0.35f, 0.75f, 0.95f);

        MakeLabel(card.transform, SafeGet("minigame_complete", "Gefeliciteerd!"),
            new Vector2(0f, -55f), new Vector2(840f, 80f), 56, FontStyle.Bold, Color.white, out _);

        MakeLabel(card.transform, SafeGet("minigame_otter_success_title", "Smikkelen maar!"),
            new Vector2(0f, -150f), new Vector2(840f, 60f), 36, FontStyle.Normal, new Color(0.6f, 0.9f, 1f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_coins_earned", $"Je hebt {coinReward} munten verdiend!"),
            new Vector2(0f, -240f), new Vector2(840f, 60f), 38, FontStyle.Normal, new Color(0.35f, 1f, 0.55f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_otter_success_desc", "De otter heeft de schelp gekraakt!"),
            new Vector2(0f, -310f), new Vector2(840f, 50f), 26, FontStyle.Normal, new Color(0.92f, 0.97f, 1f), out _);

        var continueBtn = MakeButton(card.transform, SafeGet("btn_continue", "Doorgaan"),
            new Vector2(0f, 32f), new Vector2(500f, 110f), new Color(0.18f, 0.62f, 0.32f));
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